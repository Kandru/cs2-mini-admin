> [!IMPORTANT]  
> Work in progress. Might not work as expected. Use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/) to keep updated just in case :)

# CounterstrikeSharp - Mini Admin

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-mini-admin?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-mini-admin/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-mini-admin](https://img.shields.io/github/issues/Kandru/cs2-mini-admin)](https://github.com/Kandru/cs2-mini-admin/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This will be a very simple admin implementation without a database or other stuff. Simply grant admin permissions and use !kick, !ban, !mute or !unmute. Will remember players even when they reconnect.

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-mini-admin/releases/).
2. Move the "MiniAdmin" folder to the `/addons/counterstrikesharp/plugins/` directory.
3. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically. To automate updates please use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/).


## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/MiniAdmin/MiniAdmin.json`.

```json

```

## Commands

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

### !fswitch <player> <t/ct/s>

**Permission: @miniadmin/switch**

Forcibly switches the team of the player, the player will remain alive and keep their weapons.

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
