using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        private bool KickPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            // kick player
            player.Disconnect(0);
            // announce to all
            Server.PrintToChatAll(Localizer["command.kick"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }
        private bool BanPlayer(CCSPlayerController player, string reason = "Unknown")
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            // kick player
            player.Disconnect(0);
            // add to ban list if not already added
            if (!Config.BannedPlayers.ContainsKey(player.SteamID))
            {
                Config.BannedPlayers.Add(player.SteamID, new Dictionary<string, string>
                {
                    { "name", player.PlayerName },
                    { "reason", reason}
                });
                // write to config
                Config.Update();
            }
            // announce to all
            Server.PrintToChatAll(Localizer["command.ban"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{reason}", reason));
            return true;
        }

        private bool UnbanPlayer(ulong SteamID)
        {
            if (!Config.BannedPlayers.ContainsKey(SteamID))
            {
                return false;
            }

            string playerName = Config.BannedPlayers[SteamID].TryGetValue("name", out string? name) ? name : SteamID.ToString();
            // remove from ban list)
            _ = Config.BannedPlayers.Remove(SteamID);
            // write to config
            Config.Update();
            // announce to all
            Server.PrintToChatAll(Localizer["command.unban"].Value
            .Replace("{player}", playerName));
            return true;
        }

        private bool MutePlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
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
                    { "name", player.PlayerName }
                });
                // write to config
                Config.Update();
            }
            // announce to all
            Server.PrintToChatAll(Localizer["command.mute"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool UnmutePlayer(ulong SteamID)
        {
            if (!Config.MutedPlayers.ContainsKey(SteamID))
            {
                return false;
            }

            string playerName = Config.MutedPlayers[SteamID].TryGetValue("name", out string? name) ? name : SteamID.ToString();
            // remove from mute list)
            _ = Config.MutedPlayers.Remove(SteamID);
            // write to config
            Config.Update();
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

        private bool ForceSwitchPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            CsTeam newTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            player.SwitchTeam(newTeam);
            // announce to all
            Server.PrintToChatAll(Localizer["command.fswitch"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool SwitchPlayer(CCSPlayerController player, CsTeam team = CsTeam.None)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            if (team == CsTeam.None)
            {
                team = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            }

            player.ChangeTeam(team);
            // announce to all
            Server.PrintToChatAll(Localizer["command.switch"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{team}", team.ToString()));
            return true;
        }

        private bool RespawnPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            player.Respawn();
            // announce to all
            Server.PrintToChatAll(Localizer["command.respawn"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool KillPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid)
            {
                return false;
            }
            player.CommitSuicide(false, true);
            // announce to all
            Server.PrintToChatAll(Localizer["command.kill"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool GivePlayer(CCSPlayerController player, string? item)
        {
            if (player == null
                || !player.IsValid
                || item == null
                || item == "")
            {
                return false;
            }
            player.GiveNamedItem(item.ToLower());
            // announce to all
            Server.PrintToChatAll(Localizer["command.give"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{item}", item));
            return true;
        }

        private bool RestartMatch(int delay = 3)
        {
            // restart match
            Server.ExecuteCommand($"mp_restartgame {delay}");
            // announce to all
            Server.PrintToChatAll(Localizer["command.restart"]);
            return true;
        }
    }
}
