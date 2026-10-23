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
    public class MenuConfig
    {
        // maps of the !admin menu: map name, or workshop id with an optional label ("3070284539|Mocha")
        [JsonPropertyName("maps")]
        public List<string> Maps { get; set; } =
        [
            "de_ancient", "de_anubis", "de_dust2", "de_inferno", "de_mirage", "de_nuke",
            "de_overpass", "de_train", "de_vertigo", "cs_italy", "cs_office"
        ];
        // items of the "give" buttons (at most 12)
        [JsonPropertyName("give_items")]
        public List<string> GiveItems { get; set; } =
        [
            "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_awp",
            "weapon_deagle", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade",
            "weapon_molotov", "item_assaultsuit", "item_defuser", "weapon_healthshot"
        ];
        // ban durations of the menu's ban picker in minutes (exactly 4), 0 = permanent
        [JsonPropertyName("ban_durations")] public List<int> BanDurations { get; set; } = [60, 1440, 10080, 0];
        // damage of one slap
        [JsonPropertyName("slap_damage")] public int SlapDamage { get; set; } = 5;
    }
    public class DiscordConfig
    {
        // Discord webhook for admin actions; empty = off
        [JsonPropertyName("webhook_url")] public string WebhookUrl { get; set; } = "";
        // actions that are posted (action log keys, e.g. kick, ban, unban, mute, gag)
        [JsonPropertyName("events")] public List<string> Events { get; set; } = ["kick", "ban", "unban"];
    }
    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // detection settings
        [JsonPropertyName("detections")] public DetectionConfig Detections { get; set; } = new();
        // admin menu (!admin)
        [JsonPropertyName("menu")] public MenuConfig Menu { get; set; } = new();
        // discord notifications
        [JsonPropertyName("discord")] public DiscordConfig Discord { get; set; } = new();
        // write admin actions to addons/counterstrikesharp/logs/MiniAdmin-actions.log
        [JsonPropertyName("action_log_file")] public bool ActionLogFile { get; set; } = true;
        // banned players
        [JsonPropertyName("players_banned")] public Dictionary<ulong, Dictionary<string, string>> BannedPlayers { get; set; } = [];
        // muted players
        [JsonPropertyName("players_muted")] public Dictionary<ulong, Dictionary<string, string>> MutedPlayers { get; set; } = [];
        // gagged players (chat blocked)
        [JsonPropertyName("players_gagged")] public Dictionary<ulong, Dictionary<string, string>> GaggedPlayers { get; set; } = [];
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
