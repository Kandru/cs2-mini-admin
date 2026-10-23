using CounterStrikeSharp.API.Core;

namespace MiniAdmin.Hud
{
    /// <summary>One entry of a team column or the spectator strip, already localized.</summary>
    public readonly record struct PlayerRow(string Name, string Stats, string Meta, bool Dead, bool Muted, bool Gagged, bool Selected);

    /// <summary>A button of the player card or the server view.</summary>
    public readonly record struct ButtonModel(string Label, bool Disabled, bool Confirm = false);

    public readonly record struct SwitchModel(string Label, string State, bool On, bool Disabled);

    /// <summary>‹ value › row; each side can be disabled at its limit or without permission.</summary>
    public readonly record struct StepperModel(string Label, string Value, bool DecDisabled, bool IncDisabled);

    public readonly record struct MapTile(string Name, string Sub, bool Current, bool Confirm, bool Disabled);

    public readonly record struct RecordRow(string Index, string Name, string Meta, bool Confirm, bool Disabled);

    public readonly record struct LogRow(string Time, string Text, bool Danger);

    /// <summary>Status strip above the views.</summary>
    public readonly record struct StatusModel(
        string MapHead, string Map,
        string RoundHead, string Round,
        string ScoreHead, string TScore, string CtScore,
        string PhaseHead, string Phase, MatchPhase PhaseKind, string Time,
        string PlayersHead, string Players);

    /// <summary>Player card content; one instance is reused by the controller for every paint.</summary>
    public sealed class DetailModel
    {
        public string Name = string.Empty;
        public string Sub = string.Empty;
        /// <summary>Index into <see cref="AdminMenuIds.Teams"/>.</summary>
        public int Team;
        public DetailPage Page;
        public readonly string[] PageLabels = new string[AdminMenuIds.DetailPages];
        public readonly string[] StatLabels = new string[AdminMenuIds.Stats];
        public readonly string[] StatValues = new string[AdminMenuIds.Stats];
        public readonly ButtonModel[] Actions = new ButtonModel[AdminMenuIds.ActionKeys.Length];
        public readonly StepperModel[] Values = new StepperModel[AdminMenuIds.PlayerStepKeys.Length];
        public string[] GiveLabels = [];
        public bool GiveDisabled;
        public bool BanPick;
        public string BanHead = string.Empty;
        public string DurationHead = string.Empty;
        public string ReasonHead = string.Empty;
        public string Cancel = string.Empty;
        public readonly string[] Durations = new string[AdminMenuIds.Durations];
        public int Duration;
        public readonly string[] Reasons = new string[AdminMenuIds.Reasons];
    }

    /// <summary>
    /// Paints admin_v1.xml from models. Knows ids only through <see cref="AdminMenuIds"/>; every write goes
    /// through the <see cref="HudLayout"/> diff cache, so painting the same model twice sends nothing.
    /// </summary>
    public sealed class AdminMenuView
    {
        /// <summary>Player actions whose button shows "SURE?" before it runs (MaBtnSure in the layout).</summary>
        public static readonly PlayerAction[] ConfirmActions = [PlayerAction.Kick, PlayerAction.Rename, PlayerAction.Kill, PlayerAction.Strip];

        /// <summary>Server actions whose button shows "SURE?" before it runs.</summary>
        public static readonly ServerAction[] ConfirmServer = [ServerAction.RestartMatch, ServerAction.SwapTeams, ServerAction.ScrambleTeams, ServerAction.BotKick];

        private readonly HudLayout _hud = new(AdminMenuIds.Resource, AdminMenuIds.RootId);

        private readonly TextSlot _title, _sub, _navHead, _hint, _credit, _sure, _tagMute, _tagGag, _teamEmpty;
        private readonly TextSlot _stMapH, _stMap, _stRoundH, _stRound, _stScoreH, _stT, _stCt;
        private readonly TextSlot _stPhaseH, _stPhase, _stTime, _stPlayersH, _stPlayers;
        private readonly ClassSlot _view, _phase;
        private readonly TextSlot[] _tabLabel = new TextSlot[AdminMenuIds.Tabs];
        private readonly TextSlot[] _tabCount = new TextSlot[AdminMenuIds.Tabs];
        private readonly ClassSlot[] _tabActive = new ClassSlot[AdminMenuIds.Tabs];

        private readonly TextSlot[] _teamHead = new TextSlot[2];
        private readonly TextSlot[] _teamCount = new TextSlot[2];
        private readonly TextSlot[] _teamPage = new TextSlot[2];
        private readonly ClassSlot[] _teamPagerOff = new ClassSlot[2];
        private readonly ClassSlot[] _teamPrevDisabled = new ClassSlot[2];
        private readonly ClassSlot[] _teamNextDisabled = new ClassSlot[2];
        private readonly ClassSlot[] _teamEmptyOff = new ClassSlot[2];
        private readonly TextSlot[][] _rowName = new TextSlot[2][];
        private readonly TextSlot[][] _rowStats = new TextSlot[2][];
        private readonly TextSlot[][] _rowMeta = new TextSlot[2][];
        private readonly ClassSlot[][] _rowOff = new ClassSlot[2][];
        private readonly ClassSlot[][] _rowActive = new ClassSlot[2][];
        private readonly ClassSlot[][] _rowDead = new ClassSlot[2][];
        private readonly ClassSlot[][] _rowMuted = new ClassSlot[2][];
        private readonly ClassSlot[][] _rowGagged = new ClassSlot[2][];

