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
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV)
            {
                return HookResult.Continue;
            }
            // add player if not already
            if (!_detectionNameChange.ContainsKey(player))
            {
                _detectionNameChange.Add(player, []);
            }
            // initialize counter and increase appropriately
            if (!_detectionNameChange[player].ContainsKey("count"))
            {
                _detectionNameChange[player]["count"] = 1;
            }
            else
            {
                _detectionNameChange[player]["count"]++;
            }
            // reset counter after minute has passed
            if (_detectionNameChange[player]["timestamp"] < (int)Server.CurrentTime - 60)
            {
                _detectionNameChange[player]["count"] = 0;
            }
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
