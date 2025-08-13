using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        [ConsoleCommand("kick", "kick a player")]
        [RequiresPermissions("@miniadmin/kick")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "<player>")]
        public void CommandKick(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                {
                    _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = KickPlayer(entry); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV
                        && p.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    players.Add(entry);
                }

                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    _ = KickPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = KickPlayer(entry); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("ban", "ban a player")]
        [RequiresPermissions("@miniadmin/ban")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "<player>")]
        public void CommandBan(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                {
                    _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = BanPlayer(entry); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV
                        && p.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    players.Add(entry);
                }

                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    _ = BanPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = BanPlayer(entry); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("unban", "unban a player")]
        [RequiresPermissions("@miniadmin/ban")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "<player>")]
        public void CommandUnban(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (KeyValuePair<ulong, Dictionary<string, string>> kvp in Config.BannedPlayers)
                {
                    _ = menu.AddMenuOption(kvp.Value.TryGetValue("name", out string? name) ? name + $" ({kvp.Key})" : kvp.Key.ToString(), (_, _) => { _ = UnbanPlayer(kvp.Key); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<ulong> players = [];
                foreach (KeyValuePair<ulong, Dictionary<string, string>> kvp in Config.BannedPlayers.Where(
                    kvp => kvp.Key.ToString().Equals(playerName, StringComparison.OrdinalIgnoreCase)
                    || (kvp.Value.TryGetValue("name", out string? name) && name.Contains(playerName, StringComparison.CurrentCultureIgnoreCase))))
                {
                    players.Add(kvp.Key);
                }

                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    _ = UnbanPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (ulong entry in players)
                    {
                        _ = menu.AddMenuOption(Config.BannedPlayers[entry].TryGetValue("name", out string? name) ? name + $" ({entry})" : entry.ToString(), (_, _) => { _ = UnbanPlayer(entry); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("mute", "mutes a player")]
        [RequiresPermissions("@miniadmin/mute")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player>")]
        public void CommandMute(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                {
                    _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = MutePlayer(entry); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV
                        && p.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    players.Add(entry);
                }

                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    _ = MutePlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        _ = menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { _ = MutePlayer(entry); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("unmute", "unmutes a player")]
        [RequiresPermissions("@miniadmin/mute")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player>")]
        public void CommandUnmute(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (KeyValuePair<ulong, Dictionary<string, string>> kvp in Config.MutedPlayers)
                {
                    _ = menu.AddMenuOption(kvp.Value.TryGetValue("name", out string? name) ? name + $" ({kvp.Key})" : kvp.Key.ToString(), (_, _) => { _ = UnmutePlayer(kvp.Key); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<ulong> players = [];
                foreach (KeyValuePair<ulong, Dictionary<string, string>> kvp in Config.MutedPlayers.Where(
                    kvp => kvp.Key.ToString().Equals(playerName, StringComparison.OrdinalIgnoreCase)
                    || (kvp.Value.TryGetValue("name", out string? name) && name.Contains(playerName, StringComparison.CurrentCultureIgnoreCase))))
                {
                    players.Add(kvp.Key);
                }

                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    _ = UnmutePlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (ulong entry in players)
                    {
                        _ = menu.AddMenuOption(Config.MutedPlayers[entry].TryGetValue("name", out string? name) ? name + $" ({entry})" : entry.ToString(), (_, _) => { _ = UnmutePlayer(entry); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("restart", "restarts the match")]
        [RequiresPermissions("@miniadmin/restart")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<delay>")]
        public void CommandRestart(CCSPlayerController player, CommandInfo command)
        {
            int delay = 3;
            if (command.ArgCount > 1 && int.TryParse(command.GetArg(1), out int parsedDelay) && parsedDelay >= 0)
            {
                delay = parsedDelay;
            }
            _ = RestartMatch(delay);
        }
    }
}
