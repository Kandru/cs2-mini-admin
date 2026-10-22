#!/usr/bin/env python3
"""Generate the admin menu layout (admin_v1.xml).

The whole layout is generated: the row, chip and button pools must match src/Hud/AdminMenuIds.cs
(checked by validate_panorama.py), and hand-copied pool markup drifts.

Usage: gen_admin_panorama.py          rewrite admin_v1.xml
       gen_admin_panorama.py --check  exit 1 if admin_v1.xml is out of date
"""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "workshop" / "content" / "panorama"
ADMIN_XML = CONTENT / "layout" / "custom_game" / "miniadmin" / "admin_v1.xml"

# Keep in sync with AdminMenuIds (validated by validate_panorama.py).
TABS = 5
TEAM_ROWS = 10
SPEC_CHIPS = 6
STATS = 10
DETAIL_PAGES = 4
REASONS = 4
DURATIONS = 4
GIVE = 12
MAPS = 12
RECORDS = 8
RECORD_KINDS = 3
LOG_ROWS = 10

# detail card pages: rows of (button key, danger, confirm); order of the keys = AdminMenuIds.ActionKeys
DETAIL_ACTIONS = [
    # moderation
    [[("kick", True, True), ("ban", True, False), ("mute", False, False)],
     [("gag", False, False), ("rename", False, True)]],
    # team
    [[("tot", False, False), ("toct", False, False), ("tospec", False, False)],
     [("fswitch", False, False), ("respawn", False, False), ("kill", True, True)]],
    # player
    [[("slap", False, False), ("freeze", False, False), ("noclip", False, False)],
     [("god", False, False), ("bring", False, False), ("goto", False, False)]],
    # items: the give chips come first, strip below them
    [[("strip", True, True)]],
]
PLAYER_STEPPERS = ["hp", "armor", "money"]

# server view cards: (rows of (key, danger, confirm), switches, steppers)
SERVER_CARDS = [
    ([[("rr", False, False), ("mr", True, True), ("pause", False, False)],
      [("tmt", False, False), ("tmct", False, False)]], [], []),
    ([[("wustart", False, False), ("wuend", False, False), ("wuext", False, False)]], ["wuhold"], []),
    ([], [], ["roundtime", "freezetime", "maxrounds"]),
    ([[("swap", True, True), ("scramble", True, True)],
      [("bott", False, False), ("botct", False, False), ("botkick", True, True)]], [], []),
    ([], ["ff", "alltalk", "deadtalk", "buyany", "respawn"], ["gravity", "teamlimit"]),
]
SERVER_LEFT = [0, 1, 2]
SERVER_RIGHT = [3, 4]


def label(cls: str, text: str, font: str = "stratum-bold-tf", pid: str | None = None) -> str:
    id_attr = f'id="{pid}" ' if pid else ""
    return f'<Label {id_attr}class="{cls} {font}" hittest="false" text="{text}" />'


def button(pid: str, cls: str, body: list[str]) -> list[str]:
    return [f'<Button id="{pid}" class="{cls}">', *indent(body, 1), "</Button>"]


def panel(cls: str, body: list[str], pid: str | None = None) -> list[str]:
    id_attr = f'id="{pid}" ' if pid else ""
    if not body:
        return [f'<Panel {id_attr}class="{cls}" hittest="false" />']
    return [f'<Panel {id_attr}class="{cls}" hittest="false">', *indent(body, 1), "</Panel>"]


def indent(lines: list[str], depth: int) -> list[str]:
    pad = "\t" * depth
    return [pad + line for line in lines]


def action_button(pid: str, var: str, danger: bool, confirm: bool, extra: str = "") -> list[str]:
    cls = "MaBtn" + (" MaBtnDanger" if danger else "") + extra
    body = [label("MaBtnLabel", f"{{s:{var}}}")]
    if confirm:
        body.append(label("MaBtnSure", "{s:ma_sure}"))
    return button(pid, cls, body)


def button_rows(rows: list[list[tuple[str, bool, bool]]], prefix: str, var_prefix: str) -> list[str]:
    out: list[str] = []
    for row in rows:
        cells: list[str] = []
        for key, danger, confirm in row:
            cells += action_button(f"{prefix}{key}", f"{var_prefix}{key}", danger, confirm)
        out += panel("MaBtnRow", cells)
    return out


