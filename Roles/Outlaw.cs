using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using UnityEngine;
using static RoleOptionsData;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Outlaw
    {
        private static readonly int Id = 60000;
        public static List<byte> playerIdList = new();

        public static CustomOption OutlawKillCooldown;
        public static CustomOption OutlawCanVent;
        public static CustomOption OutlawCanUseSabotage;
        public static CustomOption OutlawHasImpostorVision;
        public static CustomOption OutlawCanKill;
        public static CustomOption ChangeRolesAfterTargetKilled;
        public static CustomOption ChangeRolesAfterKilledTarget;
        public static CustomOption CorruptSheriffEnabled;

        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static Dictionary<byte, byte> Target = new();
        public static readonly string[] ChangeRoles =
        {
            CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
        };
        public static readonly CustomRoles[] CRoleChangeRoles =
        {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,
        };
        public static readonly string[] ChangeRolesAfterMurder =
        {
            CustomRoles.Sheriff.ToString(), CustomRoles.CorruptSheriff.ToString(),
        };
        public static readonly CustomRoles[] CRoleChangeRolesAfterMurder =
        {
            CustomRoles.Sheriff, CustomRoles.CorruptSheriff,
        };
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Outlaw, 1);
            OutlawCanVent = CustomOption.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "CanVent", true, Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawHasImpostorVision = CustomOption.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "ImpostorVision", true, Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawCanKill = CustomOption.Create(Id + 12, TabGroup.NeutralRoles, Color.white, "CanKill", true, Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawKillCooldown = CustomOption.Create(Id + 13, TabGroup.NeutralRoles, Color.white, "KillCooldown", 30, 2.5f, 180, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            ChangeRolesAfterTargetKilled = CustomOption.Create(Id + 14, TabGroup.NeutralRoles, Color.white, "OutlawChangeRolesAfterTargetKilled", ChangeRoles, ChangeRoles[1], Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            ChangeRolesAfterKilledTarget = CustomOption.Create(Id + 15, TabGroup.NeutralRoles, Color.white, "OutlawChangeRolesAfterKilledTarget", ChangeRolesAfterMurder, ChangeRolesAfterMurder[1], Options.CustomRoleSpawnChances[CustomRoles.Outlaw]);
            //CorruptSheriffEnabled = CustomOption.Create(Id + 15, TabGroup.NeutralRoles, Color.white, "%role%", true, Options.CustomRoleSpawnChances[CustomRoles.Outlaw], replacementDic: new() { { "%role%", Helpers.ColorString(Utils.GetRoleColor(CustomRoles.CorruptSheriff), Utils.GetRoleName(CustomRoles.CorruptSheriff)) } });
        }
        public static void Init()
        {
            playerIdList = new();
            CurrentKillCooldown = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, OutlawKillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}", "Outlaw");

            if (AmongUsClient.Instance.AmHost)
            {
                List<PlayerControl> targetList = new();
                var rand = new System.Random();
                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    if (playerId == target.PlayerId) continue;
                    if (target.Is(CustomRoles.GM)) continue;
                    if (target.Is(CustomRoles.Sheriff))
                        targetList.Add(target);
                }
                var SelectedTarget = targetList[rand.Next(targetList.Count)];
                Target.Add(playerId, SelectedTarget.PlayerId);
                SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
                Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Outlaw");
            } 
        }
        public static int SheriffSpawned()
        {
            int enabled = 0;
            for(int i=0; i <= int.Parse(Sheriff.IsEnable.ToString()) ; i++)
            {
                enabled++;
            }
            return enabled;
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SendRPC(byte outlawId, byte targetId = 0x74, string Progress = "")
        {
            MessageWriter writer;
            switch (Progress)
            {
                case "SetTarget":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOutlawTarget, SendOption.Reliable);
                    writer.Write(outlawId);
                    writer.Write(targetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "":
                    if (!AmongUsClient.Instance.AmHost) return;
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveOutlawTarget, SendOption.Reliable);
                    writer.Write(outlawId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
            }
        }
        public static void ReceiveRPC(MessageReader reader, bool SetTarget)
        {
            if (SetTarget)
            {
                byte OutlawId = reader.ReadByte();
                byte TargetId = reader.ReadByte();
                Target[OutlawId] = TargetId;
            }
            else
                Target.Remove(reader.ReadByte());
        }
        public static void ChangeRoleByTarget(PlayerControl target)
        {
            byte Outlaw = 0x74;
            Target.Do(x =>
            {
                if (x.Value == target.PlayerId)
                    Outlaw = x.Key;
            });
            Utils.GetPlayerById(Outlaw).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetSelection()]);
            Target.Remove(Outlaw);
            SendRPC(Outlaw);
            Utils.NotifyRoles();
        }
        public static void ChangeRole(PlayerControl outlaw)
        {
            outlaw.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetSelection()]);
            Target.Remove(outlaw.PlayerId);
            SendRPC(outlaw.PlayerId);
        } 
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead)
                return false;

            if (!Sheriff.IsEnable)
            {
                if (OutlawCanKill.GetBool())
                    return true;
                return false;
            }
            return true;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target, string Process)
        {
            switch (Process)
            {
                case "Shot Sheriff":
                    if (target.Is(CustomRoles.Sheriff))
                    {
                        PlayerState.SetDeathReason(target.PlayerId, PlayerState.DeathReason.Shot);
                        killer.RpcMurderPlayer(target);
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                        killer.RpcSetCustomRole(CRoleChangeRolesAfterMurder[ChangeRolesAfterKilledTarget.GetSelection()]);
                        Main.AliveImpostorCount++;
                        Target.Remove(killer.PlayerId);
                        SendRPC(killer.PlayerId);
                    }
                    break;

                case "Suicide":
                    if (!target.Is(CustomRoles.Sheriff))
                    {
                        PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Misfire);
                        killer.RpcMurderPlayer(killer);
                        /*if (MisfireKillsTarget.GetBool())
                            killer.RpcMurderPlayer(target);*/
                        return false;
                    }
                    break;
            }
            return true;
        }
    }
}