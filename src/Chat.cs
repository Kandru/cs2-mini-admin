using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace MiniAdmin
{
    /// <summary>
    /// Chat filter: "@text" goes to the admins only (anybody can reach them this way), gagged players
    /// cannot write. Chat commands ("!" and "/") always pass, so a gagged player can still use them.
    /// </summary>
    public partial class MiniAdmin
    {
        private void LoadChat()
        {
            AddCommandListener("say", OnChatMessage, HookMode.Pre);
            AddCommandListener("say_team", OnChatMessage, HookMode.Pre);
        }

        private void UnloadChat()
        {
            RemoveCommandListener("say", OnChatMessage, HookMode.Pre);
            RemoveCommandListener("say_team", OnChatMessage, HookMode.Pre);
        }

        private HookResult OnChatMessage(CCSPlayerController? player, CommandInfo info)
        {
            if (player is not { IsValid: true, IsBot: false })
            {
                return HookResult.Continue;
            }
            string message = info.ArgString.Trim();
            if (message.Length >= 2 && message[0] == '"' && message[^1] == '"')
            {
                message = message[1..^1].Trim();
            }
            if (message.Length == 0
                || message[0] is '!' or '/')
            {
                return HookResult.Continue;
            }
            if (message[0] == '@' && message.Length > 1)
            {
                SendAdminChat(player, message[1..].Trim());
                return HookResult.Handled;
            }
            if (IsPlayerGagged(player))
            {
                player.PrintToChat(Localizer["chat.gagged"]);
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }

        /// <summary>To every admin (any MiniAdmin permission) and the sender.</summary>
        private void SendAdminChat(CCSPlayerController sender, string text)
        {
            if (text.Length == 0)
            {
                return;
            }
            string line = Localizer["chat.admin"].Value
                .Replace("{player}", sender.PlayerName)
                .Replace("{message}", text);
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is { IsValid: true, IsBot: false }
                    && (player.Slot == sender.Slot || HasAnyMenuPermission(player)))
                {
                    player.PrintToChat(line);
                }
            }
        }

        /// <summary>Announcement in the middle of every screen, repeated in the chat.</summary>
        private bool CenterSay(string text, CCSPlayerController? admin = null)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                return false;
            }
            string sender = admin is { IsValid: true } ? admin.PlayerName : "Console";
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is { IsValid: true, IsBot: false })
                {
                    player.PrintToCenterAlert(text);
                }
            }
            Server.PrintToChatAll(Localizer["chat.csay"].Value
                .Replace("{player}", sender)
                .Replace("{message}", text));
            LogAction(admin, "csay", string.Empty, string.Empty, text);
            return true;
        }
    }
}
