using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        private Dictionary<CCSPlayerController, Dictionary<string, int>> _detectionNameChange = [];

        // detection of name changes (which normally makes it harder to kick someone)
        private HookResult OnPlayerChangeName(EventPlayerChangename @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (!Config.Detections.Enabled
                || player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV)
            {
                return HookResult.Continue;
            }
            int now = (int)Server.CurrentTime;
            // add player if not already; the timestamp has to exist before it is read below
            if (!_detectionNameChange.TryGetValue(player, out Dictionary<string, int>? data))
            {
                data = new Dictionary<string, int> { { "count", 0 }, { "timestamp", now } };
                _detectionNameChange.Add(player, data);
            }
            // reset counter after a minute without name changes, then count this one
            if (data["timestamp"] < now - 60)
            {
                data["count"] = 0;
            }
            data["count"]++;
            // check for spamming name change events (bigger then Config.Detections.MaxNameChangesPerMinute)
            if (_detectionNameChange[player]["count"] >= Config.Detections.MaxNameChangesPerMinute)
            {
                string action = Config.Detections.ActionOnNamechangeDetection.ToLower();
                switch (action)
                {
                    case "kick":
                        KickPlayer(player);
                        break;
                    case "ban":
                        BanPlayer(player);
                        break;
                    default:
                        KickPlayer(player);
                        action = "kick";
                        break;
                }
                Server.PrintToChatAll(Localizer["detection.namechange"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{action}", action));
            }
            // update timestamp
            _detectionNameChange[player]["timestamp"] = (int)Server.CurrentTime;
            return HookResult.Continue;
        }
    }
}
