using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using MiniAdmin.Hud;

namespace MiniAdmin
{
    public partial class MiniAdmin
    {
        // server cvars behind the rule switches, order = ServerSwitch (respawn sets both teams)
        private static readonly string[] SwitchCvars = ["mp_warmup_pausetimer", "mp_friendlyfire", "sv_alltalk", "sv_deadtalk", "mp_buy_anywhere", "mp_respawn_on_death_t"];
        private const string RespawnCtCvar = "mp_respawn_on_death_ct";

        // server steppers, order = ServerSetting: cvar, step, min, max
        private static readonly (string Cvar, float Step, float Min, float Max)[] SettingSteps =
        [
            ("mp_roundtime", 0.25f, 0.5f, 15f),
            ("mp_freezetime", 1f, 0f, 30f),
            ("mp_maxrounds", 1f, 0f, 60f),
            ("sv_gravity", 100f, 100f, 2000f),
            ("mp_limitteams", 1f, 0f, 10f),
        ];

        // mp_pause_match only pauses in the next freeze time: remember the request to show it
        private bool _pauseRequested;
        // "+1 min" holds the warmup timer (mp_warmup_pausetimer) until this game time; 0 = no extension
        private float _warmupExtendUntil;
        // the admin switched the warmup timer off: an ending extension must not resume it
        private bool _warmupHold;

        private static CCSGameRules? GetGameRules()
        {
            foreach (CCSGameRulesProxy proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                if (proxy.IsValid && proxy.GameRules != null)
                {
                    return proxy.GameRules;
                }
            }
            return null;
        }

