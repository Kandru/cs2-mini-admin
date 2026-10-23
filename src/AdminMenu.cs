using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using MiniAdmin.Hud;

namespace MiniAdmin
{
    /// <summary>
    /// Admin menu opened with "!admin" (admin_v1.xml, own layout entity): status strip, team lists with a
    /// player card (moderation, team, player, items), server actions and settings, map change, the ban /
    /// mute / gag lists and the action log. Mouse driven: input capture while open, clicks arrive through
    /// <see cref="OnAdminMenuClicked"/>. Paints on change and once per second (two players per tick).
    /// </summary>
    public partial class MiniAdmin
    {
        private const float MenuOpenDelaySeconds = 0.2f;
        private const float MenuCaptureDelaySeconds = 0.2f;
        private const float MenuWarmUpDelaySeconds = 1.0f;
        private const float MenuRefreshSeconds = 1.0f;
        private const float MenuConfirmSeconds = 3.0f;
        private const int MenuPaintsPerTick = 2;
        private const int MenuSlots = HudLayout.MaxSlots;
        private const int MenuNameMax = 32;
        private const string MenuCredit = "made by: kandru.net";

        private const string PermKick = "@miniadmin/kick";
        private const string PermBan = "@miniadmin/ban";
        private const string PermMute = "@miniadmin/mute";
        private const string PermGag = "@miniadmin/gag";
        private const string PermSwitch = "@miniadmin/switch";
        private const string PermRespawn = "@miniadmin/respawn";
        private const string PermKill = "@miniadmin/kill";
        private const string PermGive = "@miniadmin/give";
        private const string PermPlayer = "@miniadmin/player";
        private const string PermRename = "@miniadmin/rename";
        private const string PermRestart = "@miniadmin/restart";
        private const string PermRound = "@miniadmin/round";
        private const string PermTeams = "@miniadmin/teams";
        private const string PermBots = "@miniadmin/bots";
        private const string PermRules = "@miniadmin/rules";
        private const string PermMap = "@miniadmin/map";
        private const string PermChat = "@miniadmin/chat";

        // any of these opens the menu and receives the admin chat; every button checks its own permission
        private static readonly string[] MenuPermissions =
        [
            PermKick, PermBan, PermMute, PermGag, PermSwitch, PermRespawn, PermKill, PermGive, PermPlayer, PermRename,
            PermRestart, PermRound, PermTeams, PermBots, PermRules, PermMap, PermChat
        ];

        private static readonly Dictionary<string, string> ItemLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["weapon_ak47"] = "AK-47",
            ["weapon_m4a1"] = "M4A4",
            ["weapon_m4a1_silencer"] = "M4A1-S",
            ["weapon_awp"] = "AWP",
            ["weapon_deagle"] = "Deagle",
            ["weapon_hegrenade"] = "HE",
            ["weapon_flashbang"] = "Flash",
            ["weapon_smokegrenade"] = "Smoke",
            ["weapon_molotov"] = "Molotov",
            ["weapon_incgrenade"] = "Incendiary",
            ["weapon_decoy"] = "Decoy",
            ["weapon_taser"] = "Zeus",
            ["weapon_healthshot"] = "Healthshot",
            ["item_assaultsuit"] = "Kevlar+H",
            ["item_kevlar"] = "Kevlar",
            ["item_defuser"] = "Defuser",
        };

        private static readonly string[] StatKeys = ["hp", "armor", "money", "kda", "damage", "hs", "mvp", "score", "ping", "online"];
        private static readonly string[] DetailPageKeys = ["mod", "team", "player", "items"];
        private static readonly string[] RecordKindKeys = ["bans", "mutes", "gags"];
        private static readonly string[] ServerHeadKeys = ["round", "warmup", "match", "teams", "rules"];

        private readonly AdminMenuView _menu = new();
        private readonly bool[] _menuOpen = new bool[MenuSlots];
        private readonly bool[] _menuDirty = new bool[MenuSlots];
        private readonly int[] _menuToken = new int[MenuSlots];
        private readonly AdminTab[] _menuTab = new AdminTab[MenuSlots];
        private readonly int[] _menuPage = new int[MenuSlots];
        private readonly int[,] _menuTeamPage = new int[MenuSlots, 2];
        // selected player by user id (a slot can be taken over by the next player); -1 = none
        private readonly int[] _menuTarget = NewFilled(MenuSlots, -1);
        private readonly DetailPage[] _menuDetailPage = new DetailPage[MenuSlots];
        private readonly bool[] _menuBanPick = new bool[MenuSlots];
        private readonly int[] _menuBanDuration = new int[MenuSlots];
        private readonly RecordKind[] _menuRecordKind = new RecordKind[MenuSlots];
        // button that shows "SURE?" and the game time it falls back
        private readonly string?[] _menuConfirm = new string?[MenuSlots];
        private readonly float[] _menuConfirmUntil = new float[MenuSlots];
        // what each pool slot showed in the last paint, so a click hits what the admin saw
        private readonly int[,] _menuRowUser = new int[MenuSlots, 2 * AdminMenuIds.TeamRows];
        private readonly int[,] _menuSpecUser = new int[MenuSlots, AdminMenuIds.SpecChips];
        private readonly string?[,] _menuMapShown = new string?[MenuSlots, AdminMenuIds.Maps];
        private readonly ulong[,] _menuRecordShown = new ulong[MenuSlots, AdminMenuIds.Records];
        private int _menuOpenCount;
        private float _menuNextRefresh;
        private float _menuNextConfirmExpiry = float.MaxValue;

        // paint scratch, reused (paints run only for open menus)
        private readonly List<CCSPlayerController> _menuPlayers = [];
        private readonly List<CCSPlayerController>[] _menuTeams = [[], [], []];
        private readonly List<PlayerRow> _menuRows = [];
        private readonly List<MapTile> _menuTiles = [];
        private readonly List<RecordRow> _menuRecords = [];
        private readonly List<LogRow> _menuLogRows = [];
        private readonly DetailModel _menuDetail = new();
        private readonly string[] _menuTabLabels = new string[AdminMenuIds.Tabs];
        private readonly string[] _menuTabCounts = new string[AdminMenuIds.Tabs];
        private readonly string[] _menuRecordKinds = new string[AdminMenuIds.RecordKinds];
        private readonly ButtonModel[] _menuServerButtons = new ButtonModel[AdminMenuIds.ServerKeys.Length];
        private readonly SwitchModel[] _menuSwitches = new SwitchModel[AdminMenuIds.SwitchKeys.Length];
        private readonly StepperModel[] _menuSettings = new StepperModel[AdminMenuIds.ServerStepKeys.Length];
        private readonly string[] _menuServerHeads = new string[AdminMenuIds.ServerHeadVars.Length];
        private readonly string[] _menuServerHints = new string[AdminMenuIds.ServerHintVars.Length];

        // ---- lifecycle --------------------------------------------------------------------------

        private void LoadAdminMenu(bool hotReload)
        {
            RegisterListener<Listeners.OnTick>(OnAdminMenuTick);
            RegisterListener<Listeners.CheckTransmit>(OnAdminMenuTransmit);
            RegisterListener<Listeners.OnCustomHudClicked>(OnAdminMenuClicked);
            RegisterListener<Listeners.OnMapStart>(OnAdminMenuMapStart);
            RegisterListener<Listeners.OnClientDisconnect>(OnAdminMenuDisconnect);
            RegisterEventHandler<EventRoundStart>(OnAdminMenuRoundStart);
            PurgeExpiredBans();
            if (hotReload)
            {
                // the map is already running: spawn the entity a moment in, never inside a command
                _ = AddTimer(MenuWarmUpDelaySeconds, () => _menu.Hud.EnsureEntity(), TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        private void UnloadAdminMenu()
        {
            RemoveListener<Listeners.OnTick>(OnAdminMenuTick);
            RemoveListener<Listeners.CheckTransmit>(OnAdminMenuTransmit);
            RemoveListener<Listeners.OnCustomHudClicked>(OnAdminMenuClicked);
            RemoveListener<Listeners.OnMapStart>(OnAdminMenuMapStart);
            RemoveListener<Listeners.OnClientDisconnect>(OnAdminMenuDisconnect);
            DeregisterEventHandler<EventRoundStart>(OnAdminMenuRoundStart);
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is { IsValid: true } && IsMenuOpen(player.Slot))
                {
                    _menu.Hud.SetInputCapture(player, false);
                }
            }
            ForgetAllMenus();
            _menu.Hud.Shutdown();
        }

        private void OnAdminMenuMapStart(string mapName)
        {
            ForgetAllMenus();
            _menu.Hud.ResetForMap();
            _menuNextRefresh = 0;
            _menuNextConfirmExpiry = float.MaxValue;
            _pauseRequested = false;
            ResetWarmupState();
            ResetEntityCache();
            PurgeExpiredBans();
            _ = AddTimer(MenuWarmUpDelaySeconds, () => _menu.Hud.EnsureEntity(), TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OnAdminMenuDisconnect(int slot)
        {
            ForgetMenu(slot);
            // other admins may have selected this player: their player card goes empty on the next paint
            MarkAllMenusDirty();
        }

        /// <summary>
        /// Round restart: the client rebuilt the layout from XML. Close and write the defaults back (ruleset §9),
        /// then bring an open menu back a timer later: an admin menu that vanishes every round is useless.
        /// It stays open on death on purpose, admins work from the spectator view.
        /// </summary>
        private HookResult OnAdminMenuRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is not { IsValid: true, IsBot: false, IsHLTV: false })
                {
                    continue;
                }
                bool wasOpen = IsMenuOpen(player.Slot);
                CloseMenu(player);
                _menu.Hud.RestoreDefaults(player);
                if (wasOpen)
                {
                    RequestMenuOpen(player.Slot);
                }
            }
            return HookResult.Continue;
        }

        private void OnAdminMenuTransmit(CCheckTransmitInfoList infoList) => _menu.Hud.FilterTransmit(infoList);

