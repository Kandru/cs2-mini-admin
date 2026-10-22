using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.ValveConstants.Protobuf;

namespace MiniAdmin
{
    public partial class MiniAdmin : BasePlugin
    {
        public override string ModuleName => "CS2 MiniAdmin";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        // use a dictionary for the connected players to save information to save:
        // - the player name on connection to avoid fast name-changing hacks and allow the player to be identified properly
        // entries: name, steam_id, timestamp (CurrentTime)
        private readonly Dictionary<CCSPlayerController, Dictionary<string, string>> _connectedPlayers = [];

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerChangename>(OnPlayerChangeName);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
            LoadActionLog();
            LoadChat();
            LoadAdminMenu(hotReload);
            if (hotReload)
            {
                // players connected before the reload never fired EventPlayerConnectFull for this instance
                foreach (CCSPlayerController player in Utilities.GetPlayers())
                {
                    if (player is { IsValid: true, IsBot: false, IsHLTV: false } && !_connectedPlayers.ContainsKey(player))
                    {
                        _connectedPlayers.Add(player, new Dictionary<string, string>
                        {
                            { "name", player.PlayerName },
                            { "steam_id", player.SteamID.ToString() },
                            { "timestamp", Server.CurrentTime.ToString() }
                        });
                    }
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            DeregisterEventHandler<EventPlayerChangename>(OnPlayerChangeName);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
            UnloadChat();
            UnloadAdminMenu();
            FlushConfig();
        }

        // ban check with the SteamID Steam has confirmed, before the player is in the game
        private void OnClientAuthorized(int playerSlot, SteamID steamId)
        {
            ulong id = steamId.SteamId64;
            if (!IsBanActive(id))
            {
                return;
            }
            Server.NextFrame(() =>
            {
                if (Utilities.GetPlayerFromSlot(playerSlot) is { IsValid: true } player && player.SteamID == id)
                {
                    KickBanned(player);
                }
            });
        }

        // one announcement per banned player and minute: a reconnect loop must not flood the chat
        private readonly Dictionary<ulong, long> _banAnnounced = [];

        private void KickBanned(CCSPlayerController player)
        {
            string name = player.PlayerName;
            ulong id = player.SteamID;
            player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKBANADDED);
            long now = Environment.TickCount64;
            if (_banAnnounced.TryGetValue(id, out long last) && now - last < 60_000)
            {
                return;
            }
            _banAnnounced[id] = now;
            Server.PrintToChatAll(Localizer["command.banned"].Value
                .Replace("{player}", name));
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
            _connectedPlayers[player]["timestamp"] = Server.CurrentTime.ToString();
            // kick player (fallback for servers where OnClientAuthorized does not fire)
            if (IsBanActive(player.SteamID))
            {
                KickBanned(player);
                return HookResult.Continue;
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
            _ = _detectionNameChange.Remove(player);
            // remove player from dictionary
            if (_connectedPlayers.ContainsKey(player))
            {
                _connectedPlayers.Remove(player);
            }
            return HookResult.Continue;
        }

        private void OnMapEnd()
        {
            // reset dictionaries
            _connectedPlayers.Clear();
            _detectionNameChange.Clear();
            _banAnnounced.Clear();
            FlushConfig();
        }
    }
}