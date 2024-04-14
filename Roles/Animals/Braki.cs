using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using TownOfHostForE.Roles.Neutral;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Braki : RoleBase, IKiller, ISchrodingerCatOwner
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Braki),
                player => new Braki(player),
                CustomRoles.Braki,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24400,
                SetupOptionItem,
                "ブラキディオス",
                "#FF8C00",
                true,
                countType: CountTypes.Animals,
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
        public Braki(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            brakiRadius = BrakiRadius.GetFloat();
            CurrentKillCooldown = KillCooldown.GetFloat();
            BittenPlayers.Clear();
            brakiBombTarget.Clear();
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Animals;

        static OptionItem KillCooldown;
        static OptionItem BrakiRadius;

        static float brakiRadius;
        static List<byte> brakiBombTarget = new();
        enum OptionName
        {
            BombRadius
        }


        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BittenPlayers = new(14);
        public float CurrentKillCooldown = 30;

        private static void SetupOptionItem()
        {
            KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            BrakiRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.BombRadius, new(0.5f, 20f, 0.5f), 1f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;
        }
        public float CalculateKillCooldown() => CurrentKillCooldown;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait) || target.Is(CustomRoles.AddBait)) return;
            if (info.IsFakeSuicide) return;
            Logger.Info("Flag:" + BittenPlayers.Keys.Count, "ブラキ");
            if (BittenPlayers.Keys.Count > 0)
            {
                //byte[] keys = new byte[BittenPlayers.Keys.Count];
                var keys = BittenPlayers.Keys.ToArray();
                foreach (var targetId in keys)
                {
                    var VampTarget = Utils.GetPlayerById(targetId);
                    Logger.Info("targetName:" + VampTarget.GetRealName(), "ブラキ");
                    BittenPlayers.Remove(targetId);
                    KillBitten(VampTarget);
                }
            }

            //誰かに噛まれていなければ登録
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
            }
            info.DoKill = false;
        }
        public bool CanUseImpostorVentButton() => true;
        public bool CanUseSabotageButton() => false;

        //public override void OnFixedUpdate(PlayerControl _)
        //{
        //    if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;
        //    if (brakiBombTarget.Count() == 0) return;

        //    foreach (var targetId in brakiBombTarget)
        //    {
        //        var VampTarget = Utils.GetPlayerById(targetId);
        //        if (VampTarget.IsAlive())
        //            KillBitten(VampTarget);
        //        if(BittenPlayers.ContainsKey(targetId))
        //            BittenPlayers.Remove(targetId);
        //        brakiBombTarget.Remove(targetId);
        //    }

        //    ////配列を作成する
        //    //byte[] keys = new byte[BittenPlayers.Keys.Count];
        //    //foreach (var targetId in keys)
        //    //{
        //    //    var VampTarget = Utils.GetPlayerById(targetId);
        //    //    KillBitten(VampTarget);
        //    //    BittenPlayers.Remove(targetId);
        //    //}
        //}
        public override bool OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
        {
            foreach (var targetId in BittenPlayers.Keys)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(target, true);
            }
            BittenPlayers.Clear();

            return true;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("VampireBiteButtonText");
            return true;
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);

        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            var vampire = Player;
            if (target.IsAlive())
            {
                foreach (var BombTarget in Main.AllAlivePlayerControls)
                {
                    var pos = target.transform.position;
                    var dis = Vector2.Distance(pos, BombTarget.transform.position);
                    if (dis > brakiRadius) continue;
                    var playerState = PlayerState.GetByPlayerId(BombTarget.PlayerId);
                    playerState.DeathReason = CustomDeathReason.Bombed;

                    if (BombTarget.PlayerId != vampire.PlayerId)
                    {
                        BombTarget.SetRealKiller(vampire);
                        /*CustomRoleManager.OnCheckMurder(
                            vampire, target,
                            target, target
                        );*/
                        BombTarget.RpcMurderPlayer(BombTarget);
                    }
                }
                if (!isButton && vampire.IsAlive())
                {
                    RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                }
            }
            else
            {
                Logger.Info($"Vampireに噛まれている{target.name}はすでに死んでいました。", "Vampire.KillBitten");
            }
        }
    }
}
