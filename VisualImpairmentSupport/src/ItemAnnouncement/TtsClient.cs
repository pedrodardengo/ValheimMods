using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;

namespace ItemAnnouncer
{
    internal static class TtsClient
    {
        private const string SpeakerExeName = "ItemAnnouncer.Speaker.exe";
        private const int MaxStartFailures = 5;

        private static readonly object IoLock = new object();

        private static ManualLogSource _log;
        private static string _exePath;
        private static Process _process;
        private static Stream _stdin;

        private static string _voice = "";
        private static int _rate;
        private static int _volume = 100;
        private static int _startFailures;

        internal static void Initialize(ManualLogSource log, string voice, int rate, int volume)
        {
            _log = log;
            _voice = voice ?? "";
            _rate = Clamp(rate, -10, 10);
            _volume = Clamp(volume, 0, 100);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string folder = string.IsNullOrEmpty(assemblyPath) ? null : Path.GetDirectoryName(assemblyPath);
            _exePath = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, SpeakerExeName);

            if (_exePath == null || !File.Exists(_exePath))
            {
                LogError("Alto-falante nao encontrado (esperado em: " + (_exePath ?? "?") + "). A fala nao vai funcionar.");
                return;
            }

            StartProcess();
        }

        internal static void ApplySettings(string voice, int rate, int volume)
        {
            _voice = voice ?? "";
            _rate = Clamp(rate, -10, 10);
            _volume = Clamp(volume, 0, 100);

            if (_process == null || _process.HasExited)
            {
                return;
            }

            try
            {
                SendRaw("VOICE " + _voice);
                SendRaw("RATE " + _rate);
                SendRaw("VOLUME " + _volume);
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        internal static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            text = text.Replace('\r', ' ').Replace('\n', ' ');
            if (!EnsureProcess())
            {
                return;
            }

            try
            {
                SendRaw("SAY " + text);
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
                lock (IoLock)
                {
                    WriteUnlocked("QUIT");
                }
                if (!process.WaitForExit(800))
                {
                    process.Kill();
                }
            }
            catch
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }
            finally
            {
                _stdin = null;
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

        private static bool StartProcess()
        {
            if (_startFailures >= MaxStartFailures)
            {
                return false;
            }
            if (_exePath == null || !File.Exists(_exePath))
            {
                return false;
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = _exePath,
                    WorkingDirectory = Path.GetDirectoryName(_exePath),
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

                SendRaw("VOICE " + _voice);
                SendRaw("RATE " + _rate);
                SendRaw("VOLUME " + _volume);

                LogInfo("Alto-falante iniciado.");
                return true;
            }
            catch (Exception ex)
            {
                _startFailures++;
                _process = null;
                _stdin = null;
                LogError("Falha ao iniciar o alto-falante: " + ex.Message);
                return false;
            }
        }

        private static void OnProcessExited(object sender, EventArgs e)
        {
            _process = null;
            _stdin = null;
            LogWarning("O alto-falante foi encerrado. Ele sera reiniciado no proximo anuncio.");
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
                WriteUnlocked(line);
            }
        }

        private static void WriteUnlocked(string line)
        {
            if (_stdin == null)
            {
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
            _stdin.Write(bytes, 0, bytes.Length);
            _stdin.Flush();
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
                            if (isError)
                            {
                                LogWarning("[Speaker] " + line);
                            }
                            else
                            {
                                LogInfo("[Speaker] " + line);
                            }
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
            if (_log != null)
            {
                _log.LogInfo(message);
            }
        }

        private static void LogWarning(string message)
        {
            if (_log != null)
            {
                _log.LogWarning(message);
            }
        }

        private static void LogError(string message)
        {
            if (_log != null)
            {
                _log.LogError(message);
            }
        }
    }
}
