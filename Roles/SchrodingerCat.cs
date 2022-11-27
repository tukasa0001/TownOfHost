using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class SchrodingerCat
    {
        private static readonly int Id = 50400;
        public static List<byte> playerIdList = new();

        public static OptionItem CanWinTheCrewmateBeforeChange;
        private static OptionItem ChangeTeamWhenExile;


        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingerCat);
            CanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(Id + 10, "CanBeforeSchrodingerCatWinTheCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            ChangeTeamWhenExile = BooleanOptionItem.Create(Id + 11, "SchrodingerCatExiledTeamChanges", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            //シュレディンガーの猫が切られた場合の役職変化スタート
            //直接キル出来る役職チェック
            // Sniperなど自殺扱いのものもあるので追加するときは注意
            var canDirectKill = !killer.Is(CustomRoles.Arsonist);
            if (canDirectKill)
            {
                killer.RpcGuardAndKill(target);
                if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Sniped)
                {
                    //スナイプされた時
                    target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);
                    var sniperId = Sniper.GetSniper(target.PlayerId);
                    NameColorManager.Instance.RpcAdd(sniperId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                }
                else
                {
                    switch (killer.GetCustomRole())
                    {
                        case CustomRoles.BountyHunter:
                            if (BountyHunter.GetTarget(killer) == target)
                                BountyHunter.ResetTarget(killer);//ターゲットの選びなおし
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
                        default:
                            SerialKiller.OnCheckMurder(killer, false);
                            break;
                    }
                    if (killer.GetCustomRole().IsImpostor())
                        target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);

                    NameColorManager.Instance.RpcAdd(killer.PlayerId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                }
                Utils.NotifyRoles();
                Utils.CustomSyncAllSettings();
                //シュレディンガーの猫の役職変化処理終了
                //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
            }
        }
        public static void ChangeTeam(PlayerControl player)
        {
            if (!(ChangeTeamWhenExile.GetBool() && player.Is(CustomRoles.SchrodingerCat))) return;

            var rand = IRandom.Instance;
            List<CustomRoles> Rand = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.MSchrodingerCat
            };
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Egoist) && !pc.Data.IsDead && Rand.Contains(CustomRoles.EgoSchrodingerCat))
                    Rand.Add(CustomRoles.EgoSchrodingerCat);

                if (pc.Is(CustomRoles.Jackal) && !pc.Data.IsDead && Rand.Contains(CustomRoles.JSchrodingerCat))
                    Rand.Add(CustomRoles.JSchrodingerCat);
            }
            var Role = Rand[rand.Next(Rand.Count)];
            player.RpcSetCustomRole(Role);
        }
    }
}