        private readonly TextSlot _specHead, _specCount, _specMore;
        private readonly ClassSlot _specMoreOff;
        private readonly TextSlot[] _specName = new TextSlot[AdminMenuIds.SpecChips];
        private readonly ClassSlot[] _specOff = new ClassSlot[AdminMenuIds.SpecChips];
        private readonly ClassSlot[] _specActive = new ClassSlot[AdminMenuIds.SpecChips];

        private readonly ClassSlot _detailEmpty, _detailBan, _detailTeam, _detailPage;
        private readonly TextSlot _detailEmptyText, _dName, _dSub, _banHead, _durationHead, _reasonHead, _reasonCancel;
        private readonly TextSlot[] _statLabel = new TextSlot[AdminMenuIds.Stats];
        private readonly TextSlot[] _statValue = new TextSlot[AdminMenuIds.Stats];
        private readonly TextSlot[] _pageLabel = new TextSlot[AdminMenuIds.DetailPages];
        private readonly ClassSlot[] _pageActive = new ClassSlot[AdminMenuIds.DetailPages];
        private readonly TextSlot[] _actionLabel = new TextSlot[AdminMenuIds.ActionKeys.Length];
        private readonly ClassSlot[] _actionDisabled = new ClassSlot[AdminMenuIds.ActionKeys.Length];
        private readonly ClassSlot?[] _actionConfirm = new ClassSlot?[AdminMenuIds.ActionKeys.Length];
        private readonly TextSlot[] _valueLabel = new TextSlot[AdminMenuIds.PlayerStepKeys.Length];
        private readonly TextSlot[] _valueText = new TextSlot[AdminMenuIds.PlayerStepKeys.Length];
        private readonly ClassSlot[] _valueDecDisabled = new ClassSlot[AdminMenuIds.PlayerStepKeys.Length];
        private readonly ClassSlot[] _valueIncDisabled = new ClassSlot[AdminMenuIds.PlayerStepKeys.Length];
        private readonly TextSlot[] _reasonLabel = new TextSlot[AdminMenuIds.Reasons];
        private readonly TextSlot[] _durationLabel = new TextSlot[AdminMenuIds.Durations];
        private readonly ClassSlot[] _durationActive = new ClassSlot[AdminMenuIds.Durations];
        private readonly TextSlot[] _giveLabel = new TextSlot[AdminMenuIds.Give];
        private readonly ClassSlot[] _giveOff = new ClassSlot[AdminMenuIds.Give];
        private readonly ClassSlot[] _giveDisabled = new ClassSlot[AdminMenuIds.Give];

        private readonly TextSlot[] _serverHead = new TextSlot[AdminMenuIds.ServerHeadVars.Length];
        private readonly TextSlot[] _serverHint = new TextSlot[AdminMenuIds.ServerHintVars.Length];
        private readonly TextSlot[] _serverLabel = new TextSlot[AdminMenuIds.ServerKeys.Length];
        private readonly ClassSlot[] _serverDisabled = new ClassSlot[AdminMenuIds.ServerKeys.Length];
        private readonly ClassSlot?[] _serverConfirm = new ClassSlot?[AdminMenuIds.ServerKeys.Length];
        private readonly TextSlot[] _switchLabel = new TextSlot[AdminMenuIds.SwitchKeys.Length];
        private readonly TextSlot[] _switchState = new TextSlot[AdminMenuIds.SwitchKeys.Length];
        private readonly ClassSlot[] _switchOn = new ClassSlot[AdminMenuIds.SwitchKeys.Length];
        private readonly ClassSlot[] _switchDisabled = new ClassSlot[AdminMenuIds.SwitchKeys.Length];
        private readonly TextSlot[] _settingLabel = new TextSlot[AdminMenuIds.ServerStepKeys.Length];
        private readonly TextSlot[] _settingText = new TextSlot[AdminMenuIds.ServerStepKeys.Length];
        private readonly ClassSlot[] _settingDecDisabled = new ClassSlot[AdminMenuIds.ServerStepKeys.Length];
        private readonly ClassSlot[] _settingIncDisabled = new ClassSlot[AdminMenuIds.ServerStepKeys.Length];

        private readonly TextSlot _mapHead, _mapPage, _mapEmpty, _mapHint;
        private readonly ClassSlot _mapPrevDisabled, _mapNextDisabled, _mapEmptyOff;
        private readonly TextSlot[] _mapName = new TextSlot[AdminMenuIds.Maps];
        private readonly TextSlot[] _mapSub = new TextSlot[AdminMenuIds.Maps];
        private readonly ClassSlot[] _mapOff = new ClassSlot[AdminMenuIds.Maps];
        private readonly ClassSlot[] _mapActive = new ClassSlot[AdminMenuIds.Maps];
        private readonly ClassSlot[] _mapConfirm = new ClassSlot[AdminMenuIds.Maps];
        private readonly ClassSlot[] _mapDisabled = new ClassSlot[AdminMenuIds.Maps];

