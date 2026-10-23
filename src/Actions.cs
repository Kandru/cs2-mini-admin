using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        private bool KickPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: false))
            {
                return false;
            }
            LogAction(admin, "kick", player);
            // kick player
            player.Disconnect(0);
            // announce to all
            Server.PrintToChatAll(Localizer["command.kick"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }
        /// <param name="minutes">ban duration, 0 = permanent</param>
        private bool BanPlayer(CCSPlayerController player, string reason = "Unknown", CCSPlayerController? admin = null, int minutes = 0)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: false))
            {
                return false;
            }
            // add to ban list if not already added
            if (!Config.BannedPlayers.ContainsKey(player.SteamID))
            {
                Config.BannedPlayers.Add(player.SteamID, NewBanEntry(player.PlayerName, reason, admin, minutes));
                // write to config
                SaveConfig();
                LogAction(admin, "ban", player, reason + ", " + DurationText(minutes));
            }
            // kick player
            player.Disconnect(0);
            // announce to all
            Server.PrintToChatAll((minutes > 0 ? Localizer["command.tempban"].Value : Localizer["command.ban"].Value)
                .Replace("{player}", player.PlayerName)
                .Replace("{reason}", reason)
                .Replace("{duration}", DurationText(minutes)));
            return true;
        }

        private bool UnbanPlayer(ulong SteamID, CCSPlayerController? admin = null)
        {
            if (!Config.BannedPlayers.ContainsKey(SteamID))
            {
                return false;
            }

            string playerName = Config.BannedPlayers[SteamID].TryGetValue("name", out string? name) ? name : SteamID.ToString();
            // remove from ban list)
            _ = Config.BannedPlayers.Remove(SteamID);
            // write to config
            SaveConfig();
            LogAction(admin, "unban", playerName, SteamID.ToString());
            // announce to all
            Server.PrintToChatAll(Localizer["command.unban"].Value
            .Replace("{player}", playerName));
            return true;
        }

        private bool MutePlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: false))
            {
                return false;
            }
            // mute player
            player.VoiceFlags = VoiceFlags.Muted;
            // add to ban list if not already added
            if (!Config.MutedPlayers.ContainsKey(player.SteamID))
            {
                Config.MutedPlayers.Add(player.SteamID, new Dictionary<string, string>
                {
                    { "name", player.PlayerName },
                    { "date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
                    { "admin", admin?.PlayerName ?? "Console" }
                });
                // write to config
                SaveConfig();
                LogAction(admin, "mute", player);
            }
            // announce to all
            Server.PrintToChatAll(Localizer["command.mute"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool UnmutePlayer(ulong SteamID, CCSPlayerController? admin = null)
        {
            if (!Config.MutedPlayers.ContainsKey(SteamID))
            {
                return false;
            }

            string playerName = Config.MutedPlayers[SteamID].TryGetValue("name", out string? name) ? name : SteamID.ToString();
            // remove from mute list)
            _ = Config.MutedPlayers.Remove(SteamID);
            // write to config
            SaveConfig();
            LogAction(admin, "unmute", playerName, SteamID.ToString());
            // check if player is online
            IEnumerable<CCSPlayerController> players = Utilities.GetPlayers().Where(p => p.IsValid && p.SteamID == SteamID);
            if (players.Count() == 1)
            {
                CCSPlayerController player = players.First();
                // unmute player
                player.VoiceFlags = VoiceFlags.Normal;
            }
            // announce to all
            Server.PrintToChatAll(Localizer["command.unmute"].Value
            .Replace("{player}", playerName));
            return true;
        }

        private bool ForceSwitchPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: true))
            {
                return false;
            }
            CsTeam newTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            player.SwitchTeam(newTeam);
            LogAction(admin, "fswitch", player, newTeam.ToString());
            // announce to all
            Server.PrintToChatAll(Localizer["command.fswitch"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool SwitchPlayer(CCSPlayerController player, CsTeam team = CsTeam.None, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: true))
            {
                return false;
            }
            if (team == CsTeam.None)
            {
                team = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            }

            player.ChangeTeam(team);
            LogAction(admin, "switch", player, team.ToString());
            // announce to all
            Server.PrintToChatAll(Localizer["command.switch"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{team}", team.ToString()));
            return true;
        }

        private bool RespawnPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: true))
            {
                return false;
            }
            player.Respawn();
            LogAction(admin, "respawn", player);
            // announce to all
            Server.PrintToChatAll(Localizer["command.respawn"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool KillPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: true))
            {
                return false;
            }
            player.CommitSuicide(false, true);
            LogAction(admin, "kill", player);
            // announce to all
            Server.PrintToChatAll(Localizer["command.kill"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool GivePlayer(CCSPlayerController player, string? item, CCSPlayerController? admin = null)
        {
            if (player == null
                || !player.IsValid
                || item == null
                || item == "")
            {
                return false;
            }
            if (!MayTarget(admin, player, allowSelf: true))
            {
                return false;
            }
            player.GiveNamedItem(item.ToLower());
            LogAction(admin, "give", player, item.ToLower());
            // announce to all
            Server.PrintToChatAll(Localizer["command.give"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{item}", item));
            return true;
        }

        /// <summary>
        /// Immunity for every action, whatever started it (chat menus, commands, admin menu).
        /// Console and the detections (admin null) may target anyone.
        /// </summary>
        private bool MayTarget(CCSPlayerController? admin, CCSPlayerController target, bool allowSelf)
        {
            if (admin is not { IsValid: true })
            {
                return true;
            }
            if (!allowSelf && admin.Slot == target.Slot)
            {
                admin.PrintToChat(Localizer["command.notself"]);
                return false;
            }
            if (!CanTarget(admin, target))
            {
                admin.PrintToChat(Localizer["command.immune"]);
                return false;
            }
            return true;
        }

        // config writes are bundled: a burst of bans (or the purge of expired ones) writes the file once
        private bool _configSavePending;

        private void SaveConfig()
        {
            _recordsVersion++;
            if (_configSavePending)
            {
                return;
            }
            _configSavePending = true;
            _ = AddTimer(0.5f, FlushConfig);
        }

        private void FlushConfig()
        {
            if (!_configSavePending)
            {
                return;
            }
            _configSavePending = false;
            Config.Update();
        }

        private bool RestartMatch(int delay = 3, CCSPlayerController? admin = null)
        {
            // restart match
            Server.ExecuteCommand($"mp_restartgame {delay}");
            LogAction(admin, "restart", string.Empty, string.Empty, delay + " s");
            // announce to all
            Server.PrintToChatAll(Localizer["command.restart"]);
            return true;
        }
    }
}
