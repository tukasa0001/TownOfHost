using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using UnityEngine;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Monkey : RoleBase
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Monkey),
                player => new Monkey(player),
                CustomRoles.Monkey,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Animals,
                25000,
                SetupOptionItem,
                "モンキー",
                "#FF8C00",
                countType: CountTypes.Animals
            );
        public Monkey(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.ForRecompute
        )
        {
            canKillAnimals = OptionCanKillAnimals.GetBool();
            canKill = false;
        }

        //アニマルズは1人のみなのでstaticでﾖｼｯ!
        private static bool canKill = false;

        private static OptionItem OptionCanKillAnimals;

        private static bool canKillAnimals;

        enum OptionName
        {
            MonkeyKillAnimals
        }

        private static void SetupOptionItem()
        {
            OptionCanKillAnimals = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MonkeyKillAnimals, true, false);
        }

        public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (exiled.PlayerId != Player.PlayerId) return;
            if (!IsTaskFinished) return;
            Vulture.AnimalsBomb();
            Vulture.AnimalsWin();
        }

        public override void OnStartMeeting()
        {
            canKill = false;
        }

        public override void OnFixedUpdate(PlayerControl pc)
        {
            //キルできないなら関係なし
            if (!canKill) return;

            //死んでいるなら関係なし
            if (!pc.IsAlive()) return;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId == pc.PlayerId) continue;

                if (!canKillAnimals)
                {
                    var cRole = target.GetCustomRole();
                    if (cRole.GetCustomRoleTypes() == CustomRoleTypes.Animals) continue;
                }

                var dis = Vector2.Distance(pc.transform.position, target.transform.position);

                //重なってないなら関係なし
                if (dis > 1f) continue;

                //重なった
                pc.RpcMurderPlayer(target);
                canKill = false;

                //これ以上処理する必要はないので終わり
                break;
            }
        }

        public static void CheckKillAnimals(PlayerControl killer)
        {
            //サルがおらんなら関係なし
            if (!CustomRoles.Monkey.IsEnable()) return;

            var cRole = killer.GetCustomRole();
            //モンキーキルは関係なし
            if (cRole == CustomRoles.Monkey) return;

            //アニマルズでなければ関係なし
            if (cRole.GetCustomRoleTypes() != CustomRoleTypes.Animals) return;

            //アニマルズのキル
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if(pc.GetCustomRole() != CustomRoles.Monkey) continue;
                Utils.KillFlash(pc);
                canKill = true;
            }


        }

    }
}