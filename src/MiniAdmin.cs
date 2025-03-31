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
                || !Config.BannedPlayers.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // kick player
            Server.ExecuteCommand($"kick {player.Index}");
            Server.PrintToChatAll(Localizer["command.banned"].Value
                .Replace("{player}", player.PlayerName));
            return HookResult.Continue;
        }

        private bool KickPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid) return false;
            // kick player
            Server.ExecuteCommand($"kick {player.Index}");
            Server.PrintToChatAll(Localizer["command.kick"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool BanPlayer(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid) return false;
            // kick player
            Server.ExecuteCommand($"kick {player.Index}");
            // add to ban list if not already added
            if (!Config.BannedPlayers.ContainsKey(player.NetworkIDString.ToString()))
            {
                Config.BannedPlayers.Add(player.NetworkIDString.ToString(), new Dictionary<string, string>
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

        private bool UnbanPlayer(string SteamID)
        {
            if (string.IsNullOrEmpty(SteamID)
                || Config.BannedPlayers.ContainsKey(SteamID)) return false;
            string playerName = Config.BannedPlayers[SteamID].TryGetValue("name", out var name) ? name : SteamID;
            // remove from ban list)
            Config.BannedPlayers.Remove(SteamID);
            // write to config
            Config.Update();
            Server.PrintToChatAll(Localizer["command.unban"].Value
            .Replace("{player}", playerName));
            return true;
        }
    }
}