        private readonly TextSlot _recPage, _recEmpty, _recButton;
        private readonly ClassSlot _recPrevDisabled, _recNextDisabled, _recEmptyOff;
        private readonly TextSlot[] _recKindLabel = new TextSlot[AdminMenuIds.RecordKinds];
        private readonly ClassSlot[] _recKindActive = new ClassSlot[AdminMenuIds.RecordKinds];
        private readonly TextSlot[] _recIndex = new TextSlot[AdminMenuIds.Records];
        private readonly TextSlot[] _recName = new TextSlot[AdminMenuIds.Records];
        private readonly TextSlot[] _recMeta = new TextSlot[AdminMenuIds.Records];
        private readonly ClassSlot[] _recOff = new ClassSlot[AdminMenuIds.Records];
        private readonly ClassSlot[] _recConfirm = new ClassSlot[AdminMenuIds.Records];
        private readonly ClassSlot[] _recDisabled = new ClassSlot[AdminMenuIds.Records];

        private readonly TextSlot _logHead, _logPage, _logEmpty;
        private readonly ClassSlot _logPrevDisabled, _logNextDisabled, _logEmptyOff;
        private readonly TextSlot[] _logTime = new TextSlot[AdminMenuIds.LogRows];
        private readonly TextSlot[] _logText = new TextSlot[AdminMenuIds.LogRows];
        private readonly ClassSlot[] _logOff = new ClassSlot[AdminMenuIds.LogRows];
        private readonly ClassSlot[] _logDanger = new ClassSlot[AdminMenuIds.LogRows];

