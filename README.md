> [!IMPORTANT]  
> Work in progress. Might not work as expected. Use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/) to keep updated just in case :)

# CounterstrikeSharp - Mini Admin

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-mini-admin?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-mini-admin/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-mini-admin](https://img.shields.io/github/issues/Kandru/cs2-mini-admin)](https://github.com/Kandru/cs2-mini-admin/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This will be a very simple admin implementation without a database or other stuff. Simply grant admin permissions and use !kick, !ban, !mute or !unmute. Will remember players even when they reconnect.

Admins can also open a mouse-driven admin menu with `!admin`: team lists with player stats and actions, server actions (round, warmup, teams, rules), map change and the ban and mute lists.

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-mini-admin/releases/).
2. Move the "MiniAdmin" folder to the `/addons/counterstrikesharp/plugins/` directory.
3. For the `!admin` menu: install [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager/releases) and let it load the MiniAdmin Panorama workshop addon (see [Admin menu](#admin-menu-admin)).
4. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically. To automate updates please use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/).


## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/MiniAdmin/MiniAdmin.json`.

```json
{
  "enabled": true,
  "debug": false,
  "detections": {
    "enable_namechange_check": true,
    "max_namechanges_per_minute": 20,
    "action_on_namechange_detection": "ban"
  },
  "menu": {
    "maps": [ "de_ancient", "de_anubis", "de_dust2", "de_inferno", "de_mirage", "de_nuke", "de_overpass", "de_train", "de_vertigo", "cs_italy", "cs_office" ],
    "give_items": [ "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_awp", "weapon_deagle", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "item_assaultsuit", "item_defuser", "weapon_healthshot" ],
    "ban_durations": [ 60, 1440, 10080, 0 ],
    "slap_damage": 5
  },
  "discord": {
    "webhook_url": "",
    "events": [ "kick", "ban", "unban" ]
  },
  "action_log_file": true,
  "players_banned": {},
  "players_muted": {},
  "players_gagged": {},
  "ConfigVersion": 1
}
```

- `menu.maps`: maps of the menu's map tab. A map name (`de_dust2`) or a workshop id with an optional label (`3070284539|Mocha`), which is loaded with `host_workshop_map`.
- `menu.give_items`: the item buttons of the player card (at most 12).
- `menu.ban_durations`: the four duration buttons of the ban picker in minutes, `0` = permanent.
- `menu.slap_damage`: damage of one slap.
- `discord.webhook_url` / `discord.events`: posts these actions (action log keys such as `kick`, `ban`, `unban`, `mute`, `gag`) to a Discord webhook. Empty url = off.
- `action_log_file`: appends every admin action to `addons/counterstrikesharp/logs/MiniAdmin-actions.log` (tab separated: time, admin, admin SteamID, action, target, target SteamID, detail). The menu's log tab reads the last 200 entries from it after a restart.
- Ban entries get `until` (unix time, `0` = permanent). Expired bans are removed at map start and when the player connects.

## Admin menu (!admin)

`!admin` opens and closes the menu. It opens for every player that has at least one MiniAdmin permission; each button checks its own permission and is greyed out otherwise.

| Tab | Content |
| --- | --- |
| Players | Status strip (map, round, score, phase with time, players), Terrorists and Counter-Terrorists side by side (K/D/A, ping; dead players framed red and greyed, muted/gagged tags) and spectators. A click on a player opens the player card: HP, armor, money, K/D/A, damage, headshots, MVPs, score, ping, time online and four pages: **Moderate** (kick, ban with duration and reason, mute, gag, rename to "Player <id>"), **Team** (to T / CT / spectators, force switch, respawn, kill), **Player** (slap, freeze, noclip, god mode, bring, go to; steppers for health, armor, money), **Items** (12 give buttons, remove weapons). |
| Server | Restart round (draw, score unchanged), restart match, pause/resume, timeout T/CT, warmup start/end/+1 min/hold timer, steppers for round time, freeze time, max rounds, gravity and team limit, swap/scramble teams, add/remove bots, switches for friendly fire, alltalk, dead talk, buy anywhere and respawn after death. |
| Maps | Map change from `menu.maps`. |
| Sanctions | Bans, mutes and gags with SteamID, reason, end of a timed ban, date and admin; unban/unmute/ungag per entry. |
| Log | The admin actions, newest first (destructive ones framed red). |

Destructive buttons (kick, rename, kill, remove weapons, restart match, swap, scramble, remove bots, map change, unban, unmute, ungag) show "SURE?" first and run on the second click within 3 seconds. Ban asks for duration and reason instead. The menu stays open on death and comes back after a round restart. Round time is shown and set for the cvar the map uses (`mp_roundtime_defuse` on bomb maps, `mp_roundtime_hostage` on hostage maps); a stepper writes all three.

In the chat, `@text` goes to all admins (any MiniAdmin permission) instead of everybody, so players can reach the admins. Gagged players cannot write; chat commands (`!`, `/`) still work for them.

The menu is a Panorama layout (`custom_hud_layout`). The client loads it from a workshop addon that the server mounts with MultiAddonManager:

- Sources: [`workshop/content`](workshop/content) (copy into `csgo_addons/<addon>/`), compiled with the CS2 Workshop Tools.
- The layout is generated: change [`tools/gen_admin_panorama.py`](tools/gen_admin_panorama.py), not the XML, then run `python tools/gen_admin_panorama.py` and `python tools/validate_panorama.py`. `python tools/validate_lang.py` checks that every translation key the code uses exists in all language files.
- Write button texts that are not CS2 localization tokens: the client translates such values itself ("Cancel" showed up as "Abbrechen", "CT" as "AT-Seite" on a German client).
- The menu follows the [cs2-panorama-ruleset](https://github.com/KennyZorro/cs2-panorama-ruleset).

## Commands

### !admin

**Permission: any MiniAdmin permission**

Opens or closes the admin menu (see above).

### !rr (@miniadmin/round)

Restarts the current round as a draw: no team gets a point.

### !pause / !unpause (@miniadmin/round)

Pauses the match in the next freeze time (`mp_pause_match`) / resumes it.

### !warmup <start|end|extend [seconds]|hold|resume> (@miniadmin/round)

Starts or ends the warmup, extends it (default 60 seconds, holds the warmup timer meanwhile) or holds/resumes the warmup timer.

### !swapteams / !scrambleteams (@miniadmin/teams)

Swaps or scrambles the teams. Both restart the match.

### !map <map|workshop id> (@miniadmin/map)

Changes the map after 3 seconds. Numeric ids are loaded as workshop maps.

### !timeout <t|ct> (@miniadmin/round)

Starts a tactical timeout of a team (in the next freeze time).

### !tban <player> <minutes> [reason] (@miniadmin/ban)

Bans an online player for a number of minutes (`0` = permanent).

### !banid <steamid64> [minutes] [reason] (@miniadmin/ban)

Bans a SteamID64, also when the player is offline. Without minutes the ban is permanent.

### !gag / !ungag <player> (@miniadmin/gag)

Blocks / allows the chat of a player. `!ungag` also takes a SteamID64 or the stored name of an offline player.

### !slap <player> [damage], !freeze, !noclip, !god, !strip <player> (@miniadmin/player)

Slap (push and damage), freeze/unfreeze, noclip on/off, god mode on/off, remove all weapons (the knife stays).

### !hp / !armor / !money <player> <value> (@miniadmin/player)

Sets health (1-500), armor (0-100) or money (0-`mp_maxmoney`).

### !bring / !goto <player> (@miniadmin/player)

Teleports the player to you / you to the player. Both have to be alive.

### !rename <player> <new name> (@miniadmin/rename)

Renames a player.

### !csay <message> (@miniadmin/chat)

Shows the message in the middle of every screen and in the chat.

Targets of these commands: part of the name, `#userid` or a SteamID64. More than one match lists the candidates.

### Permissions of the menu only

| Permission | Buttons |
| --- | --- |
| `@miniadmin/bots` | add bot T/CT, remove bots |
| `@miniadmin/rules` | friendly fire, alltalk, dead talk, buy anywhere, respawn after death, round time, freeze time, max rounds, gravity, team limit |

`@css/root` does **not** include these permissions: CounterStrikeSharp checks root per domain. Give admins `@miniadmin/root` for all of them.

### !kick <player> (@miniadmin/kick)

**Permission: @miniadmin/kick**

Kicks a player by his name. Will provide a list of all online players if no player name was entered. Will provide a list of all matching player names to choose from. Will kick the selected player instantly.

### !ban <player> (@miniadmin/ban)

**Permission: @miniadmin/ban**

Bans a player by his name. Will provide a list of all online players if no player name was entered. Will provide a list of all matching player names to choose from. Will kick & ban the selected player instantly.

### !unban <player> (@miniadmin/ban)

**Permission: @miniadmin/ban**

Unbans a player by his Steam ID or last known name. Will provide a list of all banned players if no player name was entered. Will provide a list of all matching player names to choose from.

### !mute <player> (@miniadmin/ban)

**Permission: @miniadmin/mute**

Mutes a player by his name. Will provide a list of all online players if no player name was entered. Will provide a list of all matching player names to choose from. Will mute instantly.

### !unmute <player> (@miniadmin/ban)

**Permission: @miniadmin/mute**

Unmutes a player by his Steam ID or last known name. Will provide a list of all muted players if no player name was entered. Will provide a list of all matching player names to choose from.

### !restart <delay>

**Permission: @miniadmin/restart**

Restarts the match by optionally providing a delay (default = 3 seconds).

### !switch <player> <t/ct/s>

**Permission: @miniadmin/switch**

Switches the team of the player, has the same effect as the "jointeam" console command. This follows gamemode rules, so this will usually cause a player suicide/loss of weapons.

### !fswitch <player>

**Permission: @miniadmin/switch**

Forcibly switches the team of the player, the player will remain alive and keep their weapons.

### !respawn <player>

**Permission: @miniadmin/respawn**

Respawns a given player.

### !kill <player>

**Permission: @miniadmin/kill**

Kills a given player.

### !give <player> <item>

**Permission: @miniadmin/give**

Gives an item to a player.


## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-mini-admin.git
```

Go to the project directory

```bash
  cd cs2-mini-admin
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

Additionally add the dependencies (if not added already for the panorama-vote-manager):

```bash
git submodule add https://github.com/Kandru/cs2-panorama-vote-manager.git
git commit -m "added panorama-vote-manager as a submodule"
git push
```

## FAQ

TODO

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
