using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class SerialKiller : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            new(
                typeof(SerialKiller),
                player => new SerialKiller(player),
                CustomRoles.SerialKiller,
                () => RoleTypes.Shapeshifter,
                CustomRoleTypes.Impostor,
                1100,
                SetUpOptionItem,
                "sk"
            );
        public SerialKiller(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            TimeLimit = OptionTimeLimit.GetFloat();

            SuicideTimer = null;
        }
        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionTimeLimit;
        enum OptionName
        {
            KillCooldown,
            SerialKillerLimit
        }
        private static float KillCooldown;
        private static float TimeLimit;

        public float? SuicideTimer;

        private static void SetUpOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionTimeLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.SerialKillerLimit, new(5f, 900f, 5f), 60f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override float SetKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = HasKilled() ? TimeLimit : 255f;
            AURoleOptions.ShapeshifterDuration = 1f;
        }
        ///<summary>
        ///シリアルキラー＋生存＋一人以上キルしている
        ///</summary>
        public bool HasKilled()
            => Player != null && Player.IsAlive() && MyState.GetKillCount(true) > 0;
        public override void OnCheckMurderAsKiller(MurderInfo info)
        {
            var killer = info.AttemptKiller;
            SuicideTimer = null;
            killer.MarkDirtySettings();
        }
        public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
        {
            SuicideTimer = null;

            return true;
        }
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!HasKilled())
            {
                SuicideTimer = null;
                return;
            }
            if (SuicideTimer == null) //タイマーがない
            {
                SuicideTimer = 0f;
                Player.RpcResetAbilityCooldown();
            }
            else if (SuicideTimer >= TimeLimit)
            {
                //自爆時間が来たとき
                MyState.DeathReason = CustomDeathReason.Suicide;//死因：自殺
                Player.RpcMurderPlayer(Player);//自殺させる
                SuicideTimer = null;
            }
            else
                SuicideTimer += Time.fixedDeltaTime;//時間をカウント
        }
        public override string GetAbilityButtonText()
        {
            DestroyableSingleton<HudManager>.Instance.AbilityButton.ToggleVisible(Player.IsAlive() && HasKilled());
            return GetString("SerialKillerSuicideButtonText");
        }
        public override void AfterMeetingTasks()
        {
            if (Player.IsAlive())
            {
                Player.RpcResetAbilityCooldown();
                if (HasKilled())
                    SuicideTimer = 0f;
            }
        }
    }
}