        /// <summary>Registers the layout contract. The authored values match admin_v1.xml.</summary>
        public AdminMenuView()
        {
            _title = Text(AdminMenuIds.TitleVar);
            _sub = Text(AdminMenuIds.SubVar);
            _navHead = Text(AdminMenuIds.NavHeadVar);
            _hint = Text(AdminMenuIds.HintVar);
            _credit = Text(AdminMenuIds.CreditVar);
            _sure = Text(AdminMenuIds.SureVar);
            _tagMute = Text(AdminMenuIds.TagMuteVar);
            _tagGag = Text(AdminMenuIds.TagGagVar);
            _teamEmpty = Text(AdminMenuIds.TeamEmptyVar);
            _stMapH = Text(AdminMenuIds.MapHeadStatVar);
            _stMap = Text(AdminMenuIds.MapStatVar);
            _stRoundH = Text(AdminMenuIds.RoundHeadStatVar);
            _stRound = Text(AdminMenuIds.RoundStatVar);
            _stScoreH = Text(AdminMenuIds.ScoreHeadStatVar);
            _stT = Text(AdminMenuIds.ScoreTVar);
            _stCt = Text(AdminMenuIds.ScoreCtVar);
            _stPhaseH = Text(AdminMenuIds.PhaseHeadStatVar);
            _stPhase = Text(AdminMenuIds.PhaseStatVar);
            _stTime = Text(AdminMenuIds.TimeStatVar);
            _stPlayersH = Text(AdminMenuIds.PlayersHeadStatVar);
            _stPlayers = Text(AdminMenuIds.PlayersStatVar);

            // authored: view-players, phase-live, tab 0 active
            _view = _hud.Group(AdminMenuIds.RootId, AdminMenuIds.Views, authored: (int)AdminTab.Players);
            _phase = _hud.Group(AdminMenuIds.PhaseId, AdminMenuIds.Phases, authored: (int)MatchPhase.Live);
            for (int t = 0; t < AdminMenuIds.Tabs; t++)
            {
                _tabLabel[t] = Text(AdminMenuIds.TabVars[t]);
                _tabCount[t] = Text(AdminMenuIds.TabCountVars[t]);
                _tabActive[t] = _hud.Flag(AdminMenuIds.TabIds[t], AdminMenuIds.ActiveClass, authoredOn: t == 0);
            }

            // authored: mini pagers off with both buttons disabled, empty labels off, rows off
            for (int team = 0; team < 2; team++)
            {
                _teamHead[team] = Text(AdminMenuIds.TeamHeadVars[team]);
                _teamCount[team] = Text(AdminMenuIds.TeamCountVars[team]);
                _teamPage[team] = Text(AdminMenuIds.TeamPageVars[team]);
                _teamPagerOff[team] = _hud.Flag(AdminMenuIds.TeamPagerIds[team], AdminMenuIds.OffClass, authoredOn: true);
                _teamPrevDisabled[team] = _hud.Flag(AdminMenuIds.TeamPrevIds[team], AdminMenuIds.DisabledClass, authoredOn: true);
                _teamNextDisabled[team] = _hud.Flag(AdminMenuIds.TeamNextIds[team], AdminMenuIds.DisabledClass, authoredOn: true);
                _teamEmptyOff[team] = _hud.Flag(AdminMenuIds.TeamEmptyIds[team], AdminMenuIds.OffClass, authoredOn: true);
                _rowName[team] = new TextSlot[AdminMenuIds.TeamRows];
                _rowStats[team] = new TextSlot[AdminMenuIds.TeamRows];
                _rowMeta[team] = new TextSlot[AdminMenuIds.TeamRows];
                _rowOff[team] = new ClassSlot[AdminMenuIds.TeamRows];
                _rowActive[team] = new ClassSlot[AdminMenuIds.TeamRows];
                _rowDead[team] = new ClassSlot[AdminMenuIds.TeamRows];
                _rowMuted[team] = new ClassSlot[AdminMenuIds.TeamRows];
                _rowGagged[team] = new ClassSlot[AdminMenuIds.TeamRows];
                for (int r = 0; r < AdminMenuIds.TeamRows; r++)
                {
                    string id = AdminMenuIds.RowIds[team][r];
                    _rowName[team][r] = Text(AdminMenuIds.RowNameVars[team][r]);
                    _rowStats[team][r] = Text(AdminMenuIds.RowStatsVars[team][r]);
                    _rowMeta[team][r] = Text(AdminMenuIds.RowMetaVars[team][r]);
                    _rowOff[team][r] = _hud.Flag(id, AdminMenuIds.OffClass, authoredOn: true);
                    _rowActive[team][r] = _hud.Flag(id, AdminMenuIds.ActiveClass);
                    _rowDead[team][r] = _hud.Flag(id, AdminMenuIds.DeadClass);
                    _rowMuted[team][r] = _hud.Flag(id, AdminMenuIds.MutedClass);
                    _rowGagged[team][r] = _hud.Flag(id, AdminMenuIds.GaggedClass);
                }
            }

            _specHead = Text(AdminMenuIds.SpecHeadVar);
            _specCount = Text(AdminMenuIds.SpecCountVar);
            _specMore = Text(AdminMenuIds.SpecMoreVar);
            _specMoreOff = _hud.Flag(AdminMenuIds.SpecMoreId, AdminMenuIds.OffClass, authoredOn: true);
            for (int s = 0; s < AdminMenuIds.SpecChips; s++)
            {
                _specName[s] = Text(AdminMenuIds.SpecVars[s]);
                _specOff[s] = _hud.Flag(AdminMenuIds.SpecIds[s], AdminMenuIds.OffClass, authoredOn: true);
                _specActive[s] = _hud.Flag(AdminMenuIds.SpecIds[s], AdminMenuIds.ActiveClass);
            }

            // authored: card is-empty, moderation page, no ban picker, no team class, first page tab active
            _detailEmpty = _hud.Flag(AdminMenuIds.DetailId, AdminMenuIds.EmptyClass, authoredOn: true);
            _detailBan = _hud.Flag(AdminMenuIds.DetailId, AdminMenuIds.BanPickClass);
            _detailTeam = _hud.Group(AdminMenuIds.DetailId, AdminMenuIds.Teams);
            _detailPage = _hud.Group(AdminMenuIds.DetailId, AdminMenuIds.DetailPageClasses, authored: (int)DetailPage.Moderation);
            _detailEmptyText = Text(AdminMenuIds.DetailEmptyVar);
            _dName = Text(AdminMenuIds.DetailNameVar);
            _dSub = Text(AdminMenuIds.DetailSubVar);
            _banHead = Text(AdminMenuIds.BanPickHeadVar);
            _durationHead = Text(AdminMenuIds.BanDurationHeadVar);
            _reasonHead = Text(AdminMenuIds.BanReasonHeadVar);
            _reasonCancel = Text(AdminMenuIds.ReasonCancelVar);
            for (int k = 0; k < AdminMenuIds.Stats; k++)
            {
                _statLabel[k] = Text(AdminMenuIds.StatLabelVars[k]);
                _statValue[k] = Text(AdminMenuIds.StatValueVars[k]);
            }

            for (int p = 0; p < AdminMenuIds.DetailPages; p++)
            {
                _pageLabel[p] = Text(AdminMenuIds.DetailTabVars[p]);
                _pageActive[p] = _hud.Flag(AdminMenuIds.DetailTabIds[p], AdminMenuIds.ActiveClass, authoredOn: p == 0);
            }

            for (int a = 0; a < AdminMenuIds.ActionKeys.Length; a++)
            {
                _actionLabel[a] = Text(AdminMenuIds.ActionVars[a]);
                _actionDisabled[a] = _hud.Flag(AdminMenuIds.ActionIds[a], AdminMenuIds.DisabledClass);
                if (Array.IndexOf(ConfirmActions, (PlayerAction)a) >= 0)
                {
                    _actionConfirm[a] = _hud.Flag(AdminMenuIds.ActionIds[a], AdminMenuIds.ConfirmClass);
                }
            }

            for (int v = 0; v < AdminMenuIds.PlayerStepKeys.Length; v++)
            {
                _valueLabel[v] = Text(AdminMenuIds.PlayerStepVars[v]);
                _valueText[v] = Text(AdminMenuIds.PlayerStepValueVars[v]);
                _valueDecDisabled[v] = _hud.Flag(AdminMenuIds.PlayerStepDecIds[v], AdminMenuIds.DisabledClass);
                _valueIncDisabled[v] = _hud.Flag(AdminMenuIds.PlayerStepIncIds[v], AdminMenuIds.DisabledClass);
            }

            for (int r = 0; r < AdminMenuIds.Reasons; r++)
            {
                _reasonLabel[r] = Text(AdminMenuIds.ReasonVars[r]);
            }

            for (int d = 0; d < AdminMenuIds.Durations; d++)
            {
                _durationLabel[d] = Text(AdminMenuIds.DurationVars[d]);
                _durationActive[d] = _hud.Flag(AdminMenuIds.DurationIds[d], AdminMenuIds.ActiveClass);
            }

            for (int g = 0; g < AdminMenuIds.Give; g++)
            {
                _giveLabel[g] = Text(AdminMenuIds.GiveVars[g]);
                _giveOff[g] = _hud.Flag(AdminMenuIds.GiveIds[g], AdminMenuIds.OffClass, authoredOn: true);
                _giveDisabled[g] = _hud.Flag(AdminMenuIds.GiveIds[g], AdminMenuIds.DisabledClass);
            }

            for (int h = 0; h < _serverHead.Length; h++)
            {
                _serverHead[h] = Text(AdminMenuIds.ServerHeadVars[h]);
                _serverHint[h] = Text(AdminMenuIds.ServerHintVars[h]);
            }

            for (int b = 0; b < AdminMenuIds.ServerKeys.Length; b++)
            {
                _serverLabel[b] = Text(AdminMenuIds.ServerVars[b]);
                _serverDisabled[b] = _hud.Flag(AdminMenuIds.ServerIds[b], AdminMenuIds.DisabledClass);
                if (Array.IndexOf(ConfirmServer, (ServerAction)b) >= 0)
                {
                    _serverConfirm[b] = _hud.Flag(AdminMenuIds.ServerIds[b], AdminMenuIds.ConfirmClass);
                }
            }

            for (int s = 0; s < AdminMenuIds.SwitchKeys.Length; s++)
            {
                _switchLabel[s] = Text(AdminMenuIds.SwitchVars[s]);
                _switchState[s] = Text(AdminMenuIds.SwitchStateVars[s]);
                _switchOn[s] = _hud.Flag(AdminMenuIds.SwitchIds[s], AdminMenuIds.OnClass);
                _switchDisabled[s] = _hud.Flag(AdminMenuIds.SwitchIds[s], AdminMenuIds.DisabledClass);
            }

            for (int s = 0; s < AdminMenuIds.ServerStepKeys.Length; s++)
            {
                _settingLabel[s] = Text(AdminMenuIds.ServerStepVars[s]);
                _settingText[s] = Text(AdminMenuIds.ServerStepValueVars[s]);
                _settingDecDisabled[s] = _hud.Flag(AdminMenuIds.ServerStepDecIds[s], AdminMenuIds.DisabledClass);
                _settingIncDisabled[s] = _hud.Flag(AdminMenuIds.ServerStepIncIds[s], AdminMenuIds.DisabledClass);
            }

            _mapHead = Text(AdminMenuIds.MapHeadVar);
            _mapPage = Text(AdminMenuIds.MapPageVar);
            _mapEmpty = Text(AdminMenuIds.MapEmptyVar);
            _mapHint = Text(AdminMenuIds.MapHintVar);
            _mapPrevDisabled = _hud.Flag(AdminMenuIds.MapPrevId, AdminMenuIds.DisabledClass, authoredOn: true);
            _mapNextDisabled = _hud.Flag(AdminMenuIds.MapNextId, AdminMenuIds.DisabledClass, authoredOn: true);
            _mapEmptyOff = _hud.Flag(AdminMenuIds.MapEmptyId, AdminMenuIds.OffClass, authoredOn: true);
            for (int m = 0; m < AdminMenuIds.Maps; m++)
            {
                _mapName[m] = Text(AdminMenuIds.MapVars[m]);
                _mapSub[m] = Text(AdminMenuIds.MapSubVars[m]);
                _mapOff[m] = _hud.Flag(AdminMenuIds.MapIds[m], AdminMenuIds.OffClass, authoredOn: true);
                _mapActive[m] = _hud.Flag(AdminMenuIds.MapIds[m], AdminMenuIds.ActiveClass);
                _mapConfirm[m] = _hud.Flag(AdminMenuIds.MapIds[m], AdminMenuIds.ConfirmClass);
                _mapDisabled[m] = _hud.Flag(AdminMenuIds.MapIds[m], AdminMenuIds.DisabledClass);
            }

            _recPage = Text(AdminMenuIds.RecordPageVar);
            _recEmpty = Text(AdminMenuIds.RecordEmptyVar);
            _recButton = Text(AdminMenuIds.RecordButtonVar);
            _recPrevDisabled = _hud.Flag(AdminMenuIds.RecordPrevId, AdminMenuIds.DisabledClass, authoredOn: true);
            _recNextDisabled = _hud.Flag(AdminMenuIds.RecordNextId, AdminMenuIds.DisabledClass, authoredOn: true);
            _recEmptyOff = _hud.Flag(AdminMenuIds.RecordEmptyId, AdminMenuIds.OffClass, authoredOn: true);
            for (int k = 0; k < AdminMenuIds.RecordKinds; k++)
            {
                _recKindLabel[k] = Text(AdminMenuIds.RecordKindVars[k]);
                _recKindActive[k] = _hud.Flag(AdminMenuIds.RecordKindIds[k], AdminMenuIds.ActiveClass, authoredOn: k == 0);
            }

            for (int r = 0; r < AdminMenuIds.Records; r++)
            {
                _recIndex[r] = Text(AdminMenuIds.RecordIndexVars[r]);
                _recName[r] = Text(AdminMenuIds.RecordNameVars[r]);
                _recMeta[r] = Text(AdminMenuIds.RecordMetaVars[r]);
                _recOff[r] = _hud.Flag(AdminMenuIds.RecordIds[r], AdminMenuIds.OffClass, authoredOn: true);
                _recConfirm[r] = _hud.Flag(AdminMenuIds.RecordButtonIds[r], AdminMenuIds.ConfirmClass);
                _recDisabled[r] = _hud.Flag(AdminMenuIds.RecordButtonIds[r], AdminMenuIds.DisabledClass);
            }

            _logHead = Text(AdminMenuIds.LogHeadVar);
            _logPage = Text(AdminMenuIds.LogPageVar);
            _logEmpty = Text(AdminMenuIds.LogEmptyVar);
            _logPrevDisabled = _hud.Flag(AdminMenuIds.LogPrevId, AdminMenuIds.DisabledClass, authoredOn: true);
            _logNextDisabled = _hud.Flag(AdminMenuIds.LogNextId, AdminMenuIds.DisabledClass, authoredOn: true);
            _logEmptyOff = _hud.Flag(AdminMenuIds.LogEmptyId, AdminMenuIds.OffClass, authoredOn: true);
            for (int r = 0; r < AdminMenuIds.LogRows; r++)
            {
                _logTime[r] = Text(AdminMenuIds.LogTimeVars[r]);
                _logText[r] = Text(AdminMenuIds.LogTextVars[r]);
                _logOff[r] = _hud.Flag(AdminMenuIds.LogIds[r], AdminMenuIds.OffClass, authoredOn: true);
                _logDanger[r] = _hud.Flag(AdminMenuIds.LogIds[r], AdminMenuIds.DangerClass);
            }
        }

        public HudLayout Hud => _hud;

        // ---- frame: head, status, rail, foot ----------------------------------------------------

        public void PaintFrame(CCSPlayerController player, string title, string sub, string navHead, string hint, string credit, string sure)
        {
            _hud.SetText(player, _title, title);
            _hud.SetText(player, _sub, sub);
            _hud.SetText(player, _navHead, navHead);
            _hud.SetText(player, _hint, hint);
            _hud.SetText(player, _credit, credit);
            _hud.SetText(player, _sure, sure);
        }

        public void PaintStatus(CCSPlayerController player, in StatusModel s)
        {
            _hud.SetText(player, _stMapH, s.MapHead);
            _hud.SetText(player, _stMap, s.Map);
            _hud.SetText(player, _stRoundH, s.RoundHead);
            _hud.SetText(player, _stRound, s.Round);
            _hud.SetText(player, _stScoreH, s.ScoreHead);
            _hud.SetText(player, _stT, s.TScore);
            _hud.SetText(player, _stCt, s.CtScore);
            _hud.SetText(player, _stPhaseH, s.PhaseHead);
            _hud.SetText(player, _stPhase, s.Phase);
            _hud.SetText(player, _stTime, s.Time);
            _hud.SetText(player, _stPlayersH, s.PlayersHead);
            _hud.SetText(player, _stPlayers, s.Players);
            _hud.SetGroup(player, _phase, (int)s.PhaseKind);
        }

        public void PaintTabs(CCSPlayerController player, AdminTab active, string[] labels, string[] counts)
        {
            for (int t = 0; t < AdminMenuIds.Tabs; t++)
            {
                _hud.SetText(player, _tabLabel[t], labels[t]);
                _hud.SetText(player, _tabCount[t], counts[t]);
            }

            _hud.SetExclusive(player, _tabActive, (int)active);
            _hud.SetGroup(player, _view, (int)active);
        }

        // ---- players view -----------------------------------------------------------------------

        public void PaintPlayerTexts(CCSPlayerController player, string tagMute, string tagGag, string teamEmpty)
        {
            _hud.SetText(player, _tagMute, tagMute);
            _hud.SetText(player, _tagGag, tagGag);
            _hud.SetText(player, _teamEmpty, teamEmpty);
        }

        /// <summary>One team column; <paramref name="rows"/> holds only the current page.</summary>
        public void PaintTeam(CCSPlayerController player, int team, string head, string count, List<PlayerRow> rows, int page, int pages)
        {
            _hud.SetText(player, _teamHead[team], head);
            _hud.SetText(player, _teamCount[team], count);
            _hud.SetFlag(player, _teamPagerOff[team], pages <= 1);
            _hud.SetFlag(player, _teamPrevDisabled[team], page == 0);
            _hud.SetFlag(player, _teamNextDisabled[team], page >= pages - 1);
            _hud.SetText(player, _teamPage[team], NumberText.Of(page + 1) + "/" + NumberText.Of(pages));
            _hud.SetFlag(player, _teamEmptyOff[team], rows.Count != 0);

            for (int r = 0; r < AdminMenuIds.TeamRows; r++)
            {
                if (r >= rows.Count)
                {
                    // hide only: stale text under is-off is invisible and saves writes
                    _hud.SetFlag(player, _rowOff[team][r], true);
                    continue;
                }

                PlayerRow row = rows[r];
                _hud.SetText(player, _rowName[team][r], row.Name);
                _hud.SetText(player, _rowStats[team][r], row.Stats);
                _hud.SetText(player, _rowMeta[team][r], row.Meta);
                _hud.SetFlag(player, _rowActive[team][r], row.Selected);
                _hud.SetFlag(player, _rowDead[team][r], row.Dead);
                _hud.SetFlag(player, _rowMuted[team][r], row.Muted);
                _hud.SetFlag(player, _rowGagged[team][r], row.Gagged);
                _hud.SetFlag(player, _rowOff[team][r], false);
            }
        }

        public void PaintSpectators(CCSPlayerController player, string head, string count, List<PlayerRow> chips, string more)
        {
            _hud.SetText(player, _specHead, head);
            _hud.SetText(player, _specCount, count);
            _hud.SetText(player, _specMore, more);
            _hud.SetFlag(player, _specMoreOff, more.Length == 0);
            for (int s = 0; s < AdminMenuIds.SpecChips; s++)
            {
                if (s >= chips.Count)
                {
                    _hud.SetFlag(player, _specOff[s], true);
                    continue;
                }

                _hud.SetText(player, _specName[s], chips[s].Name);
                _hud.SetFlag(player, _specActive[s], chips[s].Selected);
                _hud.SetFlag(player, _specOff[s], false);
            }
        }

        // ---- player card ------------------------------------------------------------------------

        public void PaintDetailEmpty(CCSPlayerController player, string emptyText)
        {
            _hud.SetText(player, _detailEmptyText, emptyText);
            _hud.SetFlag(player, _detailBan, false);
            _hud.SetFlag(player, _detailEmpty, true);
        }

        public void PaintDetail(CCSPlayerController player, DetailModel d)
        {
            _hud.SetText(player, _dName, d.Name);
            _hud.SetText(player, _dSub, d.Sub);
            _hud.SetGroup(player, _detailTeam, d.Team);
            for (int k = 0; k < AdminMenuIds.Stats; k++)
            {
                _hud.SetText(player, _statLabel[k], d.StatLabels[k]);
                _hud.SetText(player, _statValue[k], d.StatValues[k]);
            }

            for (int p = 0; p < AdminMenuIds.DetailPages; p++)
            {
                _hud.SetText(player, _pageLabel[p], d.PageLabels[p]);
            }
            _hud.SetExclusive(player, _pageActive, (int)d.Page);
            _hud.SetGroup(player, _detailPage, (int)d.Page);

            for (int a = 0; a < d.Actions.Length; a++)
            {
                _hud.SetText(player, _actionLabel[a], d.Actions[a].Label);
                _hud.SetFlag(player, _actionDisabled[a], d.Actions[a].Disabled);
                if (_actionConfirm[a] is ClassSlot confirm)
                {
                    _hud.SetFlag(player, confirm, d.Actions[a].Confirm);
                }
            }

            for (int v = 0; v < d.Values.Length; v++)
            {
                _hud.SetText(player, _valueLabel[v], d.Values[v].Label);
                _hud.SetText(player, _valueText[v], d.Values[v].Value);
                _hud.SetFlag(player, _valueDecDisabled[v], d.Values[v].DecDisabled);
                _hud.SetFlag(player, _valueIncDisabled[v], d.Values[v].IncDisabled);
            }

            for (int g = 0; g < AdminMenuIds.Give; g++)
            {
                if (g >= d.GiveLabels.Length)
                {
                    _hud.SetFlag(player, _giveOff[g], true);
                    continue;
                }

                _hud.SetText(player, _giveLabel[g], d.GiveLabels[g]);
                _hud.SetFlag(player, _giveDisabled[g], d.GiveDisabled);
                _hud.SetFlag(player, _giveOff[g], false);
            }

            _hud.SetText(player, _banHead, d.BanHead);
            _hud.SetText(player, _durationHead, d.DurationHead);
            _hud.SetText(player, _reasonHead, d.ReasonHead);
            _hud.SetText(player, _reasonCancel, d.Cancel);
            for (int i = 0; i < AdminMenuIds.Durations; i++)
            {
                _hud.SetText(player, _durationLabel[i], d.Durations[i]);
            }
            _hud.SetExclusive(player, _durationActive, d.Duration);
            for (int r = 0; r < AdminMenuIds.Reasons; r++)
            {
                _hud.SetText(player, _reasonLabel[r], d.Reasons[r]);
            }
            _hud.SetFlag(player, _detailBan, d.BanPick);

            _hud.SetFlag(player, _detailEmpty, false);
        }

        // ---- server view ------------------------------------------------------------------------

        public void PaintServer(CCSPlayerController player, string[] heads, string[] hints, ButtonModel[] buttons, SwitchModel[] switches, StepperModel[] settings)
        {
            for (int h = 0; h < _serverHead.Length; h++)
            {
                _hud.SetText(player, _serverHead[h], heads[h]);
                _hud.SetText(player, _serverHint[h], hints[h]);
            }

            for (int b = 0; b < buttons.Length; b++)
            {
                _hud.SetText(player, _serverLabel[b], buttons[b].Label);
                _hud.SetFlag(player, _serverDisabled[b], buttons[b].Disabled);
                if (_serverConfirm[b] is ClassSlot confirm)
                {
                    _hud.SetFlag(player, confirm, buttons[b].Confirm);
                }
            }

            for (int s = 0; s < switches.Length; s++)
            {
                _hud.SetText(player, _switchLabel[s], switches[s].Label);
                _hud.SetText(player, _switchState[s], switches[s].State);
                _hud.SetFlag(player, _switchOn[s], switches[s].On);
                _hud.SetFlag(player, _switchDisabled[s], switches[s].Disabled);
            }

            for (int s = 0; s < settings.Length; s++)
            {
                _hud.SetText(player, _settingLabel[s], settings[s].Label);
                _hud.SetText(player, _settingText[s], settings[s].Value);
                _hud.SetFlag(player, _settingDecDisabled[s], settings[s].DecDisabled);
                _hud.SetFlag(player, _settingIncDisabled[s], settings[s].IncDisabled);
            }
        }

        // ---- maps view --------------------------------------------------------------------------

        public void PaintMaps(CCSPlayerController player, string head, string empty, string hint, List<MapTile> tiles, int page, int pages)
        {
            _hud.SetText(player, _mapHead, head);
            _hud.SetText(player, _mapEmpty, empty);
            _hud.SetText(player, _mapHint, hint);
            _hud.SetText(player, _mapPage, NumberText.Of(page + 1) + " / " + NumberText.Of(pages));
            _hud.SetFlag(player, _mapPrevDisabled, page == 0);
            _hud.SetFlag(player, _mapNextDisabled, page >= pages - 1);
            _hud.SetFlag(player, _mapEmptyOff, tiles.Count != 0);
            for (int m = 0; m < AdminMenuIds.Maps; m++)
            {
                if (m >= tiles.Count)
                {
                    _hud.SetFlag(player, _mapOff[m], true);
                    continue;
                }

                MapTile tile = tiles[m];
                _hud.SetText(player, _mapName[m], tile.Name);
                _hud.SetText(player, _mapSub[m], tile.Sub);
                _hud.SetFlag(player, _mapActive[m], tile.Current);
                _hud.SetFlag(player, _mapConfirm[m], tile.Confirm);
                _hud.SetFlag(player, _mapDisabled[m], tile.Disabled);
                _hud.SetFlag(player, _mapOff[m], false);
            }
        }

        // ---- records view -----------------------------------------------------------------------

        public void PaintRecords(CCSPlayerController player, string[] kinds, RecordKind kind, string empty, string button, List<RecordRow> rows, int page, int pages)
        {
            for (int k = 0; k < AdminMenuIds.RecordKinds; k++)
            {
                _hud.SetText(player, _recKindLabel[k], kinds[k]);
            }
            _hud.SetExclusive(player, _recKindActive, (int)kind);
            _hud.SetText(player, _recEmpty, empty);
            _hud.SetText(player, _recButton, button);
            _hud.SetText(player, _recPage, NumberText.Of(page + 1) + " / " + NumberText.Of(pages));
            _hud.SetFlag(player, _recPrevDisabled, page == 0);
            _hud.SetFlag(player, _recNextDisabled, page >= pages - 1);
            _hud.SetFlag(player, _recEmptyOff, rows.Count != 0);
            for (int r = 0; r < AdminMenuIds.Records; r++)
            {
                if (r >= rows.Count)
                {
                    _hud.SetFlag(player, _recOff[r], true);
                    continue;
                }

                RecordRow row = rows[r];
                _hud.SetText(player, _recIndex[r], row.Index);
                _hud.SetText(player, _recName[r], row.Name);
                _hud.SetText(player, _recMeta[r], row.Meta);
                _hud.SetFlag(player, _recConfirm[r], row.Confirm);
                _hud.SetFlag(player, _recDisabled[r], row.Disabled);
                _hud.SetFlag(player, _recOff[r], false);
            }
        }

        // ---- log view ---------------------------------------------------------------------------

        public void PaintLog(CCSPlayerController player, string head, string empty, List<LogRow> rows, int page, int pages)
        {
            _hud.SetText(player, _logHead, head);
            _hud.SetText(player, _logEmpty, empty);
            _hud.SetText(player, _logPage, NumberText.Of(page + 1) + " / " + NumberText.Of(pages));
            _hud.SetFlag(player, _logPrevDisabled, page == 0);
            _hud.SetFlag(player, _logNextDisabled, page >= pages - 1);
            _hud.SetFlag(player, _logEmptyOff, rows.Count != 0);
            for (int r = 0; r < AdminMenuIds.LogRows; r++)
            {
                if (r >= rows.Count)
                {
                    _hud.SetFlag(player, _logOff[r], true);
                    continue;
                }

                _hud.SetText(player, _logTime[r], rows[r].Time);
                _hud.SetText(player, _logText[r], rows[r].Text);
                _hud.SetFlag(player, _logDanger[r], rows[r].Danger);
                _hud.SetFlag(player, _logOff[r], false);
            }
        }

        private TextSlot Text(string name) => _hud.Text(AdminMenuIds.RootId, name);
    }
}
