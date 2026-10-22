using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        // css_<name> makes the chat trigger (!kick) work; the plain name stays for existing console use
        [ConsoleCommand("css_kick", "kick a player")]
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KickPlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = KickPlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KickPlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_ban", "ban a player")]
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = BanPlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = BanPlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = BanPlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_unban", "unban a player")]
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
                    _ = menu.AddMenuOption(kvp.Value.TryGetValue("name", out string? name) ? name + $" ({kvp.Key})" : kvp.Key.ToString(), (_, _) => { _ = UnbanPlayer(kvp.Key, player); });
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
                    _ = UnbanPlayer(players.First(), player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (ulong entry in players)
                    {
                        _ = menu.AddMenuOption(Config.BannedPlayers[entry].TryGetValue("name", out string? name) ? name + $" ({entry})" : entry.ToString(), (_, _) => { _ = UnbanPlayer(entry, player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_mute", "mutes a player")]
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = MutePlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = MutePlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = MutePlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_unmute", "unmutes a player")]
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
                    _ = menu.AddMenuOption(kvp.Value.TryGetValue("name", out string? name) ? name + $" ({kvp.Key})" : kvp.Key.ToString(), (_, _) => { _ = UnmutePlayer(kvp.Key, player); });
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
                    _ = UnmutePlayer(players.First(), player);
                }
                else
                {
                    // create menu to choose map
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (ulong entry in players)
                    {
                        _ = menu.AddMenuOption(Config.MutedPlayers[entry].TryGetValue("name", out string? name) ? name + $" ({entry})" : entry.ToString(), (_, _) => { _ = UnmutePlayer(entry, player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_restart", "restarts the match")]
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
            _ = RestartMatch(delay, player);
        }

        [ConsoleCommand("css_fswitch", "forcefully switches the teams")]
        [ConsoleCommand("fswitch", "forcefully switches the teams")]
        [RequiresPermissions("@miniadmin/switch")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player>")]
        public void CommandForceSwitch(CCSPlayerController player, CommandInfo command)
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = ForceSwitchPlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = ForceSwitchPlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = ForceSwitchPlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_switch", "switches the teams")]
        [ConsoleCommand("switch", "switches the teams")]
        [RequiresPermissions("@miniadmin/switch")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player> <team>")]
        public void CommandSwitch(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            // get team name
            string? teamName = command.GetArg(2);
            CsTeam newTeam = teamName?.ToLowerInvariant() switch
            {
                string s when s.Contains("ter") || s == "t" => CsTeam.Terrorist,
                string s when s.Contains("count") || s == "ct" => CsTeam.CounterTerrorist,
                string s when s.Contains("spec") || s == "s" => CsTeam.Spectator,
                _ => CsTeam.None
            };
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = SwitchPlayer(kvp.Key, newTeam, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = SwitchPlayer(players.First(), newTeam, admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = SwitchPlayer(entry, newTeam, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_respawn", "respawns a player")]
        [ConsoleCommand("respawn", "respawns a player")]
        [RequiresPermissions("@miniadmin/respawn")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player>")]
        public void CommandRespawn(CCSPlayerController player, CommandInfo command)
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = RespawnPlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = RespawnPlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = RespawnPlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_kill", "kills a player")]
        [ConsoleCommand("kill", "kills a player")]
        [RequiresPermissions("@miniadmin/kill")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player>")]
        public void CommandKill(CCSPlayerController player, CommandInfo command)
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KillPlayer(kvp.Key, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = KillPlayer(players.First(), admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KillPlayer(entry, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("css_give", "gives a player an item")]
        [ConsoleCommand("give", "gives a player an item")]
        [RequiresPermissions("@miniadmin/give")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <item>")]
        public void CommandGive(CCSPlayerController player, CommandInfo command)
        {
            // close menu
            MenuManager.CloseActiveMenu(player);
            // get player name / id / whatever
            string? playerName = command.GetArg(1);
            // get team name
            string? item = command.GetArg(2);
            if (playerName is null or "")
            {
                // create menu to choose map
                ChatMenu menu = new(Localizer["command.menu.title"]);
                // add menu options
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"])) / 60, 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = GivePlayer(kvp.Key, item, admin: player); });
                }
                // show menu
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                List<CCSPlayerController> players = [];
                foreach (var kvp in _connectedPlayers
                    .Where(kvp => kvp.Key.PlayerName.Contains(playerName, StringComparison.CurrentCultureIgnoreCase)
                        || kvp.Value["name"].Contains(playerName, StringComparison.CurrentCultureIgnoreCase)))
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
                    _ = GivePlayer(players.First(), item, admin: player);
                }
                else
                {
                    // create menu to choose player
                    ChatMenu menu = new(Localizer["command.menu.title"]);
                    // add menu options
                    foreach (CCSPlayerController entry in players)
                    {
                        string name = _connectedPlayers[entry]["name"];
                        string steam_id = _connectedPlayers[entry]["steam_id"];
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"])) / 60, 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = GivePlayer(entry, item, admin: player); });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }
    }
}
