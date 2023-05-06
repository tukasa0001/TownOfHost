using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles
{
    public static class RoleAssignManager
    {
        private static readonly int idStart = 500;
        private static Dictionary<CustomRoleTypes, int> AssignCount;
        private static List<CustomRoles> AssignRoleList;
        private static OptionItem ImpostorMin;
        private static OptionItem ImpostorMax;
        private static OptionItem MadmateMin;
        private static OptionItem MadmateMax;
        private static OptionItem CrewmateMin;
        private static OptionItem CrewmateMax;
        private static OptionItem NeutralMin;
        private static OptionItem NeutralMax;

        public static void SetupOptionItem()
        {
            ImpostorMin = IntegerOptionItem.Create(idStart, "ImpostorRolesMin", new(0, 3, 1), 0, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            ImpostorMax = IntegerOptionItem.Create(idStart + 1, "ImpostorRolesMax", new(0, 3, 1), 0, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);
            MadmateMin = IntegerOptionItem.Create(idStart + 6, "MadRolesMin", new(0, 15, 1), 0, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);
            MadmateMax = IntegerOptionItem.Create(idStart + 7, "MadRolesMax", new(0, 15, 1), 0, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);

            CrewmateMin = IntegerOptionItem.Create(idStart + 2, "CrewmateRolesMin", new(0, 15, 1), 0, TabGroup.CrewmateRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            CrewmateMax = IntegerOptionItem.Create(idStart + 3, "CrewmateRolesMax", new(0, 15, 1), 0, TabGroup.CrewmateRoles, false)
                .SetValueFormat(OptionFormat.Players);

            NeutralMin = IntegerOptionItem.Create(idStart + 4, "NeutralRolesMin", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            NeutralMax = IntegerOptionItem.Create(idStart + 5, "NeutralRolesMax", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
                .SetValueFormat(OptionFormat.Players);
        }
        public static bool CheckRoleCount()
        {
            var result = true;
            var opt = Main.NormalOptions.Cast<IGameOptions>();

            var playerCount = GameData.Instance.PlayerCount;
            var numImpostors = Math.Min(playerCount, opt.GetInt(Int32OptionNames.NumImpostors));

            var impostorMinCount = ImpostorMin.GetInt();
            var impostorMaxCount = ImpostorMax.GetInt();
            if (impostorMinCount > impostorMaxCount || impostorMinCount > numImpostors || impostorMaxCount > numImpostors)
            {
                var msg = Translator.GetString("Warning.NotMatchImpostorCount");
                Logger.SendInGame(msg);
                Logger.Warn(msg, "BeginGame");
                result = false;
            }

            var roleMinCount = ImpostorMin.GetInt() + MadmateMin.GetInt() + CrewmateMin.GetInt() + NeutralMin.GetInt();
            if (roleMinCount > playerCount)
            {
                var msg = Translator.GetString("Warning.NotMatchRoleCount");
                Logger.SendInGame(msg);
                Logger.Warn(msg, "BeginGame");
                result = false;
            }
            return result;
        }
        public static void SelectAssignRoles()
        {
            SetAssignCount();
            SetAssignRoleList();
            SetAddOnsList();
            AssignRoleList.Sort();
            Logger.Info($"{string.Join(", ", AssignRoleList)}", "SelectAssignRoles");
        }
        ///<summary>
        ///設定と実際の人数から各役職のアサイン数を決定
        ///</summary>
        private static void SetAssignCount()
        {
            AssignCount = new();

            var rand = IRandom.Instance;
            int numImpostors = Math.Min(GameData.Instance.PlayerCount, Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors));
            //インポスター以外の人数
            //マッド、クルー、ニュートラル合計の限界値
            int numOthers = GameData.Instance.PlayerCount - numImpostors;

            List<CustomRoleTypes> otherRoleTypesList = new();
            if (numOthers > 0) //マッド、クルー、ニュートラルの人数決定
            {
                //一旦最少人数を設定
                otherRoleTypesList.AddRange(Enumerable.Repeat(CustomRoleTypes.Madmate, MadmateMin.GetInt()).ToList());
                otherRoleTypesList.AddRange(Enumerable.Repeat(CustomRoleTypes.Crewmate, CrewmateMin.GetInt()).ToList());
                otherRoleTypesList.AddRange(Enumerable.Repeat(CustomRoleTypes.Neutral, NeutralMin.GetInt()).ToList());

                //超えている場合はランダムに削除
                while (otherRoleTypesList.Count > numOthers)
                    otherRoleTypesList.RemoveAt(rand.Next(otherRoleTypesList.Count));

                int numAdditional = numOthers - otherRoleTypesList.Count;
                if (numAdditional > 0) //最少人数で限界値に満たない場合
                {
                    //追加人数を取得
                    int additionalMadCount = Math.Max(0, rand.Next(MadmateMax.GetInt() - MadmateMin.GetInt() + 1));
                    int additionalCrewCount = Math.Max(0, rand.Next(CrewmateMax.GetInt() - CrewmateMin.GetInt() + 1));
                    int additionalNeutralCount = Math.Max(0, rand.Next(NeutralMax.GetInt() - NeutralMin.GetInt() + 1));

                    List<CustomRoleTypes> additionalList = new();
                    additionalList.AddRange(Enumerable.Repeat(CustomRoleTypes.Madmate, additionalMadCount).ToList());
                    additionalList.AddRange(Enumerable.Repeat(CustomRoleTypes.Crewmate, additionalCrewCount).ToList());
                    additionalList.AddRange(Enumerable.Repeat(CustomRoleTypes.Neutral, additionalNeutralCount).ToList());

                    //超えている場合はランダムに削除
                    while (additionalList.Count > numAdditional)
                        additionalList.RemoveAt(rand.Next(additionalList.Count));

                    otherRoleTypesList.AddRange(additionalList);
                }
            }

            //Dictionaryに変換
            foreach (var roleTypes in CustomRolesHelper.AllRoleTypes)
            {
                if (roleTypes == CustomRoleTypes.Impostor)
                {
                    int impAssignCount = Math.Min(numImpostors, rand.Next(ImpostorMin.GetInt(), ImpostorMax.GetInt() + 1));
                    AssignCount.Add(roleTypes, impAssignCount);
                }
                else
                    AssignCount.Add(roleTypes, otherRoleTypesList.Count(x => x == roleTypes));
            }
            Logger.Info($"{string.Join(", ", AssignCount)}", "SetAssignCount");
        }
        ///<summary>
        ///役職のアサイン抽選
        ///既に決まったアサイン枠数に合わせて決定
        ///</summary>
        private static void SetAssignRoleList()
        {
            AssignRoleList = new();
            List<(CustomRoles, int)> randomRoleTicketPool = new(); //ランダム抽選時のプール
            var rand = IRandom.Instance;
            var assignCount = new Dictionary<CustomRoleTypes, int>(AssignCount); //アサイン枠のDictionary

            foreach (var role in CustomRolesHelper.AllRoles.Where(role => role < CustomRoles.NotAssigned).OrderBy(x => Guid.NewGuid())) //確定枠が偏らないようにシャッフル
            {
                if (role == CustomRoles.Crewmate) continue;

                var chance = role.GetChance();
                var count = role.GetCount();
                if (chance == 0 || count == 0) continue;
                if (role == CustomRoles.Egoist && Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;

                for (var i = 0; i < count; i++)
                {
                    var targetRoles = role.GetAssignTargetRolesArray();
                    //アサイン枠が足りてない場合
                    if (CustomRolesHelper.AllRoleTypes.Any(type => targetRoles.Count(role => role.GetCustomRoleTypes() == type) > assignCount[type])) continue;

                    if (chance == 100) //100%ならアサイン枠に直接追加
                    {
                        foreach (var targetRole in targetRoles)
                        {
                            AssignRoleList.Add(targetRole);
                            assignCount[targetRole.GetCustomRoleTypes()]--;
                        }
                    }
                    else //10-90%なら抽選枠にチケット追加
                    {
                        //確率がそのまま追加枚数に
                        randomRoleTicketPool.AddRange(Enumerable.Repeat((role, i), chance / 10).ToList());
                    }
                }
            }

            //確定分では足りない場合に抽選を行う
            while (assignCount.Any(kvp => kvp.Value > 0) && randomRoleTicketPool.Count > 0)
            {
                var selectedTicket = randomRoleTicketPool[rand.Next(randomRoleTicketPool.Count)];
                var targetRoles = selectedTicket.Item1.GetAssignTargetRolesArray();
                //アサイン枠が足りていれば追加
                if (CustomRolesHelper.AllRoleTypes.All(type => targetRoles.Count(role => role.GetCustomRoleTypes() == type) <= assignCount[type]))
                {
                    foreach (var targetRole in targetRoles)
                    {
                        AssignRoleList.Add(targetRole);
                        assignCount[targetRole.GetCustomRoleTypes()]--;
                    }
                }
                //1-9個ある同じチケットを削除
                randomRoleTicketPool.RemoveAll(x => x == selectedTicket);
            }
        }
        ///<summary>
        ///属性のアサイン抽選
        ///枠制限が無いので個別に抽選
        ///</summary>
        private static void SetAddOnsList()
        {
            foreach (var subRole in CustomRolesHelper.AllRoles.Where(x => x > CustomRoles.NotAssigned))
            {
                var chance = subRole.GetChance();
                var count = subRole.GetCount();
                if (chance == 0 || count == 0) continue;
                var numAssignUnit = 1; //アサインの最小単位
                if (subRole == CustomRoles.Lovers)
                    numAssignUnit = 2;
                for (var i = 0; i < count / numAssignUnit; i++) //役職の単位数ごとに抽選
                    if (IRandom.Instance.Next(100) < chance)
                        AssignRoleList.AddRange(subRole.GetAssignTargetRolesArray());
            }
        }
        ///<summary>
        ///RoleOptionのKey => 実際にアサインされる役職の配列
        ///両陣営役職、コンビ役職向け
        ///</summary>
        private static CustomRoles[] GetAssignTargetRolesArray(this CustomRoles role)
            => role switch
            {
                CustomRoles.Watcher => new CustomRoles[1] { IRandom.Instance.Next(100) < Options.EvilWatcherChance.GetInt() ? CustomRoles.EvilWatcher : CustomRoles.NiceWatcher },
                CustomRoles.Lovers => new CustomRoles[2] { CustomRoles.Lovers, CustomRoles.Lovers },
                _ => new CustomRoles[1] { role },
            };
        public static bool IsPresent(this CustomRoles role) => AssignRoleList.Any(x => x == role);
        public static int GetRealCount(this CustomRoles role) => AssignRoleList.Count(x => x == role);
    }
}