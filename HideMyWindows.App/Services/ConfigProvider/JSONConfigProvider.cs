using HideMyWindows.App.Helpers;
using HideMyWindows.App.Models;
using System.IO;
using System.Text.Json;
using Wpf.Ui.Controls;

namespace HideMyWindows.App.Services.ConfigProvider
{
    public partial class JSONConfigProvider : IConfigProvider
    {
        private ISnackbarService SnackbarService { get; }

        // TODO: Needs to be dynamic to adapt for installer or portable installations
        public string ConfigPath { get; set; } = Environment.ExpandEnvironmentVariables(Path.Join("%USERPROFILE%", "Downloads", "HideMyWindowsTest", "HideMyWindows.json"));
        public Config? Config { get; set; }

        private JsonSerializerOptions JsonOptions { get; }

        public JSONConfigProvider(ISnackbarService snackbarService)
        {
            SnackbarService = snackbarService;

            JsonOptions = new()
            {
                WriteIndented = true
            };
        }

        public void Reload(string path)
        {
            if (File.Exists(ConfigPath))
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    Config = JsonSerializer.Deserialize<Config>(json, JsonOptions);
                }
                catch (Exception e) when (e is JsonException or IOException)
                {
                    //TODO: Error handling
                    SnackbarService.Show("An error occurred!", "Error while loading the configuration, will fallback to default configuration, saving will overwrite the old one.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
                    Config = new();
                }
            else
                Config = new();
        }

        public void Save(string path)
        {
            try
            {
                var json = JsonSerializer.Serialize(Config, JsonOptions);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
            } catch (Exception e) when (e is JsonException or IOException)
            {
                //TODO: Error handling
                SnackbarService.Show("An error occurred!", e.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24));
            }
        }
    }
}
