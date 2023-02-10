using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class SchrodingerCat
    {
        static readonly int Id = 50400;
        static List<byte> playerIdList = new();
        static Color RoleColor = Utils.GetRoleColor(CustomRoles.SchrodingerCat);
        static string RoleColorCode = Utils.GetRoleColorCode(CustomRoles.SchrodingerCat);

        static OptionItem OptionCanWinTheCrewmateBeforeChange;
        static OptionItem OptionChangeTeamWhenExile;
        static OptionItem OptionCanSeeKillableTeammate;

        static bool CanWinTheCrewmateBeforeChange;
        static bool ChangeTeamWhenExile;
        static bool CanSeeKillableTeammate;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingerCat);
            OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(Id + 10, "CanBeforeSchrodingerCatWinTheCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            OptionChangeTeamWhenExile = BooleanOptionItem.Create(Id + 11, "SchrodingerCatExiledTeamChanges", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            OptionCanSeeKillableTeammate = BooleanOptionItem.Create(Id + 12, "SchrodingerCatCanSeeKillableTeammate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
        }
        public static void Init()
        {
            IsEnable = false;
            playerIdList.Clear();

            CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
            ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
            CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
        }
        public static void Add(byte playerId)
        {
            IsEnable = true;
            playerIdList.Add(playerId);
        }
        public static bool IsEnable = false;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            //シュレディンガーの猫が切られた場合の役職変化スタート
            //直接キル出来る役職チェック
            // Sniperなど自殺扱いのものもあるので追加するときは注意
            if (killer.Is(CustomRoles.Arsonist)) return true;

            killer.RpcGuardAndKill(target);

            switch (killer.GetCustomRole())
            {
                case CustomRoles.BountyHunter:
                    if (BountyHunter.GetTarget(killer) == target.PlayerId)
                        BountyHunter.ResetTarget(killer);//ターゲットの選びなおし
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.OnCheckMurder(killer, false);
                    break;
                case CustomRoles.Sheriff:
                    target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                    break;
                case CustomRoles.Egoist:
                    TeamEgoist.Add(target.PlayerId);
                    target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                    break;
                case CustomRoles.Jackal:
                    target.RpcSetCustomRole(CustomRoles.JSchrodingerCat);
                    break;
            }
            if (killer.Is(RoleType.Impostor))
                target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);

            var killerColorCode = killer.GetRoleColorCode();
            if (CanSeeKillableTeammate)
            {
                var roleType = killer.GetCustomRole().GetRoleType();
                System.Func<PlayerControl, bool> isTarget = roleType switch
                {
                    RoleType.Impostor => (pc) => pc.GetCustomRole().GetRoleType() == roleType,
                    _ => (pc) => pc.GetCustomRole() == killer.GetCustomRole()
                };
                ;
                var killerTeam = Main.AllPlayerControls.Where(pc => isTarget(pc));
                foreach (var member in killerTeam)
                {
                    NameColorManager.Instance.RpcAdd(member.PlayerId, target.PlayerId, RoleColorCode);
                    NameColorManager.Instance.RpcAdd(target.PlayerId, member.PlayerId, killerColorCode);
                }
            }
            else
            {
                NameColorManager.Instance.RpcAdd(killer.PlayerId, target.PlayerId, RoleColorCode);
                NameColorManager.Instance.RpcAdd(target.PlayerId, killer.PlayerId, killerColorCode);
            }
            Utils.NotifyRoles();
            Utils.MarkEveryoneDirtySettings();
            //シュレディンガーの猫の役職変化処理終了
            //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
            return false;
        }
        public static void ChangeTeam(PlayerControl player)
        {
            if (!(ChangeTeamWhenExile && player.Is(CustomRoles.SchrodingerCat))) return;

            var rand = IRandom.Instance;
            List<CustomRoles> Rand = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.MSchrodingerCat
            };
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.Is(CustomRoles.Egoist) && !Rand.Contains(CustomRoles.EgoSchrodingerCat))
                    Rand.Add(CustomRoles.EgoSchrodingerCat);

                if (pc.Is(CustomRoles.Jackal) && !Rand.Contains(CustomRoles.JSchrodingerCat))
                    Rand.Add(CustomRoles.JSchrodingerCat);
            }
            var Role = Rand[rand.Next(Rand.Count)];
            player.RpcSetCustomRole(Role);
        }
        public static void CheckAdditionalWin(PlayerControl player)
        {
            if (!player || !player.Is(CustomRoles.SchrodingerCat)) return;

            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange)
            {
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.SchrodingerCat);
            }
        }
    }
}