        private static int GetTeamScore(CsTeam team)
        {
            foreach (CCSTeam entry in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
            {
                if (entry.IsValid && entry.TeamNum == (int)team)
                {
                    return entry.Score;
                }
            }
            return 0;
        }

        private static float CvarNumber(string name, float fallback = 0)
        {
            ConVar? cvar = ConVar.Find(name);
            if (cvar == null)
            {
                return fallback;
            }
            return cvar.Type switch
            {
                ConVarType.Bool => cvar.GetPrimitiveValue<bool>() ? 1 : 0,
                ConVarType.Int32 => cvar.GetPrimitiveValue<int>(),
                ConVarType.UInt32 => cvar.GetPrimitiveValue<uint>(),
                ConVarType.Float32 => cvar.GetPrimitiveValue<float>(),
                _ => fallback
            };
        }

        private static bool IsCvarOn(string name) => CvarNumber(name) != 0;

        /// <summary>Executes a server command and repaints open menus once the command buffer ran it.</summary>
        private void ExecuteAndRefresh(string command)
        {
            Server.ExecuteCommand(command);
            _ = AddTimer(0.1f, MarkAllMenusDirty, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private bool RestartRound(CCSPlayerController? admin = null)
        {
            CCSGameRules? rules = GetGameRules();
            if (rules == null
                || rules.WarmupPeriod)
            {
                return false;
            }
            // draw: the round is replayed without a point for either team
            rules.TerminateRound(3.0f, RoundEndReason.RoundDraw);
            LogAction(admin, "rr", string.Empty, string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.restartround"]);
            return true;
        }

        private bool PauseMatch(CCSPlayerController? admin = null)
        {
            ExecuteAndRefresh("mp_pause_match");
            _pauseRequested = true;
            LogAction(admin, "pause", string.Empty, string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.pause"]);
            return true;
        }

        private bool UnpauseMatch(CCSPlayerController? admin = null)
        {
            ExecuteAndRefresh("mp_unpause_match");
            _pauseRequested = false;
            LogAction(admin, "unpause", string.Empty, string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.unpause"]);
            return true;
        }

        /// <summary>Tactical timeout of one team; the game starts it in the next freeze time.</summary>
        private bool StartTimeout(CsTeam team, CCSPlayerController? admin = null)
        {
            if (team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                return false;
            }
            ExecuteAndRefresh(team == CsTeam.Terrorist ? "timeout_terrorist_start" : "timeout_ct_start");
            LogAction(admin, "timeout", team == CsTeam.Terrorist ? "T" : "CT", string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.timeout"].Value
                .Replace("{team}", team == CsTeam.Terrorist ? "T" : "CT"));
            return true;
        }

        private bool StartWarmup(CCSPlayerController? admin = null)
        {
            ResetWarmupState();
            ExecuteAndRefresh("mp_warmup_start");
            LogAction(admin, "warmup", string.Empty, string.Empty, "start");
            // announce to all
            Server.PrintToChatAll(Localizer["command.warmupstart"]);
            return true;
        }

        private bool EndWarmup(CCSPlayerController? admin = null)
        {
            if (GetGameRules() is not { WarmupPeriod: true })
            {
                return false;
            }
            ResetWarmupState();
            Server.ExecuteCommand("mp_warmup_pausetimer 0");
            ExecuteAndRefresh("mp_warmup_end");
            LogAction(admin, "warmup", string.Empty, string.Empty, "end");
            // announce to all
            Server.PrintToChatAll(Localizer["command.warmupend"]);
            return true;
        }

        /// <summary>
        /// Extends the warmup by holding its timer: changing mp_warmuptime would restart the warmup,
        /// mp_warmup_pausetimer freezes the remaining time instead. Repeated calls add up.
        /// </summary>
        private bool ExtendWarmup(int seconds = 60, CCSPlayerController? admin = null)
        {
            if (seconds <= 0
                || GetGameRules() is not { WarmupPeriod: true })
            {
                return false;
            }
            float now = Server.CurrentTime;
            _warmupExtendUntil = Math.Max(_warmupExtendUntil, now) + seconds;
            ExecuteAndRefresh("mp_warmup_pausetimer 1");
            LogAction(admin, "warmup", string.Empty, string.Empty, "+" + seconds + " s");
            // announce to all
            Server.PrintToChatAll(Localizer["command.warmupextend"].Value
                .Replace("{seconds}", seconds.ToString()));
            return true;
        }

        private bool SetWarmupHold(bool hold, CCSPlayerController? admin = null)
        {
            if (GetGameRules() is not { WarmupPeriod: true })
            {
                return false;
            }
            _warmupHold = hold;
            if (hold || _warmupExtendUntil == 0)
            {
                ExecuteAndRefresh($"mp_warmup_pausetimer {(hold ? 1 : 0)}");
            }
            LogAction(admin, "warmup", string.Empty, string.Empty, hold ? "hold" : "resume");
            // announce to all
            Server.PrintToChatAll(Localizer[hold ? "command.warmuphold" : "command.warmupresume"]);
            return true;
        }

        /// <summary>Per tick: one comparison while no warmup extension runs.</summary>
        private void TickWarmupExtension()
        {
            if (_warmupExtendUntil == 0
                || Server.CurrentTime < _warmupExtendUntil)
            {
                return;
            }
            _warmupExtendUntil = 0;
            if (!_warmupHold)
            {
                ExecuteAndRefresh("mp_warmup_pausetimer 0");
            }
        }

        private void ResetWarmupState()
        {
            _warmupExtendUntil = 0;
            _warmupHold = false;
        }

        private bool SwapTeams(CCSPlayerController? admin = null)
        {
            ExecuteAndRefresh("mp_swapteams");
            LogAction(admin, "swap", string.Empty, string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.swapteams"]);
            return true;
        }

        private bool ScrambleTeams(CCSPlayerController? admin = null)
        {
            ExecuteAndRefresh("mp_scrambleteams");
            LogAction(admin, "scramble", string.Empty, string.Empty);
            // announce to all
            Server.PrintToChatAll(Localizer["command.scrambleteams"]);
            return true;
        }

        private bool AddBot(CsTeam team, CCSPlayerController? admin = null)
        {
            if (team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                return false;
            }
            ExecuteAndRefresh(team == CsTeam.Terrorist ? "bot_add_t" : "bot_add_ct");
            LogAction(admin, "bot", team == CsTeam.Terrorist ? "T" : "CT", string.Empty, "+1");
            return true;
        }

        private bool KickBots(CCSPlayerController? admin = null)
        {
            ExecuteAndRefresh("bot_kick");
            LogAction(admin, "bot", string.Empty, string.Empty, "kick");
            // announce to all
            Server.PrintToChatAll(Localizer["command.kickbots"]);
            return true;
        }

        private bool IsSwitchOn(ServerSwitch rule) => rule switch
        {
            ServerSwitch.WarmupHold => _warmupHold,
            ServerSwitch.Respawn => IsCvarOn(SwitchCvars[(int)rule]) && IsCvarOn(RespawnCtCvar),
            _ => IsCvarOn(SwitchCvars[(int)rule])
        };

        private bool SetRule(ServerSwitch rule, bool on, CCSPlayerController? admin = null)
        {
            if (rule == ServerSwitch.WarmupHold)
            {
                return SetWarmupHold(on, admin);
            }
            int value = on ? 1 : 0;
            if (rule == ServerSwitch.Respawn)
            {
                Server.ExecuteCommand($"{RespawnCtCvar} {value}");
            }
            ExecuteAndRefresh($"{SwitchCvars[(int)rule]} {value}");
            string name = Localizer[$"menu.sw.{AdminMenuIds.SwitchKeys[(int)rule]}"].Value;
            LogAction(admin, "rule", name, string.Empty, on ? "on" : "off");
            // announce to all
            Server.PrintToChatAll(Localizer["command.rule"].Value
                .Replace("{rule}", name)
                .Replace("{state}", Localizer[on ? "menu.on" : "menu.off"].Value));
            return true;
        }

        /// <summary>Bomb and hostage maps read their own round time when it is set (mp_roundtime_defuse / _hostage).</summary>
        private static string RoundTimeCvar()
        {
            string map = Server.MapName;
            if (map.StartsWith("de_", StringComparison.OrdinalIgnoreCase) && CvarNumber("mp_roundtime_defuse") > 0)
            {
                return "mp_roundtime_defuse";
            }
            if (map.StartsWith("cs_", StringComparison.OrdinalIgnoreCase) && CvarNumber("mp_roundtime_hostage") > 0)
            {
                return "mp_roundtime_hostage";
            }
            return "mp_roundtime";
        }

        private static float GetSetting(ServerSetting setting) =>
            CvarNumber(setting == ServerSetting.RoundTime ? RoundTimeCvar() : SettingSteps[(int)setting].Cvar);

        /// <summary>One stepper click: moves the value by one step inside its limits.</summary>
        private bool StepSetting(ServerSetting setting, int direction, CCSPlayerController? admin = null)
        {
            (string cvar, float step, float min, float max) = SettingSteps[(int)setting];
            float current = GetSetting(setting);
            float next = Math.Clamp((MathF.Round(current / step) + Math.Sign(direction)) * step, min, max);
            if (next == current)
            {
                return false;
            }
            string value = next.ToString(setting == ServerSetting.RoundTime ? "0.##" : "0", CultureInfo.InvariantCulture);
            if (setting == ServerSetting.RoundTime)
            {
                // all three, so the next round uses the value on every map type
                Server.ExecuteCommand($"mp_roundtime_defuse {value}");
                Server.ExecuteCommand($"mp_roundtime_hostage {value}");
            }
            ExecuteAndRefresh($"{cvar} {value}");
            string name = Localizer[$"menu.sp.{AdminMenuIds.ServerStepKeys[(int)setting]}"].Value;
            string shown = SettingText(setting, next, key => Localizer[key].Value);
            LogAction(admin, "setting", name, string.Empty, shown);
            // announce to all
            Server.PrintToChatAll(Localizer["command.setting"].Value
                .Replace("{setting}", name)
                .Replace("{value}", shown));
            return true;
        }

        private static string SettingText(ServerSetting setting, float value, Func<string, string> text) => setting switch
        {
            ServerSetting.RoundTime => Clock(value * 60),
            ServerSetting.FreezeTime => ((int)value).ToString() + " s",
            ServerSetting.MaxRounds => value <= 0 ? "∞" : ((int)value).ToString(),
            ServerSetting.TeamLimit => value <= 0 ? text("menu.off") : ((int)value).ToString(),
            _ => ((int)value).ToString()
        };

        /// <summary>Map entries: "de_dust2", or a workshop id with an optional label: "3070284539|Mocha".</summary>
        private static (string Map, string Label, bool Workshop) ParseMapEntry(string entry)
        {
            string[] parts = entry.Split('|', 2, StringSplitOptions.TrimEntries);
            string map = parts[0];
            bool workshop = map.Length > 0 && map.All(char.IsDigit);
            string label = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : map;
            return (map, label, workshop);
        }

        private bool ChangeMap(string entry, CCSPlayerController? admin = null)
        {
            (string map, string label, bool workshop) = ParseMapEntry(entry);
            if (map.Length == 0)
            {
                return false;
            }
            LogAction(admin, "map", label, string.Empty);
            // announce to all, change a moment later so the message is seen
            Server.PrintToChatAll(Localizer["command.changemap"].Value
                .Replace("{map}", label));
            _ = AddTimer(3.0f, () =>
            {
                Server.ExecuteCommand(workshop ? $"host_workshop_map {map}" : $"changelevel {map}");
            }, TimerFlags.STOP_ON_MAPCHANGE);
            return true;
        }
    }
}