def pager(prefix: str, page_var: str, pid: str | None = None, mini: bool = False) -> list[str]:
    cls = "MaMiniPager" if mini else "MaPager"
    extra = " is-off" if mini else ""
    return panel(cls + extra, [
        *button(f"{prefix}-prev", "MaPgBtn is-disabled", [label("MaPgLabel", "‹")]),
        label("MaPage", f"{{s:{page_var}}}"),
        *button(f"{prefix}-next", "MaPgBtn is-disabled", [label("MaPgLabel", "›")]),
    ], pid)


def stepper(prefix: str, var: str) -> list[str]:
    """‹ value › with a name on the left; the buttons apply the change at once."""
    return panel("MaStepper", [
        label("MaStepLabel", f"{{s:{var}}}"),
        *button(f"{prefix}-dec", "MaPgBtn MaStepBtn", [label("MaPgLabel", "‹")]),
        label("MaStepValue", f"{{s:{var}_v}}"),
        *button(f"{prefix}-inc", "MaPgBtn MaStepBtn", [label("MaPgLabel", "›")]),
    ])


# ---- head, status, rail, foot -------------------------------------------------------------

def head() -> list[str]:
    return panel("MaHead", [
        label("MaKicker", "//"),
        label("MaTitle", "{s:ma_title}"),
        label("MaSub", "{s:ma_sub}"),
        *button("ma-close", "MaClose", [
            '<Panel class="MaCloseX MaCloseA" hittest="false" />',
            '<Panel class="MaCloseX MaCloseB" hittest="false" />',
        ]),
    ])


def stat(key: str, extra: str = "", pid: str | None = None, value: list[str] | None = None) -> list[str]:
    body = [label("MaStatH", f"{{s:ma_st_{key}_h}}")]
    body += value if value is not None else [label("MaStatV", f"{{s:ma_st_{key}}}")]
    return panel("MaStat" + extra, body, pid)


def status() -> list[str]:
    score = panel("MaScore", [
        label("MaScoreTeam MaScoreTeamT", "T"),
        label("MaScoreT", "{s:ma_st_t}"),
        label("MaScoreSep", ":"),
        label("MaScoreCt", "{s:ma_st_ct}"),
        label("MaScoreTeam MaScoreTeamCt", "CT"),
    ])
    phase = panel("MaPhase", [
        '<Panel class="MaPhaseDot" hittest="false" />',
        label("MaStatV", "{s:ma_st_phase}"),
        label("MaStatTime", "{s:ma_st_time}"),
    ])
    return panel("MaStatus", [
        *stat("map"),
        *stat("round"),
        *stat("score", value=score),
        *stat("phase", " MaStatPhase phase-live", pid="ma-st-phase", value=phase),
        *stat("players"),
    ])


def rail() -> list[str]:
    tabs: list[str] = []
    for i in range(TABS):
        active = " is-active" if i == 0 else ""
        tabs += button(f"ma-tab-{i}", f"MaNav{active}", [
            label("MaNavLabel", f"{{s:ma_tab{i}}}"),
            label("MaNavCount", f"{{s:ma_tabc{i}}}"),
        ])
    return panel("MaRail", [label("MaRailHead", "{s:ma_nav_h}"), *tabs])


def foot() -> list[str]:
    return panel("MaFoot", [
        label("MaFootHint", "{s:ma_hint}"),
        label("MaCredit", "{s:ma_credit}"),
    ])


# ---- players view -------------------------------------------------------------------------

def team_row(team: str, var: str, i: int) -> list[str]:
    return button(f"ma-{team}-r{i}", "MaPRow is-off", [
        '<Panel class="MaPMark" hittest="false" />',
        label("MaPName", f"{{s:ma_{var}{i}_n}}"),
        label("MaPTag MaPTagMute", "{s:ma_tag_mute}"),
        label("MaPTag MaPTagGag", "{s:ma_tag_gag}"),
        label("MaPStats", f"{{s:ma_{var}{i}_s}}", "stratum-medium-tf"),
        label("MaPMeta", f"{{s:ma_{var}{i}_m}}", "stratum-medium-tf"),
    ])


def team_column(team: str, var: str, title_var: str) -> list[str]:
    rows = [line for i in range(TEAM_ROWS) for line in team_row(team, var, i)]
    return panel(f"MaCol MaCol{team.capitalize()}", [
        *panel("MaColHead", [
            '<Panel class="MaColMark" hittest="false" />',
            label("MaColTitle", f"{{s:{title_var}}}"),
            label("MaColCount", f"{{s:{title_var}_n}}"),
            *pager(f"ma-{team}", f"ma_{team}_page", pid=f"ma-{team}p", mini=True),
        ]),
        *rows,
        label("MaColEmpty is-off", "{s:ma_team_empty}", "stratum-medium-tf", pid=f"ma-{team}-empty"),
    ])


def spectators() -> list[str]:
    chips = [line for i in range(SPEC_CHIPS) for line in button(f"ma-s-{i}", "MaSChip is-off", [
        label("MaSChipLabel", f"{{s:ma_sr{i}_n}}"),
    ])]
    return panel("MaSpec", [
        *panel("MaSpecHead", [
            label("MaSpecTitle", "{s:ma_hs}"),
            label("MaSpecCount", "{s:ma_hs_n}"),
        ]),
        *panel("MaSpecChips", [
            *chips,
            label("MaSpecMore is-off", "{s:ma_s_more}", "stratum-medium-tf", pid="ma-s-more"),
        ]),
    ])


