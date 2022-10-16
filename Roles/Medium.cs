using System;
using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Medium
    {
        private static readonly int Id = 5000000;
        public static List<byte> playerIdList = new();
        public static CustomOption MediumUseNumber;
        public static CustomOption MediumOneTimeUse;
        public static Dictionary<byte, int> UseNumber = new();
        public static Dictionary<byte, float> DeadTimer = new();
        public static Dictionary<byte, byte> Killer = new();
        public static List<byte> Target = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
            MediumUseNumber = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "MediumUseNumber", 3, 1, 5, 1, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
        }
        public static void Init()
        {
            playerIdList = new();
            UseNumber = new();
            DeadTimer = new();
            Killer = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            UseNumber[playerId] = 0;
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyGameOptions(GameOptionsData opt)
        {
            opt.RoleOptions.ScientistCooldown = 255f;
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
        public static void UseAbility(PlayerControl reporter, PlayerControl target)
        {
            //Mediumの能力使用
            new LateTask(() =>
            {
                /*foreach (var reporter in PlayerControl.AllPlayerControls)
                {
                    reporter.RpcResetAbilityCooldown();
                    var rand = new System.Random();
                    int Mode = rand.Next(1, 6);  //ランダムに
                    if (!(reporter.Is(CustomRoles.Medium) && reporter.IsAlive())) continue;
                    if (!CanMedium[reporter.PlayerId] && UseNumber[reporter.PlayerId] > MediumUseNumber.GetFloat()) continue;
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
                }*/
                if (target == null) return;
                var rand = new System.Random();
                int Pattern = rand.Next(1, 6);  //ランダムに
                if (!(reporter.Is(CustomRoles.Medium) && reporter.IsAlive())) return;
                if (reporter == target) return;
                if (!target.Data.IsDead) return;
                if (UseNumber[reporter.PlayerId] > MediumUseNumber.GetInt()) return;
                string TargetPlayerName = target.GetRealName(true);
                var killer = GetKiller(target.PlayerId);
                string deadtime = DeadTimer[target.PlayerId].ToString("F0");
                switch (Pattern)
                {
                    case 1:
                        string pattern1 = string.Format(GetString("CauseOfDeath"), TargetPlayerName, Utils.GetVitalText(target.PlayerId));
                        Utils.SendMessage($"{pattern1}", reporter.PlayerId);
                        break;
                    case 2:
                        string pattern2 = string.Format(GetString("TargetRole"), TargetPlayerName, target.GetRoleName());
                        Utils.SendMessage($"{pattern2}", reporter.PlayerId);
                        break;
                    case 3:
                        string pattern3 = string.Format(GetString("KillerRole"), TargetPlayerName, killer.GetRoleName());
                        Utils.SendMessage($"{pattern3}", reporter.PlayerId);
                        break;
                    case 4:
                        string pattern4 = string.Format(GetString("KillerColorType"), TargetPlayerName, killer.GetColorType());
                        Utils.SendMessage($"{pattern4}", reporter.PlayerId);
                        break;
                    case 5:
                        string pattern5 = string.Format(GetString("DeadTime"), TargetPlayerName, deadtime);
                        Utils.SendMessage($"{pattern5}", reporter.PlayerId);
                        break;
                }
            }, 5f, "UseMediumAbility");
            UseNumber[reporter.PlayerId]++;
        }
    }
}