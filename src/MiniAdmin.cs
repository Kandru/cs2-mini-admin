using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;

namespace MiniAdmin
{
    public partial class MiniAdmin : BasePlugin
    {
        public override string ModuleName => "CS2 MiniAdmin";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";


        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        }

        public override void Unload(bool hotReload)
        {
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || string.IsNullOrEmpty(player.NetworkIDString))
            {
                return HookResult.Continue;
            }
            // kick player
            if (Config.BannedPlayers.ContainsKey(player.SteamID))
            {
                player.Disconnect(0);
                Server.PrintToChatAll(Localizer["command.banned"].Value
                    .Replace("{player}", player.PlayerName));
            }
            // mute player
            if (Config.MutedPlayers.ContainsKey(player.SteamID))
            {
                _ = MutePlayer(player);
            }
            return HookResult.Continue;
        }

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

        private bool BanPlayer(CCSPlayerController player)
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
                    { "name", player.PlayerName }
                });
                // write to config
                Config.Update();
            }
            Server.PrintToChatAll(Localizer["command.ban"].Value
                .Replace("{player}", player.PlayerName));
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

        private bool RestartMatch(int delay = 3)
        {
            // restart match
            Server.ExecuteCommand($"mp_restartgame {delay}");
            Server.PrintToChatAll(Localizer["command.restart"]);
            return true;
        }
    }
}