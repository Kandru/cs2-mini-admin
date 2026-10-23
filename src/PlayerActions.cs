using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using MiniAdmin.Hud;

namespace MiniAdmin
{
    /// <summary>Actions on the pawn of a living player: slap, freeze, noclip, god mode, values, strip, teleport, rename.</summary>
    public partial class MiniAdmin
    {
        // stepper limits of the player card: step, min, max (money max = mp_maxmoney)
        private static readonly (int Step, int Min, int Max)[] ValueSteps = [(25, 1, 500), (25, 0, 100), (1000, 0, 16000)];

        private static CCSPlayerPawn? AlivePawn(CCSPlayerController? player) =>
            player is { IsValid: true, PawnIsAlive: true } && player.PlayerPawn.Value is { IsValid: true } pawn ? pawn : null;

        private static bool IsFrozen(CCSPlayerPawn pawn) =>
            pawn.MoveType is MoveType_t.MOVETYPE_OBSOLETE or MoveType_t.MOVETYPE_NONE;

        private static bool IsNoclip(CCSPlayerPawn pawn) => pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP;

        private static void SetMoveType(CCSPlayerPawn pawn, MoveType_t type)
        {
            pawn.MoveType = type;
            // m_nActualMoveType is one byte: write exactly one
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (byte)type);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        private bool SlapPlayer(CCSPlayerController player, int damage, CCSPlayerController? admin = null)
        {
            if (AlivePawn(player) is not { } pawn)
            {
                return false;
            }
            // random push sideways and up, like the classic admin slap
            Vector velocity = pawn.AbsVelocity;
            pawn.Teleport(null, null, new Vector(
                velocity.X + Random.Shared.Next(-250, 251),
                velocity.Y + Random.Shared.Next(-250, 251),
                velocity.Z + Random.Shared.Next(200, 301)));
            if (damage > 0)
            {
                int health = pawn.Health - damage;
                if (health <= 0)
                {
                    player.CommitSuicide(false, true);
                }
                else
                {
                    pawn.Health = health;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }
            LogAction(admin, "slap", player, damage + " HP");
            Server.PrintToChatAll(Localizer["command.slap"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{damage}", damage.ToString()));
            return true;
        }

        private bool ToggleFreeze(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (AlivePawn(player) is not { } pawn)
            {
                return false;
            }
            bool freeze = !IsFrozen(pawn);
            SetMoveType(pawn, freeze ? MoveType_t.MOVETYPE_OBSOLETE : MoveType_t.MOVETYPE_WALK);
            LogAction(admin, freeze ? "freeze" : "unfreeze", player);
            Server.PrintToChatAll(Localizer[freeze ? "command.freeze" : "command.unfreeze"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool ToggleNoclip(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (AlivePawn(player) is not { } pawn)
            {
                return false;
            }
            bool noclip = !IsNoclip(pawn);
            SetMoveType(pawn, noclip ? MoveType_t.MOVETYPE_NOCLIP : MoveType_t.MOVETYPE_WALK);
            LogAction(admin, "noclip", player, noclip ? "on" : "off");
            Server.PrintToChatAll(Localizer[noclip ? "command.noclip" : "command.noclipoff"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool ToggleGod(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (AlivePawn(player) is not { } pawn)
            {
                return false;
            }
            bool god = pawn.TakesDamage;
            pawn.TakesDamage = !god;
            LogAction(admin, "god", player, god ? "on" : "off");
            Server.PrintToChatAll(Localizer[god ? "command.god" : "command.godoff"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private static int GetPlayerValue(CCSPlayerController player, PlayerValue value) => value switch
        {
            PlayerValue.Health => AlivePawn(player)?.Health ?? 0,
            PlayerValue.Armor => AlivePawn(player)?.ArmorValue ?? 0,
            PlayerValue.Money => player.InGameMoneyServices?.Account ?? 0,
            _ => 0
        };

        private static int MaxPlayerValue(PlayerValue value) => value == PlayerValue.Money
            ? Math.Max(0, (int)CvarNumber("mp_maxmoney", ValueSteps[(int)PlayerValue.Money].Max))
            : ValueSteps[(int)value].Max;

        private bool SetPlayerValue(CCSPlayerController player, PlayerValue value, int amount, CCSPlayerController? admin = null)
        {
            amount = Math.Clamp(amount, ValueSteps[(int)value].Min, MaxPlayerValue(value));
            switch (value)
            {
                case PlayerValue.Health:
                    if (AlivePawn(player) is not { } healthPawn)
                    {
                        return false;
                    }
                    if (amount > healthPawn.MaxHealth)
                    {
                        healthPawn.MaxHealth = amount;
                        Utilities.SetStateChanged(healthPawn, "CBaseEntity", "m_iMaxHealth");
                    }
                    healthPawn.Health = amount;
                    Utilities.SetStateChanged(healthPawn, "CBaseEntity", "m_iHealth");
                    break;
                case PlayerValue.Armor:
                    if (AlivePawn(player) is not { } armorPawn)
                    {
                        return false;
                    }
                    armorPawn.ArmorValue = amount;
                    Utilities.SetStateChanged(armorPawn, "CCSPlayerPawn", "m_ArmorValue");
                    break;
                case PlayerValue.Money:
                    if (player.InGameMoneyServices is not { } money)
                    {
                        return false;
                    }
                    money.Account = amount;
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                    break;
                default:
                    return false;
            }
            string key = AdminMenuIds.PlayerStepKeys[(int)value];
            LogAction(admin, key, player, amount.ToString());
            Server.PrintToChatAll(Localizer["command.setvalue"].Value
                .Replace("{player}", player.PlayerName)
                .Replace("{value}", Localizer[$"menu.ps.{key}"].Value)
                .Replace("{amount}", amount.ToString()));
            return true;
        }

        private bool StripPlayer(CCSPlayerController player, CCSPlayerController? admin = null)
        {
            if (AlivePawn(player) is null)
            {
                return false;
            }
            player.RemoveWeapons();
            // a player without any weapon cannot switch slots: give the knife back
            _ = player.GiveNamedItem("weapon_knife");
            LogAction(admin, "strip", player);
            Server.PrintToChatAll(Localizer["command.strip"].Value
                .Replace("{player}", player.PlayerName));
            return true;
        }

        private bool RenamePlayer(CCSPlayerController player, string name, CCSPlayerController? admin = null)
        {
            name = name.Trim();
            if (player is not { IsValid: true }
                || name.Length == 0)
            {
                return false;
            }
            if (name.Length > 32)
            {
                name = name[..32];
            }
            string old = player.PlayerName;
            player.PlayerName = name;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            LogAction(admin, "rename", old, player.IsBot ? string.Empty : player.SteamID.ToString(), name);
            Server.PrintToChatAll(Localizer["command.rename"].Value
                .Replace("{player}", old)
                .Replace("{name}", name));
            return true;
        }

        /// <summary>Bring: <paramref name="target"/> to <paramref name="admin"/>. Goto: the admin to the target.</summary>
        private bool TeleportPlayers(CCSPlayerController admin, CCSPlayerController target, bool bring)
        {
            if (AlivePawn(admin) is not { } adminPawn
                || AlivePawn(target) is not { } targetPawn
                || admin.Slot == target.Slot)
            {
                return false;
            }
            CCSPlayerPawn from = bring ? adminPawn : targetPawn;
            CCSPlayerPawn moved = bring ? targetPawn : adminPawn;
            if (from.AbsOrigin is not { } origin)
            {
                return false;
            }
            moved.Teleport(new Vector(origin.X, origin.Y, origin.Z), null, new Vector(0, 0, 0));
            LogAction(admin, bring ? "bring" : "goto", target);
            Server.PrintToChatAll(Localizer[bring ? "command.bring" : "command.goto"].Value
                .Replace("{admin}", admin.PlayerName)
                .Replace("{player}", target.PlayerName));
            return true;
        }
    }
}
