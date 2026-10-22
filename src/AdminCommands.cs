using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using MiniAdmin.Hud;

namespace MiniAdmin
{
    /// <summary>
    /// Commands of the admin menu era. Registered with the css_ prefix on purpose: ConsoleCommand("map") would
    /// take over the engine command of the same name. In the chat they work as !admin, !rr, !gag ...
    /// </summary>
    public partial class MiniAdmin
    {
        [ConsoleCommand("css_admin", "opens or closes the admin menu")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0)]
        public void CommandAdmin(CCSPlayerController? player, CommandInfo command)
        {
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false })
            {
                return;
            }
            if (!HasAnyMenuPermission(player))
            {
                command.ReplyToCommand(Localizer["menu.nopermission"]);
                return;
            }
            // close a chat menu of the other commands first
            MenuManager.CloseActiveMenu(player);
            ToggleMenu(player);
        }

        [ConsoleCommand("css_rr", "restarts the current round without a score change")]
        [RequiresPermissions("@miniadmin/round")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0)]
        public void CommandRestartRound(CCSPlayerController? player, CommandInfo command)
        {
            if (!RestartRound(player))
            {
                command.ReplyToCommand(Localizer["command.notinwarmup"]);
            }
        }

        [ConsoleCommand("css_pause", "pauses the match in the next freeze time")]
        [RequiresPermissions("@miniadmin/round")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0)]
        public void CommandPause(CCSPlayerController? player, CommandInfo command)
        {
            _ = PauseMatch(player);
        }

        [ConsoleCommand("css_unpause", "resumes the match")]
        [RequiresPermissions("@miniadmin/round")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0)]
        public void CommandUnpause(CCSPlayerController? player, CommandInfo command)
        {
            _ = UnpauseMatch(player);
        }

        [ConsoleCommand("css_timeout", "starts a tactical timeout of a team")]
        [RequiresPermissions("@miniadmin/round")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<t|ct>")]
        public void CommandTimeout(CCSPlayerController? player, CommandInfo command)
        {
            CsTeam team = command.GetArg(1).ToLowerInvariant() switch
            {
                "t" => CsTeam.Terrorist,
                "ct" => CsTeam.CounterTerrorist,
                _ => CsTeam.None
            };
            if (!StartTimeout(team, player))
            {
                command.ReplyToCommand(Localizer["command.timeoutusage"]);
            }
        }

        [ConsoleCommand("css_warmup", "controls the warmup")]
        [RequiresPermissions("@miniadmin/round")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<start|end|extend [seconds]|hold|resume>")]
        public void CommandWarmup(CCSPlayerController? player, CommandInfo command)
        {
            bool done = command.GetArg(1).ToLowerInvariant() switch
            {
                "start" => StartWarmup(player),
                "end" => EndWarmup(player),
                "extend" => ExtendWarmup(command.ArgCount > 2 && int.TryParse(command.GetArg(2), out int seconds) ? seconds : 60, player),
                "hold" => SetWarmupHold(true, player),
                "resume" => SetWarmupHold(false, player),
                _ => false
            };
            if (!done)
            {
                command.ReplyToCommand(Localizer["command.warmupusage"]);
            }
        }

        [ConsoleCommand("css_swapteams", "swaps the teams and restarts the match")]
        [RequiresPermissions("@miniadmin/teams")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0)]
        public void CommandSwapTeams(CCSPlayerController? player, CommandInfo command)
        {
            _ = SwapTeams(player);
        }

        [ConsoleCommand("css_scrambleteams", "scrambles the teams and restarts the match")]
        [RequiresPermissions("@miniadmin/teams")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 0)]
        public void CommandScrambleTeams(CCSPlayerController? player, CommandInfo command)
        {
            _ = ScrambleTeams(player);
        }

        [ConsoleCommand("css_map", "changes the map")]
        [RequiresPermissions("@miniadmin/map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<map|workshop id>")]
        public void CommandMap(CCSPlayerController? player, CommandInfo command)
        {
            _ = ChangeMap(command.GetArg(1), player);
        }

        [ConsoleCommand("css_tban", "bans an online player for a number of minutes")]
        [RequiresPermissions("@miniadmin/ban")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <minutes, 0 = permanent> [reason]")]
        public void CommandTimedBan(CCSPlayerController? player, CommandInfo command)
        {
            if (!int.TryParse(command.GetArg(2), out int minutes) || minutes < 0)
            {
                command.ReplyToCommand(Localizer["command.minutes"]);
                return;
            }
            if (FindSingleTarget(player, command, command.GetArg(1)) is not { } target)
            {
                return;
            }
            if (!target.IsBot && !CallerCanTarget(player, target))
            {
                command.ReplyToCommand(Localizer["command.immune"]);
                return;
            }
            _ = BanPlayer(target, Rest(command, 3, "Unknown"), player, minutes);
        }

        [ConsoleCommand("css_banid", "bans a SteamID64, also when the player is offline")]
        [RequiresPermissions("@miniadmin/ban")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<steamid64> [minutes, 0 = permanent] [reason]")]
        public void CommandBanId(CCSPlayerController? player, CommandInfo command)
        {
            if (!ulong.TryParse(command.GetArg(1), out ulong steamId))
            {
                command.ReplyToCommand(Localizer["command.steamid"]);
                return;
            }
            int minutes = 0;
            int reasonArg = 2;
            if (command.ArgCount > 2 && int.TryParse(command.GetArg(2), out int parsed) && parsed >= 0)
            {
                minutes = parsed;
                reasonArg = 3;
            }
            if (!BanSteamId(steamId, minutes, Rest(command, reasonArg, "Unknown"), player))
            {
                command.ReplyToCommand(Localizer["command.banidfailed"]);
            }
        }

        [ConsoleCommand("css_gag", "blocks the chat of a player")]
        [RequiresPermissions("@miniadmin/gag")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player>")]
        public void CommandGag(CCSPlayerController? player, CommandInfo command)
        {
            if (FindSingleTarget(player, command, command.GetArg(1)) is { } target
                && !GagPlayer(target, player))
            {
                command.ReplyToCommand(Localizer["command.failed"]);
            }
        }

        [ConsoleCommand("css_ungag", "allows the chat of a player again")]
        [RequiresPermissions("@miniadmin/gag")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player|steamid64>")]
        public void CommandUngag(CCSPlayerController? player, CommandInfo command)
        {
            string query = command.GetArg(1);
            ulong steamId = ulong.TryParse(query, out ulong parsed)
                ? parsed
                : Config.GaggedPlayers.FirstOrDefault(kvp => kvp.Value.TryGetValue("name", out string? name)
                    && name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).Key;
            if (!UngagPlayer(steamId, player))
            {
                command.ReplyToCommand(Localizer["command.playernotfound"].Value.Replace("{player}", query));
            }
        }

        [ConsoleCommand("css_slap", "slaps a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player> [damage]")]
        public void CommandSlap(CCSPlayerController? player, CommandInfo command)
        {
            int damage = command.ArgCount > 2 && int.TryParse(command.GetArg(2), out int parsed) ? Math.Max(0, parsed) : Config.Menu.SlapDamage;
            RunOnTarget(player, command, target => SlapPlayer(target, damage, player));
        }

        [ConsoleCommand("css_freeze", "freezes or unfreezes a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player>")]
        public void CommandFreeze(CCSPlayerController? player, CommandInfo command)
        {
            RunOnTarget(player, command, target => ToggleFreeze(target, player));
        }

        [ConsoleCommand("css_noclip", "toggles noclip of a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player>")]
        public void CommandNoclip(CCSPlayerController? player, CommandInfo command)
        {
            RunOnTarget(player, command, target => ToggleNoclip(target, player));
        }

        [ConsoleCommand("css_god", "toggles god mode of a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player>")]
        public void CommandGod(CCSPlayerController? player, CommandInfo command)
        {
            RunOnTarget(player, command, target => ToggleGod(target, player));
        }

        [ConsoleCommand("css_strip", "removes all weapons of a player (the knife stays)")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<player>")]
        public void CommandStrip(CCSPlayerController? player, CommandInfo command)
        {
            RunOnTarget(player, command, target => StripPlayer(target, player));
        }

        [ConsoleCommand("css_hp", "sets the health of a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <health>")]
        public void CommandHealth(CCSPlayerController? player, CommandInfo command) => SetValueCommand(player, command, PlayerValue.Health);

        [ConsoleCommand("css_armor", "sets the armor of a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <armor>")]
        public void CommandArmor(CCSPlayerController? player, CommandInfo command) => SetValueCommand(player, command, PlayerValue.Armor);

        [ConsoleCommand("css_money", "sets the money of a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <money>")]
        public void CommandMoney(CCSPlayerController? player, CommandInfo command) => SetValueCommand(player, command, PlayerValue.Money);

        [ConsoleCommand("css_rename", "renames a player")]
        [RequiresPermissions("@miniadmin/rename")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 2, usage: "<player> <new name>")]
        public void CommandRename(CCSPlayerController? player, CommandInfo command)
        {
            string name = Rest(command, 2, string.Empty);
            RunOnTarget(player, command, target => RenamePlayer(target, name, player));
        }

        [ConsoleCommand("css_bring", "teleports a player to you")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 1, usage: "<player>")]
        public void CommandBring(CCSPlayerController? player, CommandInfo command)
        {
            if (player is { IsValid: true })
            {
                RunOnTarget(player, command, target => TeleportPlayers(player, target, bring: true));
            }
        }

        [ConsoleCommand("css_goto", "teleports you to a player")]
        [RequiresPermissions("@miniadmin/player")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 1, usage: "<player>")]
        public void CommandGoto(CCSPlayerController? player, CommandInfo command)
        {
            if (player is { IsValid: true })
            {
                RunOnTarget(player, command, target => TeleportPlayers(player, target, bring: false));
            }
        }

        [ConsoleCommand("css_csay", "shows a message in the middle of every screen")]
        [RequiresPermissions("@miniadmin/chat")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER, minArgs: 1, usage: "<message>")]
        public void CommandCenterSay(CCSPlayerController? player, CommandInfo command)
        {
            _ = CenterSay(Rest(command, 1, string.Empty), player);
        }

        // ---- helpers ----------------------------------------------------------------------------

        private void SetValueCommand(CCSPlayerController? player, CommandInfo command, PlayerValue value)
        {
            if (!int.TryParse(command.GetArg(2), out int amount))
            {
                command.ReplyToCommand(Localizer["command.number"]);
                return;
            }
            RunOnTarget(player, command, target => SetPlayerValue(target, value, amount, player));
        }

        /// <summary>Resolves argument 1 to one player the caller may target and runs <paramref name="action"/>.</summary>
        private void RunOnTarget(CCSPlayerController? player, CommandInfo command, Func<CCSPlayerController, bool> action)
        {
            if (FindSingleTarget(player, command, command.GetArg(1)) is not { } target)
            {
                return;
            }
            if (!target.IsBot && !CallerCanTarget(player, target))
            {
                command.ReplyToCommand(Localizer["command.immune"]);
                return;
            }
            if (!action(target))
            {
                command.ReplyToCommand(Localizer["command.failed"]);
            }
        }

        private static bool CallerCanTarget(CCSPlayerController? admin, CCSPlayerController target) =>
            admin == null || CanTarget(admin, target);

        /// <summary>"#userid", a SteamID64 or part of the name; replies when there is no or more than one match.</summary>
        private CCSPlayerController? FindSingleTarget(CCSPlayerController? caller, CommandInfo command, string query)
        {
            List<CCSPlayerController> found = [];
            bool byUserId = query.StartsWith('#') && int.TryParse(query[1..], out _);
            bool bySteamId = ulong.TryParse(query, out ulong steamId);
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is not { IsValid: true, IsHLTV: false }
                    || player.Connected != PlayerConnectedState.Connected)
                {
                    continue;
                }
                if ((byUserId && "#" + player.UserId == query)
                    || (bySteamId && !player.IsBot && player.SteamID == steamId))
                {
                    return player;
                }
                if (!byUserId && player.PlayerName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    found.Add(player);
                }
            }
            if (found.Count == 1)
            {
                return found[0];
            }
            command.ReplyToCommand(found.Count == 0
                ? Localizer["command.playernotfound"].Value.Replace("{player}", query)
                : Localizer["command.ambiguous"].Value.Replace("{players}", string.Join(", ", found.Select(p => $"{p.PlayerName} (#{p.UserId})"))));
            return null;
        }

        /// <summary>Arguments from <paramref name="first"/> on, joined with spaces.</summary>
        private static string Rest(CommandInfo command, int first, string fallback)
        {
            if (command.ArgCount <= first)
            {
                return fallback;
            }
            string text = string.Join(' ', Enumerable.Range(first, command.ArgCount - first).Select(command.GetArg)).Trim();
            return text.Length > 0 ? text : fallback;
        }
    }
}
