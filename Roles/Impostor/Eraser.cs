using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using static UnityEngine.GraphicsBuffer;
using Hazel;

namespace TownOfHostForE.Roles.Impostor
{
    public sealed class Eraser : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Eraser),
                player => new Eraser(player),
                CustomRoles.Eraser,
                () => RoleTypes.Shapeshifter,
                CustomRoleTypes.Impostor,
                22700,
                SetUpOptionItem,
                "イレイサー"
            );
        public Eraser(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            AbilityCool = OptionAbilityCool.GetFloat();
            AbilityCount = OptionAbilityCount.GetInt();
            TargetId = byte.MaxValue;
            SetTarget = false;

        }
        private static OptionItem OptionAbilityCool;
        private static OptionItem OptionAbilityCount;
        enum OptionName
        {
            EraserAbilityCool,
            EraserAbilityCount,
        }
        private static float AbilityCool;
        private static float AbilityCount;
        public byte TargetId;
        bool canAbility = false;
        bool SetTarget = false;


        private static void SetUpOptionItem()
        {
            OptionAbilityCool = FloatOptionItem.Create(RoleInfo, 10, OptionName.EraserAbilityCool, new(5f, 900f, 5f), 60f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionAbilityCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EraserAbilityCount, new(1, 15, 1), 2, false)
                .SetValueFormat(OptionFormat.Players);
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = AbilityCool;
            AURoleOptions.ShapeshifterDuration = 1f;
        }

        public override void OnShapeshift(PlayerControl target)
        {
            var shapeshifting = !Is(target);
            if (target == null || !target.IsAlive()) return;
            if (!shapeshifting) return;
            if (AbilityCount == 0) return;
            var cRole = target.GetCustomRole();
            if (cRole.GetCustomRoleTypes() == CustomRoleTypes.Impostor) return;
            TargetId = target.PlayerId;
            canAbility = true;
            SendRPC();
            Player.RpcResetAbilityCooldown();
            Logger.Info("ターゲットセット：" + target.name,"Eraser");
        }
        public override string GetAbilityButtonText() => GetString("EraserWait");
        public override string GetProgressText(bool comms = false) => Utils.ColorString(AbilityCount > 0 ? Color.red : Color.gray, $"({AbilityCount})");
        public override void AfterMeetingTasks()
        {
            if (Player.IsAlive())
                Player.RpcResetAbilityCooldown();
            if (!SetTarget) return;
            var target = Utils.GetPlayerById(TargetId);
            if(target == null) return;
            target.RpcSetCustomRole(CustomRoles.Crewmate);
            TargetId = byte.MaxValue;
            SetTarget = false;
            Logger.Info($"Make Crew:{target.name}", "Eraser");
        }
        private void SendRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.EraserSync);
            sender.Writer.Write(TargetId);
            sender.Writer.Write(AbilityCount);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.EraserSync) return;
            TargetId = reader.ReadByte();
            AbilityCount = reader.ReadInt32();
        }
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!canAbility) return;
            if (TargetId == byte.MaxValue) return;
            var target = Utils.GetPlayerById(TargetId);
            if (target == null || !target.IsAlive()) return;
            float targetDistance = Vector2.Distance(Player.transform.position, target.transform.position);

            var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
            if (targetDistance <= KillRange)
            {
                //player.RpcProtectedMurderPlayer(); //変えたことが分かるように。
                AbilityCount--;
                canAbility = false;
                SetTarget = true;
                Utils.NotifyRoles();
            }

        }
        public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
        {
            if(!SetTarget) TargetId = byte.MaxValue;
            canAbility = false;
            return true;
        }
    }
}