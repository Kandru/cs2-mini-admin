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
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KickPlayer(kvp.Key); });
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
                    _ = KickPlayer(players.First());
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = KickPlayer(entry); });
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = BanPlayer(kvp.Key); });
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
                    _ = BanPlayer(players.First());
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = BanPlayer(entry); });
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
                    // create menu to choose player
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
                foreach (var kvp in _connectedPlayers)
                {
                    string name = kvp.Value["name"];
                    string steam_id = kvp.Value["steam_id"];
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = MutePlayer(kvp.Key); });
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
                    _ = MutePlayer(players.First());
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) => { _ = MutePlayer(entry); });
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

        [ConsoleCommand("fswitch", "forcefully switches the teams")]
        [RequiresPermissions("@miniadmin/switch")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player> <team>")]
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
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                    {
                        if (kvp.Key == null
                            || !kvp.Key.IsValid)
                        {
                            return;
                        }
                        CsTeam newTeam = kvp.Key.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                        kvp.Key.SwitchTeam(newTeam);
                    });
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
                    CsTeam newTeam = players.First().Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                    players.First().SwitchTeam(newTeam);
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                        {
                            if (entry == null
                                || !entry.IsValid)
                            {
                                return;
                            }
                            CsTeam newTeam = entry.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                            entry.SwitchTeam(newTeam);
                        });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

        [ConsoleCommand("switch", "switches the teams")]
        [RequiresPermissions("@miniadmin/switch")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0, usage: "<player> <team>")]
        public void CommandSwitch(CCSPlayerController player, CommandInfo command)
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
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                    {
                        if (kvp.Key == null
                            || !kvp.Key.IsValid)
                        {
                            return;
                        }
                        CsTeam newTeam = kvp.Key.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                        kvp.Key.ChangeTeam(newTeam);
                    });
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
                    CsTeam newTeam = players.First().Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                    players.First().ChangeTeam(newTeam);
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                        {
                            if (entry == null
                                || !entry.IsValid)
                            {
                                return;
                            }
                            CsTeam newTeam = entry.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                            entry.ChangeTeam(newTeam);
                        });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }

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
                    double time_online = Math.Round((Server.CurrentTime - float.Parse(kvp.Value["timestamp"]) / 60), 2);
                    _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                    {
                        if (kvp.Key == null
                            || !kvp.Key.IsValid)
                        {
                            return;
                        }
                        kvp.Key.Respawn();
                    });
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
                    players.First().Respawn();
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
                        double time_online = Math.Round((Server.CurrentTime - float.Parse(_connectedPlayers[entry]["timestamp"]) / 60), 2);
                        _ = menu.AddMenuOption($"{name} ({steam_id}, {time_online} min online)", (_, _) =>
                        {
                            if (entry == null
                                || !entry.IsValid)
                            {
                                return;
                            }
                            entry.Respawn();
                        });
                    }
                    // show menu
                    MenuManager.OpenChatMenu(player, menu);
                }
            }
        }
    }
}
