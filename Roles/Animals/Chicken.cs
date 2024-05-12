using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Chicken : RoleBase,IKiller
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Chicken),
                player => new Chicken(player),
                CustomRoles.Chicken,
                () => RoleTypes.Shapeshifter,
                CustomRoleTypes.Animals,
                24800,
                SetupOptionItem,
                "チキン",
                "#FF8C00",
                isDesyncImpostor:true,
                countType: CountTypes.Crew
            );
        public Chicken(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            voteCounter = 0;
            enterButton = true;
            ColorUtility.TryParseHtmlString("#FF8C00", out AnimalsColor);
        }
        enum OptionName
        {
            ChickenVictoryButtonCount,
            ChickenButtonCoolDown
        }

        //Option
        public static OptionItem ChickenVictoryButtonCount;
        public static OptionItem ChickenButtonCoolDown;
        Color AnimalsColor;

        private int voteCounter = 0;

        private bool enterButton = false;

        private static void SetupOptionItem()
        {
            ChickenVictoryButtonCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.ChickenVictoryButtonCount, new(1, 30, 1), 3, false)
                .SetValueFormat(OptionFormat.Times);
            ChickenButtonCoolDown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.ChickenButtonCoolDown, new(5, 60, 5), 25, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = ChickenButtonCoolDown.GetInt();
        }

        public override string GetProgressText(bool comms = false) => Utils.ColorString(AnimalsColor,$"({voteCounter}/{ChickenVictoryButtonCount.GetInt()})" + (enterButton && !GameStates.IsMeeting ? "\n『ボタンを押せ！』" : ""));

        public override bool OnCheckShapeshift(PlayerControl target, ref bool animate) => false;

        public bool CanUseImpostorVentButton() => true;
        public bool CanUseSabotageButton() => false;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            info.DoKill = false;
        }

        public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
        {
            //自分のみ
            if (reporter != Player) return true;
            //死体通報はそのまま通報
            if (target != null) return true;
            //まだ押せない
            if(!enterButton) return false;

            enterButton = false;
            voteCounter++;
            CheckWin();
            Utils.NotifyRoles(SpecifySeer: Player);

            Player.RpcResetAbilityCooldown();
            new LateTask(() =>
            {
                enterButton = true;
                Utils.NotifyRoles(SpecifySeer: Player);
            }, ChickenButtonCoolDown.GetInt(), "ChickenCool");

            return false;
        }
        public override void OnStartMeeting()
        {
            Utils.NotifyRoles(SpecifySeer: Player);
        }

        private void CheckWin()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (voteCounter >= ChickenVictoryButtonCount.GetInt())
            {
                Vulture.AnimalsBomb(CustomDeathReason.Bombed);
                Vulture.AnimalsWin();
            }
        }
    }
}
