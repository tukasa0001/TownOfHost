using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using System.Linq;

namespace TownOfHostForE.Roles.Impostor
{
   public sealed class SuicideBomber : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SuicideBomber),
            player => new SuicideBomber(player),
            CustomRoles.SuicideBomber,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            22100,
            SetupOptionItem,
            "爆裂魔"
        );
        public SuicideBomber(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            NowExpVentFlag = false;
            suicideBomberRadius = SuicideBomberRadius.GetFloat();
            canCreateMadmate = CanCreateMadmate.GetBool();
            NotExpVentSetting = notExpVentSetting.GetBool();
            canSucideDie = CanSucideDie.GetBool();
            trapperBlockMoveTime = TrapperBlockMoveTime.GetFloat();
        }


        static OptionItem SuicideBomberRadius;
        static OptionItem CanCreateMadmate;
        static OptionItem notExpVentSetting;
        static OptionItem TrapperBlockMoveTime;
        static OptionItem CanSucideDie;

        static float suicideBomberRadius = 1;
        static float trapperBlockMoveTime;
        public static bool canCreateMadmate = false;
        public static bool NotExpVentSetting = true;
        //力技フラグ
        public static bool NowExpVentFlag = false;

        public static bool canSucideDie = true;
        bool sucideFlag = false;

        enum OptionName
        {
            SuicideBomberRadius,
            CanCreateMadmate,
            TrapperBlockMoveTime,
            SuicideNotExpVentSetting,
            CanSucideDie,
        }

        private static void SetupOptionItem()
        {
            SuicideBomberRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.SuicideBomberRadius, new(0.5f, 20f, 0.5f), 1f, false)
                .SetValueFormat(OptionFormat.Multiplier);
            CanCreateMadmate = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CanCreateMadmate, false, false);
            TrapperBlockMoveTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.TrapperBlockMoveTime, new(1f, 180f, 1f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds);
            notExpVentSetting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SuicideNotExpVentSetting, false, false);
            CanSucideDie = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CanSucideDie, false, false);
        }

        public override void Add()
        {
            sucideFlag = false;
        }

        public override void OnShapeshift(PlayerControl shapeTarget)
        {
            if (sucideFlag)
            {
                SuicideExploded(Player);
                return;
            }
            var shapeshifting = !Is(shapeTarget);
            Logger.Info($"SuicideBomber ShapeShift", "SuicideBomber");
            if (!shapeshifting) return;
            //以下爆裂確定
            sucideFlag = true;
            bool suicide = false;

            //mod導入者限定で効果音再生
            BGMSettings.PlaySoundSERPC("Boom", Player.PlayerId);

            foreach (var target in Main.AllAlivePlayerControls)
            {
                var pos = Player.transform.position;
                var dis = Vector2.Distance(pos, target.transform.position);
                if (dis > suicideBomberRadius) continue;
                if (target == Player)
                {
                    if(canSucideDie) suicide = true;
                }
                else
                {
                    PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    target.SetRealKiller(Player);
                    target.RpcMurderPlayer(target);
                }

                //自爆魔モードの場合の処理
                if (suicide)
                {
                    var totalAlive = Main.AllAlivePlayerControls.Count();
                    //自分が最後の生き残りの場合は勝利のために死なない
                    if (totalAlive != 1)
                    {
                        MyState.DeathReason = CustomDeathReason.Misfire;
                        Player.RpcMurderPlayer(Player);
                    }
                }
            }
            SuicideExploded(Player);
            Utils.NotifyRoles();
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var killer = info.AttemptKiller;
            var target = info.AppearanceTarget;
            if (sucideFlag)
            {
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
                killer.RpcMurderPlayer(killer);
                if (canCreateMadmate)
                {
                    target.RpcSetCustomRole(CustomRoles.SKMadmate);
                    Logger.Info($"Make SKMadmate:{target.name}", "Shapeshift");
                    Main.SKMadmateNowCount++;
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                }
                info.DoKill = false;
            }
        }
        public static void SuicideExploded(PlayerControl killer)
        {

            Logger.Info($"エクスプロージョン！", "SucideBomber");
            if (!NotExpVentSetting) NowExpVentFlag = false;
            killer.CanUseImpostorVentButton();
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();

            new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                if (!NotExpVentSetting) NowExpVentFlag = true;
                killer.CanUseImpostorVentButton();
                killer.MarkDirtySettings();
            }, trapperBlockMoveTime, "SucideBomb BlockMove");
        }
    }
}
