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
            return true;
        }

        private bool RestartMatch(int delay = 3)
        {
            // restart match
            Server.ExecuteCommand($"mp_restartgame {delay}");
            Server.PrintToChatAll(Localizer["command.restart"]);
            return true;
        }
    }
}
