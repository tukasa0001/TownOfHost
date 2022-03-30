using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace TownOfHost
{
    public class FireWorks
    {
        public enum FireWorksState
        {
            Initial,
            SettingFireWorks,
            WaitTime,
            ReadyFire,
            FireEnd
        }
        private static FireWorks _instance = null;
        List<byte> playerIdList = new();
        List<int> nowFireWorksCount = new();
        List<List<Vector3>> fireWorksPosition = new();
        List<FireWorksState> state = new();
        List<int> fireWorksBombKill = new();

        int maxFireWorksCount = 1;
        float fireWorksRadius = 1;
        private FireWorks()
        {

        }
        private static FireWorks GetInstance()
        {
            if (_instance == null)
            {
                _instance = new FireWorks();
                main.ImpostorRoles.Add(CustomRoles.FireWorks);
                main.ShapeShifterBaseRoles.Add(CustomRoles.FireWorks);
                main.roleColors.Add(CustomRoles.FireWorks, "#ff0000");
            }
            return _instance;
        }

        public FireWorks Check(byte playerId)
        {
            if (!_instance.playerIdList.Contains(playerId)) Add(playerId);
            return _instance;
        }
        public static void Init(int _maxFireWorksCount, float _fireWorksRadius)
        {
            var instance=GetInstance();
            instance.maxFireWorksCount = _maxFireWorksCount;
            instance.fireWorksRadius = _fireWorksRadius;
        }

        public static void Add(byte playerId)
        {
            var instance = GetInstance();
            instance.playerIdList.Add(playerId);
            instance.nowFireWorksCount.Add(_instance.maxFireWorksCount);
            instance.fireWorksPosition.Add(new());
            instance.state.Add(FireWorksState.Initial);
            instance.fireWorksBombKill.Add(0);
        }
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(1700, CustomRoles.FireWorks);
            Options.FireWorksMaxCount = CustomOption.Create(1701, Color.white, "FireWorksMaxCount", 1f, 1f, 3f, 1f, Options.CustomRoleSpawnChances[CustomRoles.FireWorks]);
            Options.FireWorksRadius = CustomOption.Create(1702, Color.white, "FireWorksRadius", 1f, 0.5f, 3f, 0.5f, Options.CustomRoleSpawnChances[CustomRoles.FireWorks]);

        }
        public static  bool CanUseKillButton(PlayerControl pc)
        {
            var instance = GetInstance().Check(pc.PlayerId);
            Logger.info($"FireWorks CanUseKillButton");
            if (pc.Data.IsDead) return false;
            var index = GetPCIndex(pc);
            if (index == -1) return false;
            var canUse = false;
            if (instance.state[index] == FireWorksState.Initial || instance.state[index] == FireWorksState.FireEnd)
            {
                canUse= true;
            }
            Logger.info($" CanUseKillButton:{canUse}");
            return canUse;
        }

        public static void ShapeShiftCheck(PlayerControl pc, bool shapeShifted)
        {
            Logger.info($"FireWorks ShapeShift");
            if (pc == null || pc.Data.IsDead || shapeShifted) return;
            var instance = GetInstance().Check(pc.PlayerId);
            var index = GetPCIndex(pc);
            if(index == -1) return;
            switch (instance.state[index])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    Logger.info("花火を一個設置");
                    instance.fireWorksPosition[index].Add(pc.transform.position);
                    instance.nowFireWorksCount[index]--;
                    if (instance.nowFireWorksCount[index] == 0)
                    {
                        if (main.AliveImpostorsCount == 1)
                        {
                            instance.state[index] = FireWorksState.ReadyFire;
                        }
                        else
                        {
                            instance.state[index] = FireWorksState.WaitTime;
                        }
                    }
                    else
                    {
                        instance.state[index] = FireWorksState.SettingFireWorks;
                    }
                    break;
                case FireWorksState.ReadyFire:
                    Logger.info("花火を爆破");
                    bool suicide = false;
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (!target.Data.IsDead)
                        {
                            foreach (var pos in instance.fireWorksPosition[index])
                            {
                                var dis = Vector2.Distance(pos, target.transform.position);
                                if (dis < instance.fireWorksRadius)
                                {
                                    instance.nowFireWorksCount[index]++;
                                    if (target == pc)
                                    {
                                        //自分は後回し
                                        suicide = true;
                                    }
                                    else
                                    {
                                        PlayerState.setDeathReason(target.PlayerId, PlayerState.DeathReason.Bombed);
                                        target.RpcMurderPlayer(target);
                                        target.RpcGuardAndKill(target);
                                    }
                                }
                            }
                        }
                    }
                    if (suicide)
                    {
                        PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        pc.RpcMurderPlayer(pc);
                        pc.RpcGuardAndKill(pc);
                    }
                    instance.state[index] = FireWorksState.FireEnd;
                    break;
                default:
                    break;
            }
            SendState(pc.PlayerId, (int)instance.state[index]);
            Utils.NotifyRoles();
        }

        public static string GetStateText(PlayerControl pc, bool isLocal = true)
        {
            string retText = "";
            if (pc == null || pc.Data.IsDead) return retText;
            var instance = GetInstance().Check(pc.PlayerId);
            var index = GetPCIndex(pc);
            if(index==-1)return retText;

            if (instance.state[index] == FireWorksState.WaitTime && main.AliveImpostorsCount == 1)
            {
                Logger.info("爆破準備OK", "FireWorks");
                instance.state[index] = FireWorksState.ReadyFire;
                SendState(pc.PlayerId, (int)instance.state[index]);
                Utils.NotifyRoles();
            }
            switch (instance.state[index])
            {
                case FireWorksState.Initial:
                case FireWorksState.SettingFireWorks:
                    retText = $"Put {instance.nowFireWorksCount[index]} Fireworks";
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
        private static int GetPCIndex(PlayerControl pc)
        {
            if(pc == null) return -1;
            var instance = GetInstance().Check(pc.PlayerId);
            var index = instance.playerIdList.IndexOf(pc.PlayerId);
            if (index == -1)
            {
                Logger.info($"PC not Found", "FireWorks");
            }
            return index;

        }
        public static void SendState(byte playerID, int newState)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(playerID,(byte)CustomRPC.SendFireWorksState, Hazel.SendOption.Reliable, -1);
            writer.Write(newState);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RecieveState(byte playerId,int newState)
        {
            var instance = GetInstance().Check(playerId);
            var index = _instance.playerIdList.IndexOf(playerId);
            _instance.state[index]=(FireWorksState)newState;
        }
    }
}
