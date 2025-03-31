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
            if (playerName == null || playerName == "")
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                    menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { KickPlayer(entry); });
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
                        && p.PlayerName.ToLower().Contains(playerName.ToLower())))
                    players.Add(entry);
                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    KickPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    var menu = new ChatMenu(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                        menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { KickPlayer(entry); });
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
            if (playerName == null || playerName == "")
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                    menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { BanPlayer(entry); });
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
                        && p.PlayerName.ToLower().Contains(playerName.ToLower())))
                    players.Add(entry);
                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    BanPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    var menu = new ChatMenu(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                        menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { BanPlayer(entry); });
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
            if (playerName == null || playerName == "")
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["command.menu.title"]);
                // add menu options
                foreach (var kvp in Config.BannedPlayers)
                    menu.AddMenuOption(kvp.Value.TryGetValue("name", out var name) ? name + $" ({kvp.Key})" : kvp.Key, (_, _) => { UnbanPlayer(kvp.Key); });
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<string> players = [];
                foreach (var kvp in Config.BannedPlayers.Where(
                    kvp => kvp.Key.ToLower() == playerName.ToLower()
                    || (kvp.Value.TryGetValue("name", out var name) && name.ToLower().Contains(playerName.ToLower()))))
                    players.Add(kvp.Key);
                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    UnbanPlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    var menu = new ChatMenu(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (string entry in players)
                        menu.AddMenuOption(Config.BannedPlayers[entry].TryGetValue("name", out var name) ? name + $" ({entry})" : entry, (_, _) => { UnbanPlayer(entry); });
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
            if (playerName == null || playerName == "")
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["command.menu.title"]);
                // add menu options
                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => p.IsValid
                        && !p.IsBot
                        && !p.IsHLTV))
                    menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { MutePlayer(entry); });
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
                        && p.PlayerName.ToLower().Contains(playerName.ToLower())))
                    players.Add(entry);
                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    MutePlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    var menu = new ChatMenu(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                        menu.AddMenuOption($"{entry.PlayerName} ({entry.NetworkIDString})", (_, _) => { MutePlayer(entry); });
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
            if (playerName == null || playerName == "")
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["command.menu.title"]);
                // add menu options
                foreach (var kvp in Config.MutedPlayers)
                    menu.AddMenuOption(kvp.Value.TryGetValue("name", out var name) ? name + $" ({kvp.Key})" : kvp.Key, (_, _) => { UnmutePlayer(kvp.Key); });
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<string> players = [];
                foreach (var kvp in Config.MutedPlayers.Where(
                    kvp => kvp.Key.ToLower() == playerName.ToLower()
                    || (kvp.Value.TryGetValue("name", out var name) && name.ToLower().Contains(playerName.ToLower()))))
                    players.Add(kvp.Key);
                if (players.Count == 0)
                {
                    command.ReplyToCommand(Localizer["command.playernotfound"].Value
                        .Replace("{player}", playerName));
                }
                else if (players.Count == 1)
                {
                    UnmutePlayer(players.First());
                }
                else
                {
                    // create menu to choose map
                    var menu = new ChatMenu(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (string entry in players)
                        menu.AddMenuOption(Config.MutedPlayers[entry].TryGetValue("name", out var name) ? name + $" ({entry})" : entry, (_, _) => { UnmutePlayer(entry); });
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }
    }
}
