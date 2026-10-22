using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;

namespace MiniAdmin
{
    /// <summary>Timed bans, bans by SteamID and the chat gag.</summary>
    public partial class MiniAdmin
    {
        private static long UnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Unix time the ban ends; 0 = permanent (also for entries made before timed bans existed).</summary>
        private static long BanUntil(Dictionary<string, string> data) =>
            data.TryGetValue("until", out string? until) && long.TryParse(until, out long value) ? value : 0;

        private Dictionary<string, string> NewBanEntry(string name, string reason, CCSPlayerController? admin, int minutes) => new()
        {
            { "name", name },
            { "reason", reason },
            { "date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
            { "admin", admin?.PlayerName ?? "Console" },
            { "until", minutes > 0 ? (UnixNow() + (minutes * 60L)).ToString() : "0" }
        };

        /// <summary>True while the ban runs; an expired ban is removed on the way.</summary>
        private bool IsBanActive(ulong steamId)
        {
            if (!Config.BannedPlayers.TryGetValue(steamId, out Dictionary<string, string>? data))
            {
                return false;
            }
            long until = BanUntil(data);
            if (until > 0 && until <= UnixNow())
            {
                _ = Config.BannedPlayers.Remove(steamId);
                SaveConfig();
                return false;
            }
            return true;
        }

        private void PurgeExpiredBans()
        {
            long now = UnixNow();
            List<ulong> expired = [.. Config.BannedPlayers
                .Where(kvp => BanUntil(kvp.Value) is long until && until > 0 && until <= now)
                .Select(kvp => kvp.Key)];
            if (expired.Count == 0)
            {
                return;
            }
            foreach (ulong steamId in expired)
            {
                _ = Config.BannedPlayers.Remove(steamId);
            }
            SaveConfig();
        }

        /// <summary>Bans a SteamID64, online or not. An online player is kicked as well.</summary>
        private bool BanSteamId(ulong steamId, int minutes, string reason, CCSPlayerController? admin = null)
        {
            if (steamId < 76561197960265728UL)
            {
                return false;
            }
            CCSPlayerController? online = Utilities.GetPlayers()
                .FirstOrDefault(p => p is { IsValid: true, IsBot: false } && p.SteamID == steamId);
            if (online != null)
            {
                return BanPlayer(online, reason, admin, minutes);
            }
            if (Config.BannedPlayers.ContainsKey(steamId))
            {
                return false;
            }
            Config.BannedPlayers.Add(steamId, NewBanEntry(steamId.ToString(), reason, admin, minutes));
            SaveConfig();
            LogAction(admin, "ban", steamId.ToString(), steamId.ToString(), reason + ", " + DurationText(minutes));
            // announce to all
            Server.PrintToChatAll(Localizer["command.banid"].Value
                .Replace("{steamid}", steamId.ToString())
                .Replace("{reason}", reason)
                .Replace("{duration}", DurationText(minutes)));
            return true;
        }

        private bool IsPlayerGagged(CCSPlayerController player) =>
            !player.IsBot && Config.GaggedPlayers.ContainsKey(player.SteamID);

        private bool GagPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player is not { IsValid: true, IsBot: false }
                || Config.GaggedPlayers.ContainsKey(player.SteamID))
            {
                return false;
            }
            Config.GaggedPlayers.Add(player.SteamID, new Dictionary<string, string>
            {
                { "name", player.PlayerName },
                { "date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                { "admin", admin?.PlayerName ?? "Console" }
            });
            SaveConfig();
            LogAction(admin, "gag", player);
            // announce to all
            Server.PrintToChatAll(Localizer["command.gag"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool UngagPlayer(ulong steamId, CCSPlayerController? admin = null)
        {
            if (!Config.GaggedPlayers.TryGetValue(steamId, out Dictionary<string, string>? data))
            {
                return false;
            }
            string playerName = data.TryGetValue("name", out string? name) ? name : steamId.ToString();
            _ = Config.GaggedPlayers.Remove(steamId);
            SaveConfig();
            LogAction(admin, "ungag", playerName, steamId.ToString());
            // announce to all
            Server.PrintToChatAll(Localizer["command.ungag"].Value
                .Replace("{player}", playerName));
            return true;
        }

        private string DurationText(int minutes) => DurationText(minutes, key => Localizer[key].Value);

        private static string DurationText(int minutes, Func<string, string> text)
        {
            if (minutes <= 0)
            {
                return text("menu.dur.perm");
            }
            if (minutes % 1440 == 0)
            {
                return text("menu.dur.days").Replace("{n}", (minutes / 1440).ToString());
            }
            if (minutes % 60 == 0)
            {
                return text("menu.dur.hours").Replace("{n}", (minutes / 60).ToString());
            }
            return text("menu.dur.minutes").Replace("{n}", minutes.ToString());
        }
    }
}
