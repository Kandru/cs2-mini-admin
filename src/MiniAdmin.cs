using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MiniAdmin
{
    public partial class MiniAdmin : BasePlugin
    {
        public override string ModuleName => "CS2 MiniAdmin";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        // use a dictionary for the connected players to save information to save:
        // - the player name on connection to avoid fast name-changing hacks and allow the player to be identified properly
        private readonly Dictionary<CCSPlayerController, Dictionary<string, string>> _connectedPlayers = [];

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerChangename>(OnPlayerChangeName);
        }

        public override void Unload(bool hotReload)
        {
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerChangename>(OnPlayerChangeName);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV)
            {
                return HookResult.Continue;
            }
            // add player to dictionary
            if (!_connectedPlayers.ContainsKey(player))
            {
                _connectedPlayers.Add(player, []);
            }
            // update player data in dictionary
            _connectedPlayers[player]["name"] = player.PlayerName;
            _connectedPlayers[player]["steam_id"] = player.SteamID.ToString();
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

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV)
            {
                return HookResult.Continue;
            }
            // remove player from dictionary
            if (_connectedPlayers.ContainsKey(player))
            {
                _connectedPlayers.Remove(player);
            }
            return HookResult.Continue;
        }
    }
}