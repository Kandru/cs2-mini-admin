using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;

namespace MiniAdmin
{
    public class DetectionConfig
    {
        [JsonPropertyName("enable_namechange_check")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("max_namechanges_per_minute")] public int MaxNameChangesPerMinute { get; set; } = 20;
        [JsonPropertyName("action_on_namechange_detection")] public string ActionOnNamechangeDetection { get; set; } = "ban";
    }
    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // detection settings
        [JsonPropertyName("detections")] public DetectionConfig Detections { get; set; } = new();
        // banned players
        [JsonPropertyName("players_banned")] public Dictionary<ulong, Dictionary<string, string>> BannedPlayers { get; set; } = [];
        // muted players
        [JsonPropertyName("players_muted")] public Dictionary<ulong, Dictionary<string, string>> MutedPlayers { get; set; } = [];
    }

    public partial class MiniAdmin : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }
    }
}