        private void OnAdminMenuTick()
        {
            _menu.Hud.Tick();
            TickWarmupExtension();
            if (_menuOpenCount == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            if (now >= _menuNextRefresh)
            {
                // stats, timers and alive states change all the time: refresh open menus once per second
                _menuNextRefresh = now + MenuRefreshSeconds;
                MarkAllMenusDirty();
            }

            if (now >= _menuNextConfirmExpiry)
            {
                ExpireConfirmations(now);
            }

            int budget = MenuPaintsPerTick;
            for (int slot = 0; slot < MenuSlots && budget > 0; slot++)
            {
                if (!_menuDirty[slot] || !_menuOpen[slot])
                {
                    continue;
                }
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                {
                    SafePaint(player);
                    budget--;
                }
                else
                {
                    _menuDirty[slot] = false;
                }
            }
        }

        // ---- open / close -----------------------------------------------------------------------

        private bool IsMenuOpen(int slot) => (uint)slot < MenuSlots && _menuOpen[slot];

        private static bool HasAnyMenuPermission(CCSPlayerController player)
        {
            foreach (string permission in MenuPermissions)
            {
                if (AdminManager.PlayerHasPermissions(player, permission))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Chat command: open (one timer later, never inside the command's tick) or close.</summary>
        private void ToggleMenu(CCSPlayerController player)
        {
            int slot = player.Slot;
            if ((uint)slot >= MenuSlots)
            {
                return;
            }
            if (_menuOpen[slot])
            {
                CloseMenu(player);
                return;
            }
            RequestMenuOpen(slot);
        }

        private void RequestMenuOpen(int slot)
        {
            int token = ++_menuToken[slot];
            _ = AddTimer(MenuOpenDelaySeconds, () =>
            {
                if (_menuToken[slot] != token
                    || Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } live
                    || !HasAnyMenuPermission(live)
                    || !_menu.Hud.EnsureEntity())
                {
                    return;
                }
                OpenMenu(live);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OpenMenu(CCSPlayerController player)
        {
            int slot = player.Slot;
            SetMenuOpen(slot, true);
            // tab, card page and selection survive reopening, pages start at 1
            _menuPage[slot] = 0;
            _menuTeamPage[slot, 0] = 0;
            _menuTeamPage[slot, 1] = 0;
            _menuBanPick[slot] = false;
            _menuConfirm[slot] = null;
            SafePaint(player);
            _menu.Hud.Show(player);

            // capture one timer after the first paint, so spawn/first write and capture are not in one burst
            int token = _menuToken[slot];
            _ = AddTimer(MenuCaptureDelaySeconds, () =>
            {
                if (_menuToken[slot] == token
                    && _menuOpen[slot]
                    && Utilities.GetPlayerFromSlot(slot) is { IsValid: true } live)
                {
                    _menu.Hud.SetInputCapture(live, true);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void CloseMenu(CCSPlayerController player)
        {
            int slot = player.Slot;
            if ((uint)slot >= MenuSlots)
            {
                return;
            }
            _menuToken[slot]++; // a pending open or capture sees a stale token and does nothing
            if (!_menuOpen[slot])
            {
                return;
            }
            SetMenuOpen(slot, false);
            _menu.Hud.SetInputCapture(player, false);
            _menu.Hud.Hide(player);
        }

        private void SetMenuOpen(int slot, bool open)
        {
            if (_menuOpen[slot] == open)
            {
                return;
            }
            _menuOpen[slot] = open;
            _menuDirty[slot] = false;
            _menuOpenCount += open ? 1 : -1;
        }

        private void ForgetMenu(int slot)
        {
            if ((uint)slot >= MenuSlots)
            {
                return;
            }
            _menuToken[slot]++;
            SetMenuOpen(slot, false);
            _menuTab[slot] = AdminTab.Players;
            _menuDetailPage[slot] = DetailPage.Moderation;
            _menuRecordKind[slot] = RecordKind.Bans;
            _menuTarget[slot] = -1;
            _menuBanPick[slot] = false;
            _menuBanDuration[slot] = 0;
            _menuConfirm[slot] = null;
            _menu.Hud.Forget(slot);
        }

        private void ForgetAllMenus()
        {
            for (int slot = 0; slot < MenuSlots; slot++)
            {
                _menuToken[slot]++;
                SetMenuOpen(slot, false);
                _menuConfirm[slot] = null;
                _menuBanPick[slot] = false;
            }
        }

        private void MarkAllMenusDirty()
        {
            if (_menuOpenCount == 0)
            {
                return;
            }
            for (int slot = 0; slot < MenuSlots; slot++)
            {
                _menuDirty[slot] |= _menuOpen[slot];
            }
        }

        // ---- confirmation ("SURE?") -------------------------------------------------------------

        /// <summary>First click arms the button for <see cref="MenuConfirmSeconds"/>; returns true while only armed.</summary>
        private bool NeedsConfirm(int slot, string buttonId, bool confirmed)
        {
            if (confirmed)
            {
                return false;
            }
            float until = Server.CurrentTime + MenuConfirmSeconds;
            _menuConfirm[slot] = buttonId;
            _menuConfirmUntil[slot] = until;
            _menuNextConfirmExpiry = Math.Min(_menuNextConfirmExpiry, until);
            return true;
        }

        private bool IsArmed(int slot, string buttonId) =>
            _menuConfirm[slot] is string armed && string.Equals(armed, buttonId, StringComparison.Ordinal);

        private void ExpireConfirmations(float now)
        {
            _menuNextConfirmExpiry = float.MaxValue;
            for (int slot = 0; slot < MenuSlots; slot++)
            {
                if (_menuConfirm[slot] == null)
                {
                    continue;
                }
                if (now < _menuConfirmUntil[slot])
                {
                    _menuNextConfirmExpiry = Math.Min(_menuNextConfirmExpiry, _menuConfirmUntil[slot]);
                    continue;
                }
                _menuConfirm[slot] = null;
                _menuDirty[slot] |= _menuOpen[slot];
            }
        }

        // ---- clicks -----------------------------------------------------------------------------

        /// <summary>Listener OnCustomHudClicked: only clicks on this layout from a player with the menu open.</summary>
        private void OnAdminMenuClicked(CCSPlayerController player, CCSCustomHudLayout layout, string buttonId)
        {
            if (player is not { IsValid: true }
                || !IsMenuOpen(player.Slot)
                || !_menu.Hud.Owns(layout))
            {
                return;
            }

            try
            {
                HandleMenuClick(player, buttonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MiniAdmin][AdminMenu] Click '{buttonId}' failed: {ex.Message}");
            }
        }

        private void HandleMenuClick(CCSPlayerController admin, string id)
        {
            int slot = admin.Slot;
            bool confirmed = IsArmed(slot, id) && Server.CurrentTime < _menuConfirmUntil[slot];
            // any click disarms a pending confirmation; the same button again runs it
            _menuConfirm[slot] = null;

            if (id == AdminMenuIds.CloseId)
            {
                CloseMenu(admin);
                return;
            }

            int index;
            if ((index = Array.IndexOf(AdminMenuIds.TabIds, id)) >= 0)
            {
                _menuTab[slot] = (AdminTab)index;
                _menuPage[slot] = 0;
                _menuBanPick[slot] = false;
            }
            else if ((index = Array.IndexOf(AdminMenuIds.ActionIds, id)) >= 0)
            {
                HandlePlayerAction(admin, (PlayerAction)index, id, confirmed);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.DetailTabIds, id)) >= 0)
            {
                _menuDetailPage[slot] = (DetailPage)index;
                _menuBanPick[slot] = false;
            }
            else if ((index = Array.IndexOf(AdminMenuIds.PlayerStepDecIds, id)) >= 0)
            {
                StepTargetValue(admin, (PlayerValue)index, -1);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.PlayerStepIncIds, id)) >= 0)
            {
                StepTargetValue(admin, (PlayerValue)index, 1);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.DurationIds, id)) >= 0)
            {
                _menuBanDuration[slot] = index;
            }
            else if ((index = Array.IndexOf(AdminMenuIds.ReasonIds, id)) >= 0)
            {
                BanTarget(admin, index);
            }
            else if (id == AdminMenuIds.ReasonCancelId)
            {
                _menuBanPick[slot] = false;
            }
            else if ((index = Array.IndexOf(AdminMenuIds.GiveIds, id)) >= 0)
            {
                GiveTarget(admin, index);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.ServerIds, id)) >= 0)
            {
                HandleServerAction(admin, (ServerAction)index, id, confirmed);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.SwitchIds, id)) >= 0)
            {
                HandleSwitch(admin, (ServerSwitch)index);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.ServerStepDecIds, id)) >= 0)
            {
                HandleSetting(admin, (ServerSetting)index, -1);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.ServerStepIncIds, id)) >= 0)
            {
                HandleSetting(admin, (ServerSetting)index, 1);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.MapIds, id)) >= 0)
            {
                HandleMap(admin, index, id, confirmed);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.RecordKindIds, id)) >= 0)
            {
                _menuRecordKind[slot] = (RecordKind)index;
                _menuPage[slot] = 0;
            }
            else if ((index = Array.IndexOf(AdminMenuIds.RecordButtonIds, id)) >= 0)
            {
                HandleRecord(admin, index, id, confirmed);
            }
            else if ((index = Array.IndexOf(AdminMenuIds.SpecIds, id)) >= 0)
            {
                SelectTarget(slot, _menuSpecUser[slot, index]);
            }
            else if (id is AdminMenuIds.MapPrevId or AdminMenuIds.RecordPrevId or AdminMenuIds.LogPrevId)
            {
                _menuPage[slot]--;
            }
            else if (id is AdminMenuIds.MapNextId or AdminMenuIds.RecordNextId or AdminMenuIds.LogNextId)
            {
                _menuPage[slot]++;
            }
            else if (!HandleTeamClick(slot, id))
            {
                return;
            }

            // feedback within the same tick: paint now, not on the next scheduler pass
            PaintMenu(admin);
        }

        private bool HandleTeamClick(int slot, string id)
        {
            for (int team = 0; team < 2; team++)
            {
                if (id == AdminMenuIds.TeamPrevIds[team])
                {
                    _menuTeamPage[slot, team]--;
                    return true;
                }
                if (id == AdminMenuIds.TeamNextIds[team])
                {
                    _menuTeamPage[slot, team]++;
                    return true;
                }
                int row = Array.IndexOf(AdminMenuIds.RowIds[team], id);
                if (row >= 0)
                {
                    SelectTarget(slot, _menuRowUser[slot, (team * AdminMenuIds.TeamRows) + row]);
                    return true;
                }
            }
            return false;
        }

        private void SelectTarget(int slot, int userId)
        {
            _menuTarget[slot] = userId;
            _menuBanPick[slot] = false;
        }

        private CCSPlayerController? MenuTarget(int slot)
        {
            int userId = _menuTarget[slot];
            if (userId < 0)
            {
                return null;
            }
            CCSPlayerController? target = Utilities.GetPlayerFromUserid(userId);
            if (target is not { IsValid: true, IsHLTV: false }
                || target.Connected != PlayerConnectedState.Connected)
            {
                _menuTarget[slot] = -1;
                _menuBanPick[slot] = false;
                return null;
            }
            return target;
        }

        private void HandlePlayerAction(CCSPlayerController admin, PlayerAction action, string id, bool confirmed)
        {
            int slot = admin.Slot;
            CCSPlayerController? target = MenuTarget(slot);
            if (target == null
                || !IsActionAllowed(admin, target, action))
            {
                return;
            }
            if (Array.IndexOf(AdminMenuView.ConfirmActions, action) >= 0
                && NeedsConfirm(slot, id, confirmed))
            {
                return;
            }

            switch (action)
            {
                case PlayerAction.Kick:
                    _ = KickPlayer(target, admin);
                    _menuTarget[slot] = -1;
                    break;
                case PlayerAction.Ban:
                    // duration and reason buttons are the confirmation
                    _menuBanPick[slot] = true;
                    break;
                case PlayerAction.Mute:
                    _ = IsPlayerMuted(target) ? UnmutePlayer(target.SteamID, admin) : MutePlayer(target, admin);
                    break;
                case PlayerAction.Gag:
                    _ = IsPlayerGagged(target) ? UngagPlayer(target.SteamID, admin) : GagPlayer(target, admin);
                    break;
                case PlayerAction.Rename:
                    _ = RenamePlayer(target, Localizer["menu.rename.default"].Value.Replace("{id}", NumberText.Of(target.UserId ?? 0)), admin);
                    break;
                case PlayerAction.ToT:
                    _ = SwitchPlayer(target, CsTeam.Terrorist, admin);
                    break;
                case PlayerAction.ToCt:
                    _ = SwitchPlayer(target, CsTeam.CounterTerrorist, admin);
                    break;
                case PlayerAction.ToSpec:
                    _ = SwitchPlayer(target, CsTeam.Spectator, admin);
                    break;
                case PlayerAction.ForceSwitch:
                    _ = ForceSwitchPlayer(target, admin);
                    break;
                case PlayerAction.Respawn:
                    _ = RespawnPlayer(target, admin);
                    break;
                case PlayerAction.Kill:
                    _ = KillPlayer(target, admin);
                    break;
                case PlayerAction.Slap:
                    _ = SlapPlayer(target, Config.Menu.SlapDamage, admin);
                    break;
                case PlayerAction.Freeze:
                    _ = ToggleFreeze(target, admin);
                    break;
                case PlayerAction.Noclip:
                    _ = ToggleNoclip(target, admin);
                    break;
                case PlayerAction.God:
                    _ = ToggleGod(target, admin);
                    break;
                case PlayerAction.Bring:
                    _ = TeleportPlayers(admin, target, bring: true);
                    break;
                case PlayerAction.Goto:
                    _ = TeleportPlayers(admin, target, bring: false);
                    break;
                case PlayerAction.Strip:
                    _ = StripPlayer(target, admin);
                    break;
                default:
                    break;
            }
            MarkAllMenusDirty();
        }

        private void StepTargetValue(CCSPlayerController admin, PlayerValue value, int direction)
        {
            CCSPlayerController? target = MenuTarget(admin.Slot);
            if (target == null
                || !CanSetValues(admin, target, value))
            {
                return;
            }
            int step = ValueSteps[(int)value].Step;
            int current = GetPlayerValue(target, value);
            // snap to the step grid first, so 87 HP goes to 100 or 75
            int next = direction > 0 ? ((current / step) + 1) * step : ((current + step - 1) / step - 1) * step;
            _ = SetPlayerValue(target, value, next, admin);
            MarkAllMenusDirty();
        }

        private int BanMinutes(int slot)
        {
            List<int> durations = Config.Menu.BanDurations;
            int index = _menuBanDuration[slot];
            return index < durations.Count ? Math.Max(0, durations[index]) : 0;
        }

        private void BanTarget(CCSPlayerController admin, int reason)
        {
            int slot = admin.Slot;
            CCSPlayerController? target = MenuTarget(slot);
            if (!_menuBanPick[slot]
                || target == null
                || !IsActionAllowed(admin, target, PlayerAction.Ban))
            {
                return;
            }
            _menuBanPick[slot] = false;
            _menuTarget[slot] = -1;
            // stored in the server language, so the ban list reads the same for every admin
            _ = BanPlayer(target, Localizer[$"menu.ban.reason{reason}"].Value, admin, BanMinutes(slot));
            MarkAllMenusDirty();
        }

        private void GiveTarget(CCSPlayerController admin, int index)
        {
            CCSPlayerController? target = MenuTarget(admin.Slot);
            List<string> items = Config.Menu.GiveItems;
            if (target == null
                || index >= items.Count
                || !CanGive(admin, target))
            {
                return;
            }
            _ = GivePlayer(target, items[index], admin);
        }

        private void HandleServerAction(CCSPlayerController admin, ServerAction action, string id, bool confirmed)
        {
            ServerState state = ReadServerState();
            if (!IsServerActionAllowed(admin, action, state))
            {
                return;
            }
            if (Array.IndexOf(AdminMenuView.ConfirmServer, action) >= 0
                && NeedsConfirm(admin.Slot, id, confirmed))
            {
                return;
            }

            switch (action)
            {
                case ServerAction.RestartRound:
                    _ = RestartRound(admin);
                    break;
                case ServerAction.RestartMatch:
                    _ = RestartMatch(3, admin);
                    break;
                case ServerAction.Pause:
                    _ = state.Paused || _pauseRequested ? UnpauseMatch(admin) : PauseMatch(admin);
                    break;
                case ServerAction.TimeoutT:
                    _ = StartTimeout(CsTeam.Terrorist, admin);
                    break;
                case ServerAction.TimeoutCt:
                    _ = StartTimeout(CsTeam.CounterTerrorist, admin);
                    break;
                case ServerAction.WarmupStart:
                    _ = StartWarmup(admin);
                    break;
                case ServerAction.WarmupEnd:
                    _ = EndWarmup(admin);
                    break;
                case ServerAction.WarmupExtend:
                    _ = ExtendWarmup(60, admin);
                    break;
                case ServerAction.SwapTeams:
                    _ = SwapTeams(admin);
                    break;
                case ServerAction.ScrambleTeams:
                    _ = ScrambleTeams(admin);
                    break;
                case ServerAction.BotT:
                    _ = AddBot(CsTeam.Terrorist, admin);
                    break;
                case ServerAction.BotCt:
                    _ = AddBot(CsTeam.CounterTerrorist, admin);
                    break;
                case ServerAction.BotKick:
                    _ = KickBots(admin);
                    break;
                default:
                    break;
            }
            MarkAllMenusDirty();
        }

        private void HandleSwitch(CCSPlayerController admin, ServerSwitch rule)
        {
            ServerState state = ReadServerState();
            if (!IsSwitchAllowed(admin, rule, state))
            {
                return;
            }
            _ = SetRule(rule, !IsSwitchOn(rule), admin);
            MarkAllMenusDirty();
        }

        private void HandleSetting(CCSPlayerController admin, ServerSetting setting, int direction)
        {
            if (!Has(admin, PermRules))
            {
                return;
            }
            _ = StepSetting(setting, direction, admin);
        }

        private void HandleMap(CCSPlayerController admin, int index, string id, bool confirmed)
        {
            int slot = admin.Slot;
            string? entry = _menuMapShown[slot, index];
            if (entry == null
                || !Has(admin, PermMap)
                || IsCurrentMap(entry)
                || NeedsConfirm(slot, id, confirmed))
            {
                return;
            }
            _ = ChangeMap(entry, admin);
        }

        private void HandleRecord(CCSPlayerController admin, int index, string id, bool confirmed)
        {
            int slot = admin.Slot;
            RecordKind kind = _menuRecordKind[slot];
            ulong steamId = _menuRecordShown[slot, index];
            if (steamId == 0
                || !Has(admin, RecordPermission(kind))
                || NeedsConfirm(slot, id, confirmed))
            {
                return;
            }
            _ = kind switch
            {
                RecordKind.Bans => UnbanPlayer(steamId, admin),
                RecordKind.Mutes => UnmutePlayer(steamId, admin),
                _ => UngagPlayer(steamId, admin)
            };
            MarkAllMenusDirty();
        }

        // ---- rules: who may press what ----------------------------------------------------------

        private readonly record struct ServerState(bool HasRules, bool Warmup, bool Paused, bool Freeze, bool GameOver, int Bots);

        private static ServerState ReadServerState()
        {
            int bots = 0;
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is { IsValid: true, IsBot: true })
                {
                    bots++;
                }
            }
            CCSGameRules? rules = GetGameRules();
            return rules == null
                ? new ServerState(false, false, false, false, false, bots)
                : new ServerState(true, rules.WarmupPeriod, rules.MatchWaitingForResume, rules.FreezePeriod, rules.GamePhase == 5, bots);
        }

        private static bool CanTarget(CCSPlayerController admin, CCSPlayerController target) =>
            target.IsBot || target.Slot == admin.Slot || AdminManager.CanPlayerTarget(admin, target);

        private static bool IsPlaying(CCSPlayerController player) =>
            player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;

        private static bool IsAlive(CCSPlayerController player) => IsPlaying(player) && player.PawnIsAlive;

        private bool IsPlayerMuted(CCSPlayerController player) =>
            !player.IsBot && Config.MutedPlayers.ContainsKey(player.SteamID);

        private static string RecordPermission(RecordKind kind) => kind switch
        {
            RecordKind.Bans => PermBan,
            RecordKind.Mutes => PermMute,
            _ => PermGag
        };

        private bool IsActionAllowed(CCSPlayerController admin, CCSPlayerController target, PlayerAction action)
        {
            bool self = target.Slot == admin.Slot;
            bool targetable = CanTarget(admin, target);
            // sanctions only for other humans
            bool other = !self && !target.IsBot && targetable;
            bool alive = IsAlive(target);
            return action switch
            {
                PlayerAction.Kick => other && Has(admin, PermKick),
                PlayerAction.Ban => other && Has(admin, PermBan),
                PlayerAction.Mute => other && Has(admin, PermMute),
                PlayerAction.Gag => other && Has(admin, PermGag),
                PlayerAction.Rename => !self && targetable && Has(admin, PermRename),
                PlayerAction.ToT => targetable && target.Team != CsTeam.Terrorist && Has(admin, PermSwitch),
                PlayerAction.ToCt => targetable && target.Team != CsTeam.CounterTerrorist && Has(admin, PermSwitch),
                PlayerAction.ToSpec => targetable && target.Team != CsTeam.Spectator && Has(admin, PermSwitch),
                PlayerAction.ForceSwitch => targetable && IsPlaying(target) && Has(admin, PermSwitch),
                PlayerAction.Respawn => IsPlaying(target) && !target.PawnIsAlive && Has(admin, PermRespawn),
                PlayerAction.Kill => targetable && alive && Has(admin, PermKill),
                PlayerAction.Slap or PlayerAction.Freeze or PlayerAction.Noclip or PlayerAction.God or PlayerAction.Strip =>
                    targetable && alive && Has(admin, PermPlayer),
                // teleports need two living players
                PlayerAction.Bring or PlayerAction.Goto =>
                    !self && targetable && alive && IsAlive(admin) && Has(admin, PermPlayer),
                _ => false
            };
        }

        private bool CanSetValues(CCSPlayerController admin, CCSPlayerController target, PlayerValue value) =>
            CanTarget(admin, target)
            && (value == PlayerValue.Money ? IsPlaying(target) : IsAlive(target))
            && Has(admin, PermPlayer);

        private bool CanGive(CCSPlayerController admin, CCSPlayerController target) =>
            IsAlive(target) && Has(admin, PermGive);

        private bool IsServerActionAllowed(CCSPlayerController admin, ServerAction action, ServerState state) => action switch
        {
            ServerAction.RestartRound => state.HasRules && !state.Warmup && Has(admin, PermRound),
            ServerAction.RestartMatch => Has(admin, PermRestart),
            ServerAction.Pause => !state.Warmup && Has(admin, PermRound),
            ServerAction.TimeoutT or ServerAction.TimeoutCt => !state.Warmup && Has(admin, PermRound),
            ServerAction.WarmupStart => state.HasRules && !state.Warmup && Has(admin, PermRound),
            ServerAction.WarmupEnd or ServerAction.WarmupExtend => state.Warmup && Has(admin, PermRound),
            ServerAction.SwapTeams or ServerAction.ScrambleTeams => Has(admin, PermTeams),
            ServerAction.BotT or ServerAction.BotCt => Has(admin, PermBots),
            ServerAction.BotKick => state.Bots > 0 && Has(admin, PermBots),
            _ => false
        };

        private bool IsSwitchAllowed(CCSPlayerController admin, ServerSwitch rule, ServerState state) => rule == ServerSwitch.WarmupHold
            ? state.Warmup && Has(admin, PermRound)
            : Has(admin, PermRules);

        private static bool IsCurrentMap(string entry)
        {
            (string map, string label, bool workshop) = ParseMapEntry(entry);
            string current = Server.MapName;
            return workshop
                ? string.Equals(label, current, StringComparison.OrdinalIgnoreCase)
                : string.Equals(map, current, StringComparison.OrdinalIgnoreCase);
        }

        // ---- paint ------------------------------------------------------------------------------

        private string MenuText(CCSPlayerController player, string key) => Localizer.ForPlayer(player, key);

        /// <summary>Scheduler and open: a paint that throws is logged, the next refresh tries again.</summary>
        private void SafePaint(CCSPlayerController player)
        {
            try
            {
                PaintMenu(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MiniAdmin][AdminMenu] Paint failed: {ex.Message}");
            }
        }

        // permission answers of the admin being painted: one AdminManager lookup per permission and paint
        private int _permSlot = -1;
        private readonly Dictionary<string, bool> _permCache = new(StringComparer.Ordinal);

        private bool Has(CCSPlayerController admin, string permission)
        {
            if (_permSlot != admin.Slot)
            {
                return AdminManager.PlayerHasPermissions(admin, permission);
            }
            if (!_permCache.TryGetValue(permission, out bool has))
            {
                _permCache[permission] = has = AdminManager.PlayerHasPermissions(admin, permission);
            }
            return has;
        }

        private void PaintMenu(CCSPlayerController player)
        {
            _permSlot = player.Slot;
            _permCache.Clear();
            try
            {
                PaintMenuCore(player);
            }
            finally
            {
                _permSlot = -1;
            }
        }

        private void PaintMenuCore(CCSPlayerController player)
        {
            int slot = player.Slot;
            _menuDirty[slot] = false;
            string T(string key) => MenuText(player, key);

            ServerState state = ReadServerState();
            AdminTab tab = _menuTab[slot];

            _menu.PaintFrame(player,
                T("menu.title"),
                FindCvar("hostname")?.StringValue ?? string.Empty,
                T("menu.nav"),
                T("menu.hint"),
                MenuCredit,
                T("menu.sure"));
            PaintStatus(player, state, T);

            CollectPlayers();
            for (int t = 0; t < AdminMenuIds.Tabs; t++)
            {
                _menuTabLabels[t] = T($"menu.tab.{TabKey((AdminTab)t)}");
            }
            _menuTabCounts[(int)AdminTab.Players] = NumberText.Of(_menuPlayers.Count);
            _menuTabCounts[(int)AdminTab.Server] = string.Empty;
            _menuTabCounts[(int)AdminTab.Maps] = NumberText.Of(Config.Menu.Maps.Count);
            _menuTabCounts[(int)AdminTab.Records] = NumberText.Of(Config.BannedPlayers.Count + Config.MutedPlayers.Count + Config.GaggedPlayers.Count);
            _menuTabCounts[(int)AdminTab.Log] = NumberText.Of(_actionLog.Count);
            _menu.PaintTabs(player, tab, _menuTabLabels, _menuTabCounts);

            switch (tab)
            {
                case AdminTab.Players:
                    PaintPlayersTab(player, T);
                    break;
                case AdminTab.Server:
                    PaintServerTab(player, state, T);
                    break;
                case AdminTab.Maps:
                    PaintMapsTab(player, T);
                    break;
                case AdminTab.Records:
                    PaintRecordsTab(player, T);
                    break;
                case AdminTab.Log:
                    PaintLogTab(player, T);
                    break;
                default:
                    break;
            }
        }

        private void PaintStatus(CCSPlayerController player, ServerState state, Func<string, string> T)
        {
            CCSGameRules? rules = GetGameRules();
            float now = Server.CurrentTime;
            MatchPhase phase;
            string phaseText;
            string time = string.Empty;
            if (rules == null)
            {
                phase = MatchPhase.Live;
                phaseText = "-";
            }
            else if (state.GameOver)
            {
                phase = MatchPhase.End;
                phaseText = T("menu.phase.end");
            }
            else if (state.Warmup)
            {
                phase = MatchPhase.Warmup;
                phaseText = T("menu.phase.warmup");
                time = _warmupHold || _warmupExtendUntil > 0
                    ? T("menu.phase.held")
                    : Clock(rules.WarmupPeriodEnd - now);
            }
            else if (state.Paused && state.Freeze)
            {
                phase = MatchPhase.Pause;
                phaseText = T("menu.phase.pause");
            }
            else if (state.Paused || _pauseRequested)
            {
                // mp_pause_match waits for the next freeze time
                phase = MatchPhase.Pause;
                phaseText = T("menu.phase.pausenext");
                time = Clock(rules.RoundStartTime + rules.RoundTime - now);
            }
            else if (state.Freeze)
            {
                phase = MatchPhase.Freeze;
                phaseText = T("menu.phase.freeze");
                time = Clock(rules.RoundStartTime - now);
            }
            else
            {
                phase = MatchPhase.Live;
                phaseText = T("menu.phase.live");
                time = Clock(rules.RoundStartTime + rules.RoundTime - now);
            }

            int humans = 0;
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                if (entry is { IsValid: true, IsBot: false, IsHLTV: false })
                {
                    humans++;
                }
            }

            _menu.PaintStatus(player, new StatusModel(
                T("menu.st.map"), Server.MapName,
                T("menu.st.round"), rules == null || state.Warmup ? "-" : NumberText.Of(rules.TotalRoundsPlayed + 1),
                T("menu.st.score"), NumberText.Of(GetTeamScore(CsTeam.Terrorist)), NumberText.Of(GetTeamScore(CsTeam.CounterTerrorist)),
                T("menu.st.phase"), phaseText, phase, time,
                T("menu.st.players"), state.Bots > 0
                    ? T("menu.st.playersbots").Replace("{humans}", NumberText.Of(humans)).Replace("{bots}", NumberText.Of(state.Bots))
                    : NumberText.Of(humans)));
        }

        /// <summary>Connected players (bots included) into _menuPlayers and per team, sorted like the scoreboard.</summary>
        private void CollectPlayers()
        {
            _menuPlayers.Clear();
            foreach (List<CCSPlayerController> team in _menuTeams)
            {
                team.Clear();
            }
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is not { IsValid: true, IsHLTV: false }
                    || player.Connected != PlayerConnectedState.Connected)
                {
                    continue;
                }
                _menuPlayers.Add(player);
                int team = player.Team switch
                {
                    CsTeam.Terrorist => 0,
                    CsTeam.CounterTerrorist => 1,
                    _ => 2
                };
                _menuTeams[team].Add(player);
            }
            foreach (List<CCSPlayerController> team in _menuTeams)
            {
                team.Sort(static (a, b) =>
                {
                    int byScore = b.Score.CompareTo(a.Score);
                    return byScore != 0 ? byScore : string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        private void PaintPlayersTab(CCSPlayerController admin, Func<string, string> T)
        {
            int slot = admin.Slot;
            CCSPlayerController? target = MenuTarget(slot);
            int targetUser = target?.UserId ?? -1;
            _menu.PaintPlayerTexts(admin, T("menu.tag.mute"), T("menu.tag.gag"), T("menu.team.empty"));

            for (int team = 0; team < 2; team++)
            {
                List<CCSPlayerController> members = _menuTeams[team];
                int pages = Math.Max(1, (members.Count + AdminMenuIds.TeamRows - 1) / AdminMenuIds.TeamRows);
                int page = _menuTeamPage[slot, team] = Math.Clamp(_menuTeamPage[slot, team], 0, pages - 1);
                _menuRows.Clear();
                for (int r = 0; r < AdminMenuIds.TeamRows; r++)
                {
                    int i = (page * AdminMenuIds.TeamRows) + r;
                    if (i >= members.Count)
                    {
                        _menuRowUser[slot, (team * AdminMenuIds.TeamRows) + r] = -1;
                        continue;
                    }
                    CCSPlayerController member = members[i];
                    _menuRowUser[slot, (team * AdminMenuIds.TeamRows) + r] = member.UserId ?? -1;
                    _menuRows.Add(BuildRow(member, targetUser, T));
                }
                _menu.PaintTeam(admin, team, T(team == 0 ? "menu.team.t" : "menu.team.ct"), NumberText.Of(members.Count), _menuRows, page, pages);
            }

            List<CCSPlayerController> spectators = _menuTeams[2];
            _menuRows.Clear();
            for (int s = 0; s < AdminMenuIds.SpecChips; s++)
            {
                if (s >= spectators.Count)
                {
                    _menuSpecUser[slot, s] = -1;
                    continue;
                }
                _menuSpecUser[slot, s] = spectators[s].UserId ?? -1;
                _menuRows.Add(BuildRow(spectators[s], targetUser, T));
            }
            int more = spectators.Count - AdminMenuIds.SpecChips;
            _menu.PaintSpectators(admin, T("menu.team.spec"), NumberText.Of(spectators.Count), _menuRows, more > 0 ? "+" + NumberText.Of(more) : string.Empty);

            if (target == null)
            {
                _menu.PaintDetailEmpty(admin, T("menu.detail.empty"));
                return;
            }
            PaintDetail(admin, target, T);
        }

        private PlayerRow BuildRow(CCSPlayerController player, int targetUser, Func<string, string> T)
        {
            CSMatchStats_t? stats = player.ActionTrackingServices?.MatchStats;
            string kda = stats == null
                ? "-"
                : NumberText.Of(stats.Kills) + "/" + NumberText.Of(stats.Deaths) + "/" + NumberText.Of(stats.Assists);
            string meta = player.IsBot ? T("menu.row.bot") : NumberText.Of((int)player.Ping) + "ms";
            return new PlayerRow(
                Shorten(player.PlayerName, MenuNameMax),
                kda,
                meta,
                IsPlaying(player) && !player.PawnIsAlive,
                IsPlayerMuted(player),
                IsPlayerGagged(player),
                (player.UserId ?? -2) == targetUser);
        }

        private void PaintDetail(CCSPlayerController admin, CCSPlayerController target, Func<string, string> T)
        {
            int slot = admin.Slot;
            DetailModel d = _menuDetail;
            CSMatchStats_t? stats = target.ActionTrackingServices?.MatchStats;
            CCSPlayerPawn? pawn = AlivePawn(target);
            bool alive = IsAlive(target);
            d.Team = target.Team switch
            {
                CsTeam.Terrorist => 0,
                CsTeam.CounterTerrorist => 1,
                _ => 2
            };
            string teamName = T(d.Team switch
            {
                0 => "menu.team.t",
                1 => "menu.team.ct",
                _ => "menu.team.spec"
            });
            string sub = (target.IsBot ? T("menu.row.bot") : target.SteamID.ToString()) + " · " + teamName;
            if (IsPlayerMuted(target))
            {
                sub += " · " + T("menu.tag.mute");
            }
            if (IsPlayerGagged(target))
            {
                sub += " · " + T("menu.tag.gag");
            }
            d.Name = Shorten(target.PlayerName, MenuNameMax);
            d.Sub = sub;

            for (int k = 0; k < AdminMenuIds.Stats; k++)
            {
                d.StatLabels[k] = T($"menu.stat.{StatKeys[k]}");
            }
            int kills = stats?.Kills ?? 0;
            int headshots = stats?.HeadShotKills ?? 0;
            d.StatValues[0] = alive ? NumberText.Of(target.PawnHealth > 0 ? (int)target.PawnHealth : 0) : "-";
            d.StatValues[1] = alive ? NumberText.Of((int)target.PawnArmor) : "-";
            d.StatValues[2] = target.InGameMoneyServices is { } money ? "$" + NumberText.Of(money.Account) : "-";
            d.StatValues[3] = stats == null ? "-" : NumberText.Of(kills) + "/" + NumberText.Of(stats.Deaths) + "/" + NumberText.Of(stats.Assists);
            d.StatValues[4] = stats == null ? "-" : NumberText.Of(stats.Damage);
            d.StatValues[5] = kills > 0 ? NumberText.Of(headshots) + " (" + NumberText.Of(headshots * 100 / kills) + "%)" : NumberText.Of(headshots);
            d.StatValues[6] = NumberText.Of(target.MVPs);
            d.StatValues[7] = NumberText.Of(target.Score);
            d.StatValues[8] = target.IsBot ? "-" : NumberText.Of((int)target.Ping) + "ms";
            d.StatValues[9] = OnlineText(target);

            d.Page = _menuDetailPage[slot];
            for (int p = 0; p < AdminMenuIds.DetailPages; p++)
            {
                d.PageLabels[p] = T($"menu.dpage.{DetailPageKeys[p]}");
            }

            for (int a = 0; a < d.Actions.Length; a++)
            {
                PlayerAction action = (PlayerAction)a;
                string key = AdminMenuIds.ActionKeys[a];
                // toggles name what a click does next
                string label = action switch
                {
                    PlayerAction.Mute when IsPlayerMuted(target) => T("menu.act.unmute"),
                    PlayerAction.Gag when IsPlayerGagged(target) => T("menu.act.ungag"),
                    PlayerAction.Freeze when pawn != null && IsFrozen(pawn) => T("menu.act.unfreeze"),
                    PlayerAction.Noclip when pawn != null && IsNoclip(pawn) => T("menu.act.noclipoff"),
                    PlayerAction.God when pawn != null && !pawn.TakesDamage => T("menu.act.godoff"),
                    _ => T($"menu.act.{key}")
                };
                d.Actions[a] = new ButtonModel(label, !IsActionAllowed(admin, target, action), IsArmed(slot, AdminMenuIds.ActionIds[a]));
            }

            for (int v = 0; v < d.Values.Length; v++)
            {
                PlayerValue value = (PlayerValue)v;
                bool allowed = CanSetValues(admin, target, value);
                int current = GetPlayerValue(target, value);
                string shown = !allowed && value != PlayerValue.Money && !alive
                    ? "-"
                    : (value == PlayerValue.Money ? "$" : string.Empty) + NumberText.Of(current);
                d.Values[v] = new StepperModel(
                    T($"menu.ps.{AdminMenuIds.PlayerStepKeys[v]}"),
                    shown,
                    !allowed || current <= ValueSteps[v].Min,
                    !allowed || current >= MaxPlayerValue(value));
            }

            List<string> items = Config.Menu.GiveItems;
            if (d.GiveLabels.Length != Math.Min(items.Count, AdminMenuIds.Give))
            {
                d.GiveLabels = new string[Math.Min(items.Count, AdminMenuIds.Give)];
            }
            for (int g = 0; g < d.GiveLabels.Length; g++)
            {
                d.GiveLabels[g] = ItemLabel(items[g]);
            }
            d.GiveDisabled = !CanGive(admin, target);

            d.BanPick = _menuBanPick[slot];
            d.BanHead = T("menu.ban.head").Replace("{player}", d.Name);
            d.DurationHead = T("menu.ban.duration");
            d.ReasonHead = T("menu.ban.reason");
            d.Cancel = T("menu.ban.cancel");
            List<int> durations = Config.Menu.BanDurations;
            for (int i = 0; i < AdminMenuIds.Durations; i++)
            {
                d.Durations[i] = DurationText(i < durations.Count ? durations[i] : 0, T);
            }
            d.Duration = Math.Clamp(_menuBanDuration[slot], 0, AdminMenuIds.Durations - 1);
            for (int r = 0; r < AdminMenuIds.Reasons; r++)
            {
                d.Reasons[r] = T($"menu.ban.reason{r}");
            }

            _menu.PaintDetail(admin, d);
        }

        private void PaintServerTab(CCSPlayerController admin, ServerState state, Func<string, string> T)
        {
            int slot = admin.Slot;
            for (int h = 0; h < ServerHeadKeys.Length; h++)
            {
                _menuServerHeads[h] = T($"menu.sv.head.{ServerHeadKeys[h]}");
                _menuServerHints[h] = T($"menu.sv.hint.{ServerHeadKeys[h]}");
            }
            if (_warmupExtendUntil > 0)
            {
                _menuServerHints[1] = T("menu.sv.hint.extended").Replace("{time}", Clock(_warmupExtendUntil - Server.CurrentTime));
            }

            for (int b = 0; b < _menuServerButtons.Length; b++)
            {
                ServerAction action = (ServerAction)b;
                string label = action == ServerAction.Pause
                    ? T(state.Paused || _pauseRequested ? "menu.sv.unpause" : "menu.sv.pause")
                    : T($"menu.sv.{AdminMenuIds.ServerKeys[b]}");
                _menuServerButtons[b] = new ButtonModel(label, !IsServerActionAllowed(admin, action, state), IsArmed(slot, AdminMenuIds.ServerIds[b]));
            }

            for (int s = 0; s < _menuSwitches.Length; s++)
            {
                ServerSwitch rule = (ServerSwitch)s;
                bool on = IsSwitchOn(rule);
                _menuSwitches[s] = new SwitchModel(T($"menu.sw.{AdminMenuIds.SwitchKeys[s]}"), T(on ? "menu.on" : "menu.off"), on, !IsSwitchAllowed(admin, rule, state));
            }

            bool rulesAllowed = Has(admin, PermRules);
            for (int s = 0; s < _menuSettings.Length; s++)
            {
                ServerSetting setting = (ServerSetting)s;
                float value = GetSetting(setting);
                (_, float step, float min, float max) = SettingSteps[s];
                _menuSettings[s] = new StepperModel(
                    T($"menu.sp.{AdminMenuIds.ServerStepKeys[s]}"),
                    SettingText(setting, value, T),
                    !rulesAllowed || value - step < min - 0.001f,
                    !rulesAllowed || value + step > max + 0.001f);
            }

            _menu.PaintServer(admin, _menuServerHeads, _menuServerHints, _menuServerButtons, _menuSwitches, _menuSettings);
        }

        private void PaintMapsTab(CCSPlayerController admin, Func<string, string> T)
        {
            int slot = admin.Slot;
            List<string> maps = Config.Menu.Maps;
            int pages = Math.Max(1, (maps.Count + AdminMenuIds.Maps - 1) / AdminMenuIds.Maps);
            int page = _menuPage[slot] = Math.Clamp(_menuPage[slot], 0, pages - 1);
            bool allowed = Has(admin, PermMap);
            _menuTiles.Clear();
            for (int m = 0; m < AdminMenuIds.Maps; m++)
            {
                int i = (page * AdminMenuIds.Maps) + m;
                if (i >= maps.Count)
                {
                    _menuMapShown[slot, m] = null;
                    continue;
                }
                string entry = maps[i];
                (_, string label, bool workshop) = ParseMapEntry(entry);
                bool current = IsCurrentMap(entry);
                _menuMapShown[slot, m] = entry;
                _menuTiles.Add(new MapTile(
                    label,
                    current ? T("menu.map.current") : workshop ? T("menu.map.workshop") : T("menu.map.official"),
                    current,
                    IsArmed(slot, AdminMenuIds.MapIds[m]),
                    !allowed));
            }
            _menu.PaintMaps(admin, T("menu.map.head"), T("menu.map.empty"), T(allowed ? "menu.map.hint" : "menu.nopermission"), _menuTiles, page, pages);
        }

        private Dictionary<ulong, Dictionary<string, string>> Records(RecordKind kind) => kind switch
        {
            RecordKind.Bans => Config.BannedPlayers,
            RecordKind.Mutes => Config.MutedPlayers,
            _ => Config.GaggedPlayers
        };

        // insertion order of each list; rebuilt only when a sanction changed (SaveConfig bumps the version)
        private int _recordsVersion;
        private readonly List<ulong>[] _recordKeys = [[], [], []];
        private readonly int[] _recordKeysVersion = [-1, -1, -1];

        private List<ulong> RecordKeys(RecordKind kind)
        {
            int k = (int)kind;
            if (_recordKeysVersion[k] != _recordsVersion)
            {
                _recordKeys[k].Clear();
                _recordKeys[k].AddRange(Records(kind).Keys);
                _recordKeysVersion[k] = _recordsVersion;
            }
            return _recordKeys[k];
        }

        private void PaintRecordsTab(CCSPlayerController admin, Func<string, string> T)
        {
            int slot = admin.Slot;
            RecordKind kind = _menuRecordKind[slot];
            for (int k = 0; k < AdminMenuIds.RecordKinds; k++)
            {
                _menuRecordKinds[k] = T($"menu.rec.{RecordKindKeys[k]}") + " " + NumberText.Of(Records((RecordKind)k).Count);
            }
            Dictionary<ulong, Dictionary<string, string>> records = Records(kind);
            bool allowed = Has(admin, RecordPermission(kind));
            int pages = Math.Max(1, (records.Count + AdminMenuIds.Records - 1) / AdminMenuIds.Records);
            int page = _menuPage[slot] = Math.Clamp(_menuPage[slot], 0, pages - 1);
            long now = UnixNow();

            // newest first: entries are appended to the config in the order they were made
            List<ulong> keys = RecordKeys(kind);
            int r = 0;
            _menuRecords.Clear();
            for (int i = keys.Count - 1 - (page * AdminMenuIds.Records); i >= 0 && r < AdminMenuIds.Records; i--)
            {
                if (!records.TryGetValue(keys[i], out Dictionary<string, string>? data))
                {
                    continue;
                }
                KeyValuePair<ulong, Dictionary<string, string>> record = new(keys[i], data);
                string name = data.TryGetValue("name", out string? stored) && stored.Length > 0 ? stored : record.Key.ToString();
                List<string> meta = [record.Key.ToString()];
                if (kind == RecordKind.Bans)
                {
                    if (data.TryGetValue("reason", out string? reason) && reason.Length > 0)
                    {
                        meta.Add(reason);
                    }
                    long until = BanUntil(data);
                    meta.Add(until <= 0
                        ? T("menu.dur.perm")
                        : until <= now
                            ? T("menu.rec.expired")
                            : T("menu.rec.until").Replace("{date}", DateTimeOffset.FromUnixTimeSeconds(until).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC"));
                }
                if (data.TryGetValue("date", out string? date) && date.Length > 0)
                {
                    meta.Add(date + " UTC");
                }
                if (data.TryGetValue("admin", out string? by) && by.Length > 0)
                {
                    meta.Add(T("menu.rec.by").Replace("{admin}", by));
                }
                _menuRecordShown[slot, r] = record.Key;
                _menuRecords.Add(new RecordRow(
                    ((page * AdminMenuIds.Records) + r + 1).ToString("00"),
                    Shorten(name, MenuNameMax),
                    string.Join(" · ", meta),
                    IsArmed(slot, AdminMenuIds.RecordButtonIds[r]),
                    !allowed));
                r++;
            }
            for (int i = r; i < AdminMenuIds.Records; i++)
            {
                _menuRecordShown[slot, i] = 0;
            }

            string key = RecordKindKeys[(int)kind];
            _menu.PaintRecords(admin, _menuRecordKinds, kind,
                T($"menu.rec.{key}.empty"),
                T($"menu.rec.{key}.button"),
                _menuRecords, page, pages);
        }

        private void PaintLogTab(CCSPlayerController admin, Func<string, string> T)
        {
            int slot = admin.Slot;
            int pages = Math.Max(1, (_actionLog.Count + AdminMenuIds.LogRows - 1) / AdminMenuIds.LogRows);
            int page = _menuPage[slot] = Math.Clamp(_menuPage[slot], 0, pages - 1);
            _menuLogRows.Clear();
            // newest first
            for (int r = 0; r < AdminMenuIds.LogRows; r++)
            {
                int i = _actionLog.Count - 1 - ((page * AdminMenuIds.LogRows) + r);
                if (i < 0)
                {
                    break;
                }
                LogEntry entry = _actionLog[i];
                string time = entry.Time.Date == DateTime.Today ? entry.Time.ToString("HH:mm:ss") : entry.Time.ToString("MM-dd HH:mm");
                _menuLogRows.Add(new LogRow(time, LogText(entry, T), DangerActions.Contains(entry.Action)));
            }
            _menu.PaintLog(admin, T("menu.log.head"), T("menu.log.empty"), _menuLogRows, page, pages);
        }

        // ---- helpers ----------------------------------------------------------------------------

        private string OnlineText(CCSPlayerController player)
        {
            if (player.IsBot
                || !_connectedPlayers.TryGetValue(player, out Dictionary<string, string>? data)
                || !data.TryGetValue("timestamp", out string? stamp)
                || !float.TryParse(stamp, out float since))
            {
                return "-";
            }
            return NumberText.Of((int)Math.Max(0, (Server.CurrentTime - since) / 60)) + " min";
        }

        private static string ItemLabel(string item)
        {
            if (ItemLabels.TryGetValue(item, out string? label))
            {
                return label;
            }
            string name = item.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase) ? item[7..]
                : item.StartsWith("item_", StringComparison.OrdinalIgnoreCase) ? item[5..]
                : item;
            return name.ToUpperInvariant();
        }

        private static string Clock(float seconds)
        {
            // right after a map start the warmup end is not set yet and lies far in the future
            if (float.IsNaN(seconds) || seconds > 24 * 3600)
            {
                return "-";
            }
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            return NumberText.Of(total / 60) + ":" + (total % 60).ToString("00");
        }

        private static string Shorten(string text, int max) =>
            text.Length <= max ? text : text[..(max - 1)] + "…";

        private static string TabKey(AdminTab tab) => tab switch
        {
            AdminTab.Players => "players",
            AdminTab.Server => "server",
            AdminTab.Maps => "maps",
            AdminTab.Records => "records",
            AdminTab.Log => "log",
            _ => "players"
        };

        private static int[] NewFilled(int length, int value)
        {
            int[] values = new int[length];
            Array.Fill(values, value);
            return values;
        }
    }
}
