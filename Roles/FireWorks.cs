using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

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
        static int Id = 1700;

        static CustomOption FireWorksCount;
        static CustomOption FireWorksRadius;

        static Dictionary<byte, int> nowFireWorksCount = new();
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
            Logger.info($"Player{playerId}:SendRPC", "FireWorks");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendFireWorksState, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(nowFireWorksCount[playerId]);
            writer.Write((int)state[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RecieveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            nowFireWorksCount[playerId] = msg.ReadInt32();
            state[playerId] = (FireWorksState)msg.ReadInt32();
            Logger.info($"Player{playerId}:RecieveRPC", "FireWorks");
        }

        public static bool CanUseKillButton(PlayerControl pc)
        {
            Logger.info($"FireWorks CanUseKillButton");
            if (pc.Data.IsDead) return false;
            var canUse = false;
            if ((state[pc.PlayerId] & FireWorksState.CanUseKill) != 0)
            {
                canUse = true;
            }
            Logger.info($" CanUseKillButton:{canUse}");
            return canUse;
        }

        public static void ShapeShiftState(PlayerControl pc, bool shapeshifting)
        {
            Logger.info($"FireWorks ShapeShift");
            if (pc == null || pc.Data.IsDead || !shapeshifting) return;
            switch (state[pc.PlayerId])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    Logger.info("花火を一個設置");
                    fireWorksPosition[pc.PlayerId].Add(pc.transform.position);
                    nowFireWorksCount[pc.PlayerId]--;
                    if (nowFireWorksCount[pc.PlayerId] == 0)
                    {
                        if (main.AliveImpostorCount <= 1)
                        {
                            state[pc.PlayerId] = FireWorksState.ReadyFire;
                        }
                        else
                        {
                            state[pc.PlayerId] = FireWorksState.WaitTime;
                        }
                    }
                    else
                    {
                        state[pc.PlayerId] = FireWorksState.SettingFireWorks;
                    }
                    break;
                case FireWorksState.ReadyFire:
                    Logger.info("花火を爆破");
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
                                PlayerState.setDeathReason(target.PlayerId, PlayerState.DeathReason.Bombed);
                                target.RpcMurderPlayer(target);
                            }
                        }
                    }
                    if (suicide)
                    {
                        PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
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

            if (state[pc.PlayerId] == FireWorksState.WaitTime && main.AliveImpostorCount <= 1)
            {
                Logger.info("爆破準備OK", "FireWorks");
                state[pc.PlayerId] = FireWorksState.ReadyFire;
                SendRPC(pc.PlayerId);
                Utils.NotifyRoles();
            }
            switch (state[pc.PlayerId])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    retText = $"Put {nowFireWorksCount[pc.PlayerId]} Fireworks";
                    break;
                case FireWorksState.WaitTime:
                    retText = "Wait for that time";
                    break;
                case FireWorksState.ReadyFire:
                    retText = "Ready To Fire";
                    break;
                case FireWorksState.FireEnd:
                    break;
            }
            return retText;
        }
    }
}
