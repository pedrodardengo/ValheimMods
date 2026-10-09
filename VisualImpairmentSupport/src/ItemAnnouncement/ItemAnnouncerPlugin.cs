using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ItemAnnouncer
{
    internal static class Plugin
    {
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Ativar;
        internal static ConfigEntry<bool> FalarCliqueMeio;
        internal static ConfigEntry<string> Formato;
        internal static ConfigEntry<bool> AnunciarTotal;
        internal static ConfigEntry<string> FormatoComTotal;
        internal static ConfigEntry<string> Voz;
        internal static ConfigEntry<int> Velocidade;
        internal static ConfigEntry<int> Volume;
        private static BepInEx.Configuration.ConfigFile _config;

        internal static void Initialize(BaseUnityPlugin host, ManualLogSource logger)
        {
            _config = host.Config;
            Log = logger;

            Ativar = host.Config.Bind("1 - Geral", "Ativar", true,
                "Ativar ou desativar os anuncios por voz.");

            FalarCliqueMeio = host.Config.Bind("1 - Geral", "FalarCliqueMeio", true,
                "Falar ao clicar com o botao do meio (M3) sobre um item no inventario.");

            Formato = host.Config.Bind("2 - Texto", "Formato", "{name}: {count}",
                "Texto falado. Placeholders: {name} = nome do item, {count} = quantidade na pilha.");

            AnunciarTotal = host.Config.Bind("2 - Texto", "AnunciarTotal", false,
                "Se verdadeiro, usa o FormatoComTotal (inclui a quantidade total do item no inventario/container).");

            FormatoComTotal = host.Config.Bind("2 - Texto", "FormatoComTotal", "{name}: {count}, total {total}",
                "Texto falado quando AnunciarTotal esta ligado. Placeholders: {name}, {count}, {total}.");

            Voz = host.Config.Bind("3 - Voz", "Voz", "",
                "Nome (ou parte do nome) da voz do Windows. Vazio = usa uma voz pt-BR automaticamente.");

            Velocidade = host.Config.Bind("3 - Voz", "Velocidade", 0,
                "Velocidade da fala, de -10 (devagar) a 10 (rapido).");

            Volume = host.Config.Bind("3 - Voz", "Volume", 100,
                "Volume da fala, de 0 a 100.");

            TtsClient.Initialize(Log, Voz.Value, Velocidade.Value, Volume.Value);
            _config.SettingChanged += OnSettingChanged;
        }

        private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            TtsClient.ApplySettings(Voz.Value, Velocidade.Value, Volume.Value);
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
