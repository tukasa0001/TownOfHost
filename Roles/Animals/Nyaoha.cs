using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Neutral;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Nyaoha : RoleBase, IKiller, ISchrodingerCatOwner
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Nyaoha),
                player => new Nyaoha(player),
                CustomRoles.Nyaoha,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24700,
                SetupOptionItem,
                "クサネコ",
                "#FF8C00",
                true,
                countType: CountTypes.Animals
            );
        public Nyaoha(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            SolarBeamCount = OptionSolarBeamCount.GetInt();
            fireDelay = OptionFireDelay.GetInt();

            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);

            UpdateTime = 0;
            NyaohaVector = 0;
            BeamPostion = Vector3.zero;
            NyaohaFireing = new ();
            firechk = false;
            nyaohaKawaiinePosition = new();
            fireCount = -1;
            AnimationIndex = -1;
            secTimer = 0;
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Animals;

        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionSolarBeamCount;
        private static OptionItem OptionFireDelay;
        private static float KillCooldown;
        private float BeamRadius = 3f;
        private int AnimationIndex = -1;

        //時間観測用
        private float UpdateTime;
        private int NyaohaVector = 0;
        private int SolarBeamCount = 0;
        private Vector3 BeamPostion = new();
        private static List<NyaohaFire> NyaohaFireing = new();
        private bool firechk = false;
        private Vector3 nyaohaKawaiinePosition = new ();
        private int fireCount = -1;
        private int fireDelay = 0;
        private int secTimer = 0;

        private Dictionary<int, string> nyaonyaoAnimation = new()
        {
            { 5, "\n　　　\n　│　\n─〇─\n　│　"},
            { 4, "\n　　　　　\n　　│　　\n　＼　／　\n─　〇　─\n　／　＼　\n　　│　　"},
            { 3, "\n　　　　　　　\n　　　│　　　\n　＼　　　／　\n　　　│　　　\n─　─〇─　─\n　　　│　　　\n　／　　　＼　\n　　　│　　　"},
            { 2, "\n　　　　　　　　　\n　　　　│　　　　\n　＼＼│　│／／　\n　＼　　│　　／　\n　─　＼　／　─　\n─　─　〇　─　─\n　─　／　＼　─　\n　／　　│　　＼　\n　／／│　│＼＼　\n　　　　│　　　　"},
            { 1, "\n　　　　　　　　　　　\n　　　　　　　　　　　\n　　　　　│　　　　　\n　＼＼　│　│　／／　\n　＼　　　│　　　／　\n　　　＼│　│／　　　\n　─　─　│　─　─　\n─　─　─〇─　─　─\n　─　─　│　─　─　\n　　　／│　│＼　　　\n　／　　　│　　　＼　\n　／／　│　│　＼＼　\n　　　　　│　　　　　"},
            { 0, "\n　　　　　　　　　　　　　\n　　　　│　│　│　　　　\n　＼＼│　│　│　│／／　\n　＼　　　　│　　　　／　\n　─　＼＼│　│／／　─　\n─　　＼　　│　　／　　─\n　─　─　＼　／　─　─　\n─　─　─　〇　─　─　─\n　─　─　／　＼　─　─　\n─　　／　　│　　＼　　─\n　─　／／│　│＼＼　─　\n　／　　　　│　　　　＼　\n　／／│　│　│　│＼＼　\n　　　　│　│　│　　　　"},
            { 6, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　／　　　　　　　　　　　\n　　　　　　　　　／　　　　　　　　　　　　\n　　　　　　　　／　　　　　　　　　　　　　\n　　　　　　　／／／　　　　　　　　　　　　\n　　　　　　│／　　　　　　　　　　　　　　\n　　　　／　　／─　　　　　　　　　　　　　\n　　　　　─│─　─　　　　　　　　　　　　\n　　　　＼　　＼─　　　　　　　　　　　　　\n　　　　　　│＼　　　　　　　　　　　　　　\n　　　　　　　＼＼＼　　　　　　　　　　　　\n　　　　　　　　＼　　　　　　　　　　　　　\n　　　　　　　　　＼　　　　　　　　　　　　\n　　　　　　　　　　＼　　　　　　　　　　　"},
            { 7, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　／／　　　　　　　　　　　　　　\n　　　　　　／　　　　　　　　　　　　　　　\n　　　／　／　　　　　　　　　　　　　　　　\n　　　　／／／　　　　　　　　　　　　　　　\n　　　│／　─　─　　　　　　　　　　　　　\n　／　　／─　─　─　　　　　　　　　　　　\n　　─│─　─　─　　　　　　　　　　　　　\n　＼　　＼─　─　─　　　　　　　　　　　　\n　　　│＼　─　─　　　　　　　　　　　　　\n　　　　＼＼＼　　　　　　　　　　　　　　　\n　　　＼　＼　　　　　　　　　　　　　　　　\n　　　　　　＼　　　　　　　　　　　　　　　\n　　　　　　＼＼　　　　　　　　　　　　　　"},
            { 8, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　／／　　　　　　　　　　　　　　　　　　\n　　／　　　　　　　　　　　　　　　　　　　\n　／　　　　　　─　　　　　　　　　　　　　\n／／／　　─　　　　　　　　　　　　　　　　\n／　─　─　─　─　　　　　　　　　　　　　\n／─　─　─　─　　　　　　　　　　　　　　\n─　─　─　　　　　　　　　　　　　　　　　\n＼─　─　─　─　─　　　　　　　　　　　　\n＼　─　─　─　─　　　　　　　　　　　　　\n＼＼＼　　─　　　　　　　　　　　　　　　　\n　＼　　　　　　─　　　　　　　　　　　　　\n　　＼　　　　　　　　　　　　　　　　　　　\n　　＼＼　　　　　　　　　　　　　　　　　　"},
            { 9, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　─　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n─　─　　　　　　　　　　　　　　　　　　　\n　─　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　─　─　　　　　　　　　　　　　　　　　　\n─　─　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　─　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　"},
            { 10, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　＼　　　　　　　　　　　\n　　　　　　　　　　　＼　　　　　　　　　　\n　　　　　　　　　　　　＼　　　　　　　　　\n　　　　　　　　　　　＼＼＼　　　　　　　　\n　　　　　　　　　　　　　＼│　　　　　　　\n　　　　　　　　　　　　─＼　　＼　　　　　\n　　　　　　　　　　　─　─│─　　　　　　\n　　　　　　　　　　　　─／　　／　　　　　\n　　　　　　　　　　　　　／│　　　　　　　\n　　　　　　　　　　／／／　　　　　　　　　\n　　　　　　　　　　　／　　　　　　　　　　\n　　　　　　　　　　／　　　　　　　　　　　\n　　　　　　　　　／　　　　　　　　　　　　"},
            { 11, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　＼＼　　　　　　\n　　　　　　　　　　　　　　　＼　　　　　　\n　　　　　　　　　　　　　　　　＼　＼　　　\n　　　　　　　　　　　　　　　＼＼＼　　　　\n　　　　　　　　　　　　　─　─　＼│　　　\n　　　　　　　　　　　　─　─　─＼　　＼　\n　　　　　　　　　　　　　─　─　─│─　　\n　　　　　　　　　　　　─　─　─／　　／　\n　　　　　　　　　　　　　─　─　／│　　　\n　　　　　　　　　　　　　　　／／／　　　　\n　　　　　　　　　　　　　　　　／　／　　　\n　　　　　　　　　　　　　　　／　　　　　　\n　　　　　　　　　　　　　　／／　　　　　　"},
            { 12, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　＼＼　　\n　　　　　　　　　　　　　　　　　　　＼　　\n　　　　　　　　　　　　　─　　　　　　＼　\n　　　　　　　　　　　　　　　　─　　＼＼＼\n　　　　　　　　　　　　　─　─　─　─　＼\n　　　　　　　　　　　　　　─　─　─　─＼\n　　　　　　　　　　　　　　　　　─　─　─\n　　　　　　　　　　─　─　─　─　─　─／\n　　　　　　　　　　　─　─　─　─　─　／\n　　　　　　　　　　　　　　　　─　　／／／\n　　　　　　　　　　　　　─　　　　　　／　\n　　　　　　　　　　　　　　　　　　　／　　\n　　　　　　　　　　　　　　　　　　／／　　"},
            { 13, "\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　─　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　─　─\n　　　　　　　　　　　　　　　　　　　　─　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　─　─　\n　　　　　　　　　　　　　　　　　　　─　─\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　─　　\n　　　　　　　　　　　　　　　　　　　　　　\n　　　　　　　　　　　　　　　　　　　　　　"},
        };
        private enum OptionName
        {
            NyaohaSolarBeamCount,
            NyaohaFireDelay
        }

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionSolarBeamCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.NyaohaSolarBeamCount, new(0, 10, 1), 1, false)
                .SetValueFormat(OptionFormat.None);
            OptionFireDelay = IntegerOptionItem.Create(RoleInfo, 12, OptionName.NyaohaFireDelay, new(0, 60, 5), 30, false)
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override void Add()
        {
            var playerId = Player.PlayerId;
            var MyNyaoha = new NyaohaFire { NyaohaID = playerId, Fired = false};
            NyaohaFireing.Add(MyNyaoha);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
        public override string GetProgressText(bool comms = false)
        {
            string returnString = "";

            if(SolarBeamCount > 0)
            {
                returnString = fireDelay > 0 ? $"「{fireDelay}」" : "『GO!!』";
                returnString += $"({SolarBeamCount})";

                returnString = Utils.ColorString(Color.yellow,returnString);
            }
            else
            {
                returnString = Utils.ColorString(Color.gray, $"「もう力が出なそうだ...」");
            }

            return returnString;
        }
        public bool CanUseSabotageButton() => false;

        public override void AfterMeetingTasks()
        {
            fireDelay = OptionFireDelay.GetInt();
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);

        public override void OnFixedUpdate(PlayerControl player)
        {
            UpdateTime -= Time.fixedDeltaTime;
            if (UpdateTime < 0) UpdateTime = 0.5f; //0.5秒ごとの更新

            if (UpdateTime == 0.5f)
            {
                DelayCheck();
                AnimationCheck();
                FireBeam();
            }
        }

        public override void OnStartMeeting()
        {
            //仮にソーラービームしていたとしても問答無用で止める。
            ResetNyaoha();
        }

        public override void OnTouchPet(PlayerControl player)
        {
            if (SolarBeamCount <= 0) return;
            if (fireDelay > 0) return;
            if (firechk) return;
            firechk = true;
            nyaohaKawaiinePosition = Player.transform.position;
            fireCount = 3;
            AnimationIndex = 0;
            SolarBeamCount--;
            fireDelay = OptionFireDelay.GetInt();
            BeamPostion = Player.transform.position;
            Utils.NotifyRoles(SpecifySeer: Player);
        }
        private void AnimationCheck()
        {
            if (SolarBeamCount <= 0) return;

            //発射中のみ描画
            if (!firechk) return;

            if (AnimationIndex <= nyaonyaoAnimation.Count -1)
            {
                //右向きか
                if (NyaohaVector> 0 && AnimationIndex == 5)
                {
                    AnimationIndex = 10;
                }
                else if(NyaohaVector < 0 && AnimationIndex == 9)
                {
                    AnimationIndex = 15;
                    return;
                }

                AnimationIndex++;
            }
        }

        private void DelayCheck()
        {
            if (fireDelay <= 0) return;
            secTimer++;
            //1秒に1回処理
            if (secTimer < 2) return;
            secTimer = 0;
            fireDelay--;
            Utils.NotifyRoles(SpecifySeer:Player);
        }

        private void FireBeam()
        {
            //撫でたかチェック
            if (!firechk) return;

            //ビーム座標が初期値なら通さない()
            if (BeamPostion == Vector3.zero) return;

            //カウント中は通さない。
            if (fireCount > 0)
            {
                fireCount--;
                Utils.NotifyRoles(Player);
                return;
            }
            else if (fireCount == 0)
            {
                fireCount = -1;
                BeamPostion = Player.transform.position;
                float vectorPos = Player.transform.position.x - nyaohaKawaiinePosition.x;
                if (vectorPos > 0)
                {
                    NyaohaVector = 1;
                }
                else
                {
                    NyaohaVector = -1;
                }

                nyaohaCheck(Player.PlayerId,true);

                Utils.NotifyRoles(SpecifySeer: Player);
                return;
            }

            //3ずつ増やす
            BeamPostion.x += NyaohaVector * 3;
            if (BeamPostion.x <= -50 || BeamPostion.x >= 50)
            {
                ResetNyaoha();
                return;
            }

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId == Player.PlayerId) continue;

                TargetArrow.Add(pc.PlayerId, Player.PlayerId, BeamPostion);
                nyaohaCheckP(Player.PlayerId,BeamPostion);

                var dis = Vector2.Distance(BeamPostion, pc.transform.position);
                if (BeamPostion == Vector3.zero || dis > BeamRadius) continue;

                PlayerState.GetByPlayerId(pc.PlayerId).DeathReason = CustomDeathReason.Beam;
                pc.SetRealKiller(Player);
                pc.RpcMurderPlayer(pc);
            }
            Utils.NotifyRoles(SpecifySeer: Player);
        }

        private void ResetNyaoha()
        {
            BeamPostion = Vector3.zero;
            firechk = false;
            AnimationIndex = -1;
            nyaohaCheck(Player.PlayerId, false);
            nyaohaCheckP(Player.PlayerId, BeamPostion);

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                TargetArrow.RemoveP(pc.PlayerId, Player.PlayerId);
            }

            Utils.NotifyRoles();
        }

        private void nyaohaCheck(byte id,bool change)
        {
            foreach (var nyaoha in NyaohaFireing)
            {
                if (nyaoha.NyaohaID == id)
                {
                    nyaoha.Fired = change;
                    break;
                }
            }
        }
        private void nyaohaCheckP(byte id,Vector3 postion)
        {
            foreach (var nyaoha in NyaohaFireing)
            {
                if (nyaoha.NyaohaID == id)
                {
                    nyaoha.BeamPosition = postion;
                    break;
                }
            }
        }

        private static Vector3 SearchBeamPos(byte nyaohaId)
        {
            Vector3 pos = new();

            foreach (var nya in NyaohaFireing)
            {
                if (nya.NyaohaID == nyaohaId)
                {
                    pos = nya.BeamPosition;
                    break;
                }
            }

            return pos;
        }

        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            //if (GameStates.IsMeeting) return "";
            if (isForMeeting) return "";

            if (seer.PlayerId != seen.PlayerId) return "";

            //ニャオハは自分のビームをみなくていい。
            var cRole = seer.GetCustomRole();
            if (cRole == CustomRoles.Nyaoha) return "";

            //シーアが死んでいたら処理しない。
            if (!seer.IsAlive()) return "";

            List<byte> NyaohaKawaiine = new();
            //
            foreach (var nyaoha in NyaohaFireing)
            {
                if (nyaoha.Fired) NyaohaKawaiine.Add(nyaoha.NyaohaID);
            }

            //誰一人撃ってない
            if (NyaohaKawaiine.Count <= 0) return "";

            string targetSet = "";

            foreach (var nyaoha in NyaohaKawaiine)
            {
                var Nyaoha = Utils.GetPlayerById(nyaoha);
                targetSet = TargetArrow.GetArrowsP(Nyaoha, seer.PlayerId);
                targetSet = $"ｺﾞ{targetSet}ｺﾞ";
                //サイズ変更
                var dis = Vector2.Distance(SearchBeamPos(nyaoha), seer.transform.position);
                if (dis < 4f)
                {
                    targetSet = $"<size=400%>ｺﾞｺﾞｺﾞ{targetSet}ｺﾞｺﾞｺﾞ</size>";
                }
                else if (dis < 9f)
                {
                    targetSet = $"<size=300%>ｺﾞｺﾞ{targetSet}ｺﾞｺﾞ</size>";
                }
                else if (dis < 15f)
                {
                    targetSet = $"<size=200%>ｺﾞ{targetSet}ｺﾞ</size>";
                }
            }

            return Utils.ColorString(RoleInfo.RoleColor, $"\n{targetSet}");
        }

        public override string OverrideSpecialText()
        {
            if (AnimationIndex == -1 || nyaonyaoAnimation.Count <= AnimationIndex) return "";

            return nyaonyaoAnimation[AnimationIndex];
        }

        private class NyaohaFire
        {
            public byte NyaohaID;
            public bool Fired;
            public Vector3 BeamPosition;
        }
    }
}