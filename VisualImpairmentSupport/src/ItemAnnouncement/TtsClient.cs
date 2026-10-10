using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace ItemAnnouncer
{
    internal static class TtsClient
    {
        private const string HostScriptName = "ItemAnnouncer.Speaker.ps1";
        internal const string AutomaticVoice = "Automatic (Portuguese if available)";
        private const int MaxStartFailures = 5;

        private static readonly object IoLock = new object();

        private static ManualLogSource _log;
        private static string _scriptPath;
        private static Process _process;
        private static Stream _stdin;
        private static int _startFailures;

        internal static string[] GetAvailableVoices(ManualLogSource log)
        {
            _log = log;
            SetScriptPath();

            var voices = new List<string> { AutomaticVoice };
            if (string.IsNullOrEmpty(_scriptPath) || !File.Exists(_scriptPath))
            {
                return voices.ToArray();
            }

            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string powershellPath = Path.Combine(windowsDirectory, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(powershellPath))
            {
                LogWarning("Windows PowerShell 5.1 was not found; using automatic voice selection only.");
                return voices.ToArray();
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = powershellPath,
                    Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -STA -File \"" + _scriptPath + "\" -ListVoices",
                    WorkingDirectory = Path.GetDirectoryName(_scriptPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process { StartInfo = info })
                {
                    process.Start();
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(15000))
                    {
                        process.Kill();
                        LogWarning("Timed out while listing Windows speech voices.");
                        return voices.ToArray();
                    }

                    string output = outputTask.GetAwaiter().GetResult();
                    string error = errorTask.GetAwaiter().GetResult();
                    if (process.ExitCode != 0)
                    {
                        LogWarning("Could not list Windows speech voices: " + error.Trim());
                        return voices.ToArray();
                    }

                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            string voice = Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim()));
                            if (!string.IsNullOrWhiteSpace(voice) && !voices.Contains(voice))
                            {
                                voices.Add(voice);
                            }
                        }
                        catch (FormatException)
                        {
                            LogWarning("Ignored an invalid voice entry returned by Windows Speech.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Could not list Windows speech voices: " + ex.Message);
            }

            return voices.ToArray();
        }

        internal static void Initialize(ManualLogSource log, int rate, int volume, string voice)
        {
            _log = log;
            SetScriptPath();

            if (_scriptPath == null || !File.Exists(_scriptPath))
            {
                LogError("Script do alto-falante nao encontrado: " + (_scriptPath ?? "?"));
                return;
            }

            if (EnsureProcess())
            {
                ApplySettings(rate, volume, voice);
            }
        }

        internal static void ApplySettings(int rate, int volume, string voice)
        {
            if (!EnsureProcess())
            {
                return;
            }

            try
            {
                SendRaw("VOICE " + CleanArgument(voice));
                SendRaw("RATE " + Clamp(rate, -10, 10));
                SendRaw("VOLUME " + Clamp(volume, 0, 100));
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        internal static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !EnsureProcess())
            {
                return;
            }

            try
            {
                SendRaw("SAY " + text.Replace('\r', ' ').Replace('\n', ' '));
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        internal static void Shutdown()
        {
            Process process = _process;
            _process = null;
            if (process == null)
            {
                return;
            }

            try
            {
                SendRaw("QUIT");
                if (!process.WaitForExit(1200))
                {
                    process.Kill();
                }
            }
            catch
            {
                try { process.Kill(); } catch { }
            }
            finally
            {
                _stdin = null;
                process.Dispose();
            }
        }

        private static bool EnsureProcess()
        {
            if (_process != null && !_process.HasExited)
            {
                return true;
            }
            return StartProcess();
        }

        private static void SetScriptPath()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string folder = string.IsNullOrEmpty(assemblyPath) ? null : Path.GetDirectoryName(assemblyPath);
            _scriptPath = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, HostScriptName);
        }

        private static bool StartProcess()
        {
            if (_startFailures >= MaxStartFailures || string.IsNullOrEmpty(_scriptPath))
            {
                return false;
            }

            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string powershellPath = Path.Combine(windowsDirectory, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(powershellPath))
            {
                LogError("Windows PowerShell 5.1 nao foi encontrado.");
                return false;
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = powershellPath,
                    Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -STA -File \"" + _scriptPath + "\"",
                    WorkingDirectory = Path.GetDirectoryName(_scriptPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = new Process { StartInfo = info, EnableRaisingEvents = true };
                process.Exited += OnProcessExited;
                process.Start();
                _process = process;
                _stdin = process.StandardInput.BaseStream;

                StartReadLoop(process.StandardOutput.BaseStream, false);
                StartReadLoop(process.StandardError.BaseStream, true);
                LogInfo("Host de fala do Windows iniciado.");
                return true;
            }
            catch (Exception ex)
            {
                _startFailures++;
                _process = null;
                _stdin = null;
                LogError("Falha ao iniciar o host de fala: " + ex.Message);
                return false;
            }
        }

        private static void OnProcessExited(object sender, EventArgs e)
        {
            _process = null;
            _stdin = null;
            LogWarning("O host de fala foi encerrado; sera reiniciado no proximo anuncio.");
        }

        private static void HandleFailure(Exception ex)
        {
            LogError("Erro ao falar: " + ex.Message);
            Process process = _process;
            _process = null;
            _stdin = null;
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        private static void SendRaw(string line)
        {
            lock (IoLock)
            {
                if (_stdin == null)
                {
                    throw new IOException("O host de fala nao esta conectado.");
                }
                byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
                _stdin.Write(bytes, 0, bytes.Length);
                _stdin.Flush();
            }
        }

        private static string CleanArgument(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }

        private static void StartReadLoop(Stream stream, bool isError)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isError) LogWarning("[PowerShell] " + line);
                            else LogInfo("[PowerShell] " + line);
                        }
                    }
                }
                catch
                {
                }
            });
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void LogInfo(string message)
        {
            if (_log != null) _log.LogInfo(message);
        }

        private static void LogWarning(string message)
        {
            if (_log != null) _log.LogWarning(message);
        }

        private static void LogError(string message)
        {
            if (_log != null) _log.LogError(message);
        }
    }
}
