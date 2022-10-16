using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Medium
    {
        private static readonly int Id = 5000000;
        public static List<byte> playerIdList = new();
        public static CustomOption MediumCooldown;
        public static CustomOption MediumOneTimeUse;
        public static Dictionary<byte, float> Cooldown = new();
        public static Dictionary<byte, bool> MediumUsed = new();
        public static Dictionary<byte, bool> CanMedium = new();
        public static Dictionary<byte, float> DeadTimer = new();
        public static Dictionary<byte, byte> Killer = new();
        public static List<byte> Target = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
            MediumCooldown = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "MediumCooldown", 30f, 5f, 120f, 5f, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
            MediumOneTimeUse = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "MediumOneTimeUse", false, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
        }
        public static void Init()
        {
            playerIdList = new();
            Cooldown = new();
            MediumUsed = new();
            CanMedium = new();
            DeadTimer = new();
            Killer = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            MediumUsed.Add(playerId, false);
            CanMedium.Add(playerId, false);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyGameOptions(GameOptionsData opt)
        {
            opt.RoleOptions.ScientistCooldown = MediumCooldown.GetFloat();
            opt.RoleOptions.ScientistBatteryCharge = 1f;
        }
        public static void FixedUpdate(PlayerControl target)
        {
            if (GameStates.IsInTask && Target.Contains(target.PlayerId))
            {
                DeadTimer[target.PlayerId] += Time.fixedDeltaTime; ;
            }
        }
        public static PlayerControl GetKiller(byte targetId)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return null;
            Killer.TryGetValue(targetId, out var killerId);
            var killer = Utils.GetPlayerById(killerId);
            return killer;
        }
        public static void UseAbility()
        {
            //Mediumの能力使用
            new LateTask(() =>
            {
                foreach (var reporter in PlayerControl.AllPlayerControls)
                {
                    reporter.RpcResetAbilityCooldown();
                    var rand = new System.Random();
                    int Mode = rand.Next(1, 6);  //ランダムに
                    if (!(reporter.Is(CustomRoles.Medium) && reporter.IsAlive())) continue;
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        if (reporter == target) continue;
                        if (!target.Data.IsDead) continue;
                        string TargetPlayerName = target.GetRealName(true);
                        var killer = GetKiller(target.PlayerId);
                        string deadtime = DeadTimer[target.PlayerId].ToString("F0");
                        switch (Mode)
                        {
                            case 1:
                                Utils.SendMessage($"{TargetPlayerName}の死因は{Utils.GetVitalText(target.PlayerId)}です。", reporter.PlayerId);
                                break;
                            case 2:
                                Utils.SendMessage($"{TargetPlayerName}の役職は{target.GetRoleName()}でした。", reporter.PlayerId);
                                break;
                            case 3:
                                Utils.SendMessage($"{TargetPlayerName}を殺した人の役職は{killer.GetRoleName()}です。", reporter.PlayerId);
                                break;
                            case 4:
                                Utils.SendMessage($"{TargetPlayerName}を殺した人の色のタイプは{killer.GetColorType()}です。", reporter.PlayerId);
                                break;
                            case 5:
                                Utils.SendMessage($"{TargetPlayerName}が殺されたのは{deadtime}秒前です", reporter.PlayerId);
                                break;
                        }
                    }
                }
            }, 5f, "UseMediumAbility");
        }
    }
}