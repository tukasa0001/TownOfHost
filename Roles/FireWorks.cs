using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class FireWorks
    {
        public enum FireWorksState
        {
            Initial = 1,
            SettingFireWorks = 2,
            WaitTime = 4,
            ReadyFire = 8,
            FireEnd = 16,
            CanUseKill = Initial | FireEnd
        }
        static readonly int Id = 1700;

        static CustomOption FireWorksCount;
        static CustomOption FireWorksRadius;

        public static Dictionary<byte, int> nowFireWorksCount = new();
        static Dictionary<byte, List<Vector3>> fireWorksPosition = new();
        static Dictionary<byte, FireWorksState> state = new();
        static Dictionary<byte, int> fireWorksBombKill = new();

        static int fireWorksCount = 1;
        static float fireWorksRadius = 1;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.FireWorks);
            FireWorksCount = CustomOption.Create(Id + 10, Color.white, "FireWorksMaxCount", 1f, 1f, 3f, 1f, Options.CustomRoleSpawnChances[CustomRoles.FireWorks]);
            FireWorksRadius = CustomOption.Create(Id + 11, Color.white, "FireWorksRadius", 1f, 0.5f, 3f, 0.5f, Options.CustomRoleSpawnChances[CustomRoles.FireWorks]);
        }

        public static void Init()
        {
            nowFireWorksCount = new();
            fireWorksPosition = new();
            state = new();
            fireWorksBombKill = new();
            fireWorksCount = FireWorksCount.GetInt();
            fireWorksRadius = FireWorksRadius.GetFloat();
        }

        public static void Add(byte playerId)
        {
            nowFireWorksCount[playerId] = fireWorksCount;
            fireWorksPosition[playerId] = new();
            state[playerId] = FireWorksState.Initial;
            fireWorksBombKill[playerId] = 0;
        }

        public static void SendRPC(byte playerId)
        {
            Logger.Info($"Player{playerId}:SendRPC", "FireWorks");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendFireWorksState, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(nowFireWorksCount[playerId]);
            writer.Write((int)state[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            nowFireWorksCount[playerId] = msg.ReadInt32();
            state[playerId] = (FireWorksState)msg.ReadInt32();
            Logger.Info($"Player{playerId}:ReceiveRPC", "FireWorks");
        }

        public static bool CanUseKillButton(PlayerControl pc)
        {
//            Logger.Info($"FireWorks CanUseKillButton", "FireWorks");
            if (pc.Data.IsDead) return false;
            var canUse = false;
            if ((state[pc.PlayerId] & FireWorksState.CanUseKill) != 0)
            {
                canUse = true;
            }
//            Logger.Info($"CanUseKillButton:{canUse}", "FireWorks");
            return canUse;
        }

        public static void ShapeShiftState(PlayerControl pc, bool shapeshifting)
        {
            Logger.Info($"FireWorks ShapeShift", "FireWorks");
            if (pc == null || pc.Data.IsDead || !shapeshifting) return;
            switch (state[pc.PlayerId])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    Logger.Info("花火を一個設置", "FireWorks");
                    fireWorksPosition[pc.PlayerId].Add(pc.transform.position);
                    nowFireWorksCount[pc.PlayerId]--;
                    if (nowFireWorksCount[pc.PlayerId] == 0)
                        state[pc.PlayerId] = Main.AliveImpostorCount <= 1 ? FireWorksState.ReadyFire : FireWorksState.WaitTime;
                    else
                        state[pc.PlayerId] = FireWorksState.SettingFireWorks;
                    break;
                case FireWorksState.ReadyFire:
                    Logger.Info("花火を爆破", "FireWorks");
                    bool suicide = false;
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target.Data.IsDead) continue;

                        foreach (var pos in fireWorksPosition[pc.PlayerId])
                        {
                            var dis = Vector2.Distance(pos, target.transform.position);
                            if (dis > fireWorksRadius) continue;

                            if (target == pc)
                            {
                                //自分は後回し
                                suicide = true;
                            }
                            else
                            {
                                PlayerState.SetDeathReason(target.PlayerId, PlayerState.DeathReason.Bombed);
                                target.RpcMurderPlayer(target);
                            }
                        }
                    }
                    if (suicide)
                    {
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        pc.RpcMurderPlayer(pc);
                    }
                    state[pc.PlayerId] = FireWorksState.FireEnd;
                    break;
                default:
                    break;
            }
            SendRPC(pc.PlayerId);
            Utils.NotifyRoles();
        }

        public static string GetStateText(PlayerControl pc, bool isLocal = true)
        {
            string retText = "";
            if (pc == null || pc.Data.IsDead) return retText;

            if (state[pc.PlayerId] == FireWorksState.WaitTime && Main.AliveImpostorCount <= 1)
            {
                Logger.Info("爆破準備OK", "FireWorks");
                state[pc.PlayerId] = FireWorksState.ReadyFire;
                SendRPC(pc.PlayerId);
                Utils.NotifyRoles();
            }
            switch (state[pc.PlayerId])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    retText = string.Format(GetString("FireworksPutPhase"), nowFireWorksCount[pc.PlayerId]);
                    break;
                case FireWorksState.WaitTime:
                    retText = GetString("FireworksWaitPhase");
                    break;
                case FireWorksState.ReadyFire:
                    retText = GetString("FireworksReadyFirePhase");
                    break;
                case FireWorksState.FireEnd:
                    break;
            }
            return retText;
        }
    }
}