def chip_rows(prefix: str, var_prefix: str, count: int, per_row: int, extra: str = "") -> list[str]:
    out: list[str] = []
    for r in range(count // per_row):
        cells: list[str] = []
        for c in range(per_row):
            k = r * per_row + c
            cells += button(f"{prefix}{k}", f"MaChip{extra}", [label("MaChipLabel", f"{{s:{var_prefix}{k}}}")])
        out += panel("MaChipRow", cells)
    return out


def detail() -> list[str]:
    stats: list[str] = []
    for r in range(STATS // 2):
        cells = []
        for c in range(2):
            k = r * 2 + c
            cells += panel("MaDStat", [
                label("MaDStatL", f"{{s:ma_dl{k}}}"),
                label("MaDStatV", f"{{s:ma_dv{k}}}", "stratum-medium-tf"),
            ])
        stats += panel("MaDStatRow", cells)

    tabs: list[str] = []
    for i in range(DETAIL_PAGES):
        active = " is-active" if i == 0 else ""
        tabs += button(f"ma-dt-{i}", f"MaChip MaDTab{active}", [label("MaChipLabel", f"{{s:ma_dt{i}}}")])

    pages = [
        panel("MaDPage MaDPageMod", button_rows(DETAIL_ACTIONS[0], "ma-a-", "ma_a_")),
        panel("MaDPage MaDPageTeam", button_rows(DETAIL_ACTIONS[1], "ma-a-", "ma_a_")),
        panel("MaDPage MaDPagePlayer", [
            *button_rows(DETAIL_ACTIONS[2], "ma-a-", "ma_a_"),
            *[line for key in PLAYER_STEPPERS for line in stepper(f"ma-ps-{key}", f"ma_ps_{key}")],
        ]),
        panel("MaDPage MaDPageItems", [
            *chip_rows("ma-g-", "ma_g", GIVE, 4, " is-off"),
            *panel("MaBtnRow", action_button("ma-a-strip", "ma_a_strip", True, True, " MaBtnFull")),
        ]),
    ]

    reasons: list[str] = []
    for r in range(REASONS // 2):
        cells = []
        for c in range(2):
            k = r * 2 + c
            cells += action_button(f"ma-br-{k}", f"ma_br{k}", True, False, " MaBtnWide")
        reasons += panel("MaBtnRow", cells)

    durations = [line for i in range(DURATIONS) for line in button(f"ma-bd-{i}", "MaChip", [
        label("MaChipLabel", f"{{s:ma_bd{i}}}"),
    ])]

    return panel("MaDetail is-empty dpage-mod", [
        label("MaDetailEmpty", "{s:ma_d_empty}", "stratum-medium-tf"),
        *panel("MaDBody", [
            *panel("MaDHead", [
                '<Panel class="MaDMark" hittest="false" />',
                *panel("MaDHeadText", [
                    label("MaDName", "{s:ma_d_name}"),
                    label("MaDSub", "{s:ma_d_sub}", "stratum-medium-tf"),
                ]),
            ]),
            *panel("MaDStats", stats),
            *panel("MaDTabs", tabs),
            *panel("MaDPages", [line for page in pages for line in page]),
            *panel("MaBanPick", [
                label("MaBanHint", "{s:ma_bp_h}", "stratum-medium-tf"),
                label("MaSection", "{s:ma_bp_dh}"),
                *panel("MaChipRow", durations),
                label("MaSection", "{s:ma_bp_rh}"),
                *reasons,
                *panel("MaBtnRow", action_button("ma-br-cancel", "ma_br_cancel", False, False, " MaBtnFull")),
            ]),
        ]),
    ], pid="ma-detail")


def players_view() -> list[str]:
    return panel("MaPlayers", [
        *panel("MaTeams", [
            *panel("MaCols", [
                *team_column("t", "tr", "ma_ht"),
                '<Panel class="MaColGap" hittest="false" />',
                *team_column("ct", "cr", "ma_hct"),
            ]),
            *spectators(),
        ]),
        '<Panel class="MaColGap" hittest="false" />',
        *detail(),
    ])


# ---- server view --------------------------------------------------------------------------

def switch(key: str) -> list[str]:
    return button(f"ma-sw-{key}", "MaSwitch", [
        label("MaSwitchLabel", f"{{s:ma_sw_{key}}}"),
        label("MaSwitchState", f"{{s:ma_sw_{key}_st}}"),
        *panel("MaSwitchTrack", ['<Panel class="MaSwitchKnob" hittest="false" />']),
    ])


def server_card(index: int, rows: list[list[tuple[str, bool, bool]]], switches: list[str], steppers: list[str]) -> list[str]:
    body = [label("MaSection MaSectionFirst", f"{{s:ma_sv_h{index}}}")]
    body += button_rows(rows, "ma-sv-", "ma_sv_")
    for key in switches:
        body += switch(key)
    for key in steppers:
        body += stepper(f"ma-sp-{key}", f"ma_sp_{key}")
    body.append(label("MaSvHint", f"{{s:ma_sv_n{index}}}", "stratum-medium-tf"))
    return panel("MaSvCard", panel("MaSvInner", body))


def server_view() -> list[str]:
    cards = [server_card(i, rows, switches, steppers) for i, (rows, switches, steppers) in enumerate(SERVER_CARDS)]

    def column(indexes: list[int]) -> list[str]:
        body: list[str] = []
        for n, i in enumerate(indexes):
            if n:
                body.append('<Panel class="MaSvGap" hittest="false" />')
            body += cards[i]
        return panel("MaSvCol", body)

    return panel("MaServer", [*column(SERVER_LEFT), '<Panel class="MaColGap" hittest="false" />', *column(SERVER_RIGHT)])


# ---- maps view ----------------------------------------------------------------------------

def maps_view() -> list[str]:
    rows: list[str] = []
    for r in range(MAPS // 4):
        cells = []
        for c in range(4):
            k = r * 4 + c
            cells += button(f"ma-mp-{k}", "MaMap is-off", [
                label("MaMapName", f"{{s:ma_mp{k}}}"),
                label("MaMapSub", f"{{s:ma_mp{k}_s}}", "stratum-medium-tf"),
                label("MaBtnSure", "{s:ma_sure}"),
            ])
        rows += panel("MaMapRow", cells)
    return panel("MaMaps", [
        *panel("MaBar", [label("MaSectionBar", "{s:ma_mp_h}"), *pager("ma-mp", "ma_mp_page")]),
        *rows,
        label("MaEmpty is-off", "{s:ma_mp_empty}", "stratum-medium-tf", pid="ma-mp-empty"),
        label("MaHintLine", "{s:ma_mp_hint}", "stratum-medium-tf"),
    ])


# ---- records view (bans / mutes / gags) ---------------------------------------------------

def records_view() -> list[str]:
    kinds = [line for i in range(RECORD_KINDS) for line in button(
        f"ma-rf-{i}", "MaChip MaKind" + (" is-active" if i == 0 else ""), [label("MaChipLabel MaKindLabel", f"{{s:ma_rf{i}}}")])]
    rows: list[str] = []
    for i in range(RECORDS):
        rows += panel("MaRec is-off", [
            *panel("MaIdx", [label("MaIdxLabel", f"{{s:ma_rc{i}_i}}")]),
            *panel("MaRecBody", [
                label("MaRecName", f"{{s:ma_rc{i}_n}}"),
                label("MaRecMeta", f"{{s:ma_rc{i}_m}}", "stratum-medium-tf"),
            ]),
            *action_button(f"ma-rc-un-{i}", "ma_rc_un", False, True, " MaRecBtn"),
        ], pid=f"ma-rc-{i}")
    return panel("MaRecords", [
        *panel("MaBar", [*panel("MaKinds", kinds), *pager("ma-rc", "ma_rc_page")]),
        *rows,
        label("MaEmpty is-off", "{s:ma_rc_empty}", "stratum-medium-tf", pid="ma-rc-empty"),
    ])


# ---- log view -----------------------------------------------------------------------------

def log_view() -> list[str]:
    rows: list[str] = []
    for i in range(LOG_ROWS):
        rows += panel("MaLog is-off", [
            label("MaLogTime", f"{{s:ma_lg{i}_t}}", "stratum-medium-tf"),
            label("MaLogText", f"{{s:ma_lg{i}_x}}", "stratum-medium-tf"),
        ], pid=f"ma-lg-{i}")
    return panel("MaLogs", [
        *panel("MaBar", [label("MaSectionBar", "{s:ma_lg_h}"), *pager("ma-lg", "ma_lg_page")]),
        *rows,
        label("MaEmpty is-off", "{s:ma_lg_empty}", "stratum-medium-tf", pid="ma-lg-empty"),
    ])


# ---- document -----------------------------------------------------------------------------

def layout_xml() -> str:
    card = [
        '<Panel class="MaBg" hittest="false" />',
        '<Panel class="MaCorner MaCornerTL" hittest="false" />',
        '<Panel class="MaCorner MaCornerTR" hittest="false" />',
        '<Panel class="MaCorner MaCornerBL" hittest="false" />',
        '<Panel class="MaCorner MaCornerBR" hittest="false" />',
        *panel("MaContent", [
            *head(),
            *status(),
            *panel("MaBody", [
                *rail(),
                *panel("MaMain", [
                    '<Panel class="MaPad" hittest="false" />',
                    *panel("MaInner", [
                        '<Panel class="MaVPad" hittest="false" />',
                        *panel("MaViews", [*players_view(), *server_view(), *maps_view(), *records_view(), *log_view()]),
                    ]),
                    '<Panel class="MaPad" hittest="false" />',
                ]),
            ]),
            *foot(),
        ]),
    ]
    contract = (
        f"miniadmin:admin v1 tabs={TABS} team_rows={TEAM_ROWS} spec={SPEC_CHIPS} stats={STATS} "
        f"detail_pages={DETAIL_PAGES} reasons={REASONS} durations={DURATIONS} give={GIVE} maps={MAPS} "
        f"records={RECORDS} record_kinds={RECORD_KINDS} log_rows={LOG_ROWS} - contract: src/Hud/AdminMenuIds.cs"
    )
    out = [
        "<root>",
        "\t<styles>",
        '\t\t<include src="s2r://panorama/styles/csgostyles.vcss" />',
        '\t\t<include src="s2r://panorama/styles/custom_game/miniadmin/admin_v1.vcss" />',
        "\t</styles>",
        "",
        "\t<!-- generated by tools/gen_admin_panorama.py - do not edit by hand -->",
        f"\t<!-- {contract} -->",
        '\t<Panel class="MaScreen" hittest="false">',
        *indent(panel("MaCard is-off view-players", card, pid="ma-root"), 2),
        "\t</Panel>",
        "</root>",
        "",
    ]
    return "\n".join(out)


def render() -> dict[Path, str]:
    return {ADMIN_XML: layout_xml()}


def main() -> None:
    check = "--check" in sys.argv[1:]
    stale = []
    for path, text in render().items():
        if path.is_file() and path.read_text(encoding="utf-8") == text:
            continue
        if check:
            stale.append(path.name)
        else:
            path.write_text(text, encoding="utf-8", newline="\n")
            print(f"wrote {path.relative_to(ROOT)}")
    if stale:
        print(f"ERROR: generated layout out of date in {', '.join(stale)} - run tools/gen_admin_panorama.py", file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
