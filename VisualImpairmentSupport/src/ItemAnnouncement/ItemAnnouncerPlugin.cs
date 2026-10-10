using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ItemAnnouncer
{
    internal static class Plugin
    {
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Ativar;
        internal static ConfigEntry<string> Formato;
        internal static ConfigEntry<string> Voz;
        internal static ConfigEntry<int> Velocidade;
        internal static ConfigEntry<int> Volume;
        private static BepInEx.Configuration.ConfigFile _config;

        internal static void Initialize(BaseUnityPlugin host, ManualLogSource logger)
        {
            _config = host.Config;
            Log = logger;

            Ativar = host.Config.Bind("Voice", "Enabled", true,
                "Enable or disable voice announcements.");

            Formato = host.Config.Bind("Voice", "Text", "{name}: {count}",
                "Spoken text. Placeholders: {name} = item name, {count} = stack quantity.");

            string[] voiceOptions = TtsClient.GetAvailableVoices(logger);
            Voz = host.Config.Bind("Voice", "Speech voice", TtsClient.AutomaticVoice,
                new ConfigDescription(
                    "Select an installed Windows speech voice. Automatic prefers Portuguese (Brazil), then uses the system default.",
                    new AcceptableValueList<string>(voiceOptions)));

            Velocidade = host.Config.Bind("Voice", "Speed", 0,
                new ConfigDescription("Speech speed, from -10 (slow) to 10 (fast).", new AcceptableValueRange<int>(-10, 10)));

            Volume = host.Config.Bind("Voice", "Volume", 100,
                new ConfigDescription("Speech volume, from 0 to 100.", new AcceptableValueRange<int>(0, 100)));

            TtsClient.Initialize(Log, Velocidade.Value, Volume.Value, Voz.Value);
            _config.SettingChanged += OnSettingChanged;
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            TtsClient.ApplySettings(Velocidade.Value, Volume.Value, Voz.Value);
        }

        internal static void Shutdown()
        {
            if (_config != null)
            {
                _config.SettingChanged -= OnSettingChanged;
                _config = null;
            }
            TtsClient.Shutdown();
        }
    }
}
