using System;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using System.Text;

namespace ItemAnnouncer.Speaker
{
    internal static class Program
    {
        private static SpeechSynthesizer _synth;

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = new UTF8Encoding(false);
            }
            catch
            {
            }

            try
            {
                _synth = new SpeechSynthesizer();
                _synth.SetOutputToDefaultAudioDevice();
                SelectDefaultVoice();
                WarmUp();
                Write("READY");
            }
            catch (Exception ex)
            {
                Write("ERR init " + ex.Message);
                return 1;
            }

            var reader = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                try
                {
                    Handle(line);
                }
                catch (Exception ex)
                {
                    Write("ERR " + ex.Message);
                }
            }

            try
            {
                _synth.Dispose();
            }
            catch
            {
            }
            return 0;
        }

        private static void Handle(string line)
        {
            int space = line.IndexOf(' ');
            string command = space < 0 ? line : line.Substring(0, space);
            string argument = space < 0 ? string.Empty : line.Substring(space + 1);

            switch (command)
            {
                case "SAY":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        return;
                    }
                    _synth.SpeakAsyncCancelAll();
                    _synth.SpeakAsync(argument);
                    break;

                case "RATE":
                    _synth.Rate = Clamp(ParseInt(argument, 0), -10, 10);
                    break;

                case "VOLUME":
                    _synth.Volume = Clamp(ParseInt(argument, 100), 0, 100);
                    break;

                case "QUIT":
                    _synth.SpeakAsyncCancelAll();
                    _synth.Dispose();
                    Environment.Exit(0);
                    break;

                default:
                    Write("ERR cmd " + command);
                    break;
            }
        }

        private static void SelectDefaultVoice()
        {
            foreach (InstalledVoice installed in _synth.GetInstalledVoices())
            {
                VoiceInfo info = installed.VoiceInfo;
                if (installed.Enabled && info != null && info.Culture != null && info.Culture.Name == "pt-BR")
                {
                    _synth.SelectVoice(info.Name);
                    Write("VOICE " + info.Name);
                    return;
                }
            }
            Write("VOICE (padrao do sistema)");
        }


        private static void WarmUp()
        {
            try
            {
                _synth.Speak(" ");
            }
            catch
            {
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return fallback;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void Write(string message)
        {
            try
            {
                Console.Out.WriteLine(message);
                Console.Out.Flush();
            }
            catch
            {
            }
        }
    }
}
