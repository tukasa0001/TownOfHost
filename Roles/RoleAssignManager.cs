using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles
{
    public static class RoleAssignManager
    {
        private static readonly int idStart = 500;
        class RandomAssignOptions
        {
            public int Min => min();
            private Func<int> min;
            public int Max => max();
            private Func<int> max;

            private RandomAssignOptions(int id, OptionItem parent, CustomRoleTypes roleTypes, int maxCount)
            {
                var replacementDictionary = new Dictionary<string, string>()
                { { "%roleType%", GetString( $"CustomRoleTypes.{roleTypes}") } };

                var minOption = IntegerOptionItem.Create(idStart + id + 1, "RoleTypeMin", new(0, maxCount, 1), 0, TabGroup.MainSettings, false)
                    .SetParent(parent)
                    .SetValueFormat(OptionFormat.Players);
                var maxOption = IntegerOptionItem.Create(idStart + id + 2, "RoleTypeMax", new(0, maxCount, 1), 0, TabGroup.MainSettings, false)
                    .SetParent(parent)
                    .SetValueFormat(OptionFormat.Players);

                minOption.ReplacementDictionary =
                maxOption.ReplacementDictionary = replacementDictionary;

                min = () => minOption.GetInt();
                max = () => maxOption.GetInt();

                RandomAssignOptionsCollection.Add(roleTypes, this);
            }
            public static RandomAssignOptions Create(int id, OptionItem parent, CustomRoleTypes roleTypes, int maxCount = 15)
                => new(id, parent, roleTypes, maxCount);
        }
        private static AssignAlgorithm AssignMode => assignMode();
        private static Func<AssignAlgorithm> assignMode;
        private enum AssignAlgorithm
        {
            Fixed,
            Random
        }
        private static readonly string[] AssignModeSelections =
        {
            "AssignAlgorithm.Fixed",
            "AssignAlgorithm.Random"
        };
        private static readonly CustomRoles[] AllMainRoles = CustomRolesHelper.AllRoles.Where(role => role < CustomRoles.NotAssigned).ToArray();
        public static OptionItem OptionAssignMode;
        private static Dictionary<CustomRoleTypes, RandomAssignOptions> RandomAssignOptionsCollection = new(CustomRolesHelper.AllRoleTypes.Length);
        private static Dictionary<CustomRoleTypes, int> AssignCount = new(CustomRolesHelper.AllRoleTypes.Length);
        private static List<CustomRoles> AssignRoleList = new(CustomRolesHelper.AllRoles.Length);
        public static void SetupOptionItem()
        {
            OptionAssignMode = StringOptionItem.Create(idStart, "AssignMode", AssignModeSelections, 0, TabGroup.MainSettings, false)
                .SetHeader(true);

            assignMode = () => (AssignAlgorithm)OptionAssignMode.GetInt();
            RandomAssignOptionsCollection.Clear();
            RandomAssignOptions.Create(10, OptionAssignMode, CustomRoleTypes.Impostor, 3);
            RandomAssignOptions.Create(20, OptionAssignMode, CustomRoleTypes.Madmate);
            RandomAssignOptions.Create(30, OptionAssignMode, CustomRoleTypes.Crewmate);
            RandomAssignOptions.Create(40, OptionAssignMode, CustomRoleTypes.Neutral);
        }
        public static bool CheckRoleCount()
        {
            if (AssignMode == AssignAlgorithm.Fixed) return true;
            var result = true;
            var opt = Main.NormalOptions.Cast<IGameOptions>();

            var playerCount = GameData.Instance.PlayerCount;
            var numImpostors = Math.Min(playerCount, opt.GetInt(Int32OptionNames.NumImpostors));

            var impOptions = RandomAssignOptionsCollection[CustomRoleTypes.Impostor];

            var min = impOptions.Min;
            var max = impOptions.Max;
            if (min > max || min > numImpostors || max > numImpostors)
            {
                var msg = GetString("Warning.NotMatchImpostorCount");
                Logger.SendInGame(msg);
                Logger.Warn(msg, "BeginGame");
                result = false;
            }
            var roleMinCount = 0;
            foreach (var options in RandomAssignOptionsCollection.Values)
                roleMinCount += options.Min;
            if (roleMinCount > playerCount)
            {
                var msg = GetString("Warning.NotMatchRoleCount");
                Logger.SendInGame(msg);
                Logger.Warn(msg, "BeginGame");
                result = false;
            }

            return result;
        }
        public static void SelectAssignRoles()
        {
            AssignCount.Clear();
            AssignRoleList.Clear();

            switch (AssignMode)
            {
                case AssignAlgorithm.Fixed:
                    SetFixedAssignRole();
                    SetAddOnsList(true);
                    break;
                case AssignAlgorithm.Random:
                    SetRandomAssignCount();
                    SetRandomAssignRoleList();
                    SetAddOnsList(false);
                    break;
            }

            AssignRoleList.Sort();
            Logger.Info($"{string.Join(", ", AssignCount)}", "AssignCount");
            Logger.Info($"{string.Join(", ", AssignRoleList)}", "AssignRoleList");
        }
        ///<summary>
        ///役職の固定アサイン抽選
        ///chanceが10%以上の役職を全て追加
        ///</summary>
        private static void SetFixedAssignRole()
        {
            int numImpostorsLeft = Math.Min(GameData.Instance.PlayerCount, Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors));
            //インポスター以外の人数
            //マッド、クルー、ニュートラル合計の限界値
            int numOthersLeft = GameData.Instance.PlayerCount - numImpostorsLeft;

            foreach (var role in GetCandidateRoleList(10).OrderBy(x => Guid.NewGuid()))
            {
                if (numImpostorsLeft <= 0 && numOthersLeft <= 0) break;

                var targetRoles = role.GetAssignUnitRolesArray();
                var numImpostorAssign = targetRoles.Count(role => role.IsImpostor());
                var numOthersAssign = targetRoles.Length - numImpostorAssign;
                //アサイン枠が足りてない場合
                if (numImpostorAssign > numImpostorsLeft
                || numOthersAssign > numOthersLeft) continue;

                AssignRoleList.AddRange(targetRoles);
                numImpostorsLeft -= numImpostorAssign;
                numOthersLeft -= numOthersAssign;
            }

            foreach (var roleType in CustomRolesHelper.AllRoleTypes)
            {
                var count = AssignRoleList.Count(role => role.GetCustomRoleTypes() == roleType);
                AssignCount.Add(roleType, count);
            }
        }
        ///<summary>
        ///設定と実際の人数から各役職のアサイン数を決定
        ///</summary>
        private static void SetRandomAssignCount()
        {

            var rand = IRandom.Instance;
            int numImpostors = Math.Min(GameData.Instance.PlayerCount, Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors));
            //インポスター以外の人数
            //マッド、クルー、ニュートラル合計の限界値
            int numOthers = GameData.Instance.PlayerCount - numImpostors;

            List<CustomRoleTypes> otherRoleTypesList = new();
            if (numOthers > 0) //マッド、クルー、ニュートラルの人数決定
            {
                var otherRoleTypesOptions = RandomAssignOptionsCollection.Where(x => x.Key != CustomRoleTypes.Impostor);
                //一旦最少人数を設定
                foreach (var (roleType, options) in otherRoleTypesOptions)
                    otherRoleTypesList.AddRange(Enumerable.Repeat(roleType, options.Min).ToList());

                //超えている場合はランダムに削除
                while (otherRoleTypesList.Count > numOthers)
                    otherRoleTypesList.RemoveAt(rand.Next(otherRoleTypesList.Count));

                int numAdditional = numOthers - otherRoleTypesList.Count;
                if (numAdditional > 0) //最少人数で限界値に満たない場合
                {
                    List<CustomRoleTypes> additionalList = new();
                    foreach (var (roleType, options) in otherRoleTypesOptions)
                    {
                        //追加人数を取得
                        int additionalCount = Math.Max(0, rand.Next(options.Max - options.Min + 1));

                        additionalList.AddRange(Enumerable.Repeat(roleType, additionalCount).ToList());
                    }

                    //超えている場合はランダムに削除
                    while (additionalList.Count > numAdditional)
                        additionalList.RemoveAt(rand.Next(additionalList.Count));

                    otherRoleTypesList.AddRange(additionalList);
                }
            }

            //Dictionaryに変換
            foreach (var (roleTypes, options) in RandomAssignOptionsCollection)
            {
                if (roleTypes == CustomRoleTypes.Impostor)
                {
                    int impAssignCount = Math.Min(numImpostors, rand.Next(options.Min, options.Max + 1));
                    AssignCount.Add(roleTypes, impAssignCount);
                }
                else
                    AssignCount.Add(roleTypes, otherRoleTypesList.Count(x => x == roleTypes));
            }
        }
        ///<summary>
        ///役職のアサイン抽選
        ///既に決まったアサイン枠数に合わせて決定
        ///</summary>
        private static void SetRandomAssignRoleList()
        {
            List<(CustomRoles, int)> randomRoleTicketPool = new(); //ランダム抽選時のプール
            var rand = IRandom.Instance;
            var assignCount = new Dictionary<CustomRoleTypes, int>(AssignCount); //アサイン枠のDictionary

            foreach (var role in GetCandidateRoleList(100).OrderBy(x => Guid.NewGuid()))
            {
                var targetRoles = role.GetAssignUnitRolesArray();
                //アサイン枠が足りてない場合
                if (CustomRolesHelper.AllRoleTypes.Any(
                    type => assignCount.TryGetValue(type, out var count) &&
                    targetRoles.Count(role => role.GetCustomRoleTypes() == type) > count
                )) continue;

                foreach (var targetRole in targetRoles)
                {
                    AssignRoleList.Add(targetRole);
                    var targetRoleType = targetRole.GetCustomRoleTypes();
                    if (assignCount.ContainsKey(targetRoleType))
                        assignCount[targetRoleType]--;
                }
            }

            if (assignCount.All(kvp => kvp.Value <= 0)) return;

            foreach (var role in AllMainRoles.OrderBy(x => Guid.NewGuid())) //確定枠が偏らないようにシャッフル
            {
                if (!role.IsAssignable()) continue;

                var chance = role.GetChance();
                var count = role.GetCount();
                if (chance is 0 or 100) continue;
                if (count == 0) continue;
                //確率がそのまま追加枚数に
                for (var i = 0; i < count; i++)
                    randomRoleTicketPool.AddRange(Enumerable.Repeat((role, i), chance / 10).ToList());

            }

            //確定分では足りない場合に抽選を行う
            while (assignCount.Any(kvp => kvp.Value > 0) && randomRoleTicketPool.Count > 0)
            {
                var selectedTicket = randomRoleTicketPool[rand.Next(randomRoleTicketPool.Count)];
                var targetRoles = selectedTicket.Item1.GetAssignUnitRolesArray();
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
        private static void SetAddOnsList(bool isFixedAssign)
        {
            foreach (var subRole in CustomRolesHelper.AllRoles.Where(x => x > CustomRoles.NotAssigned))
            {
                var chance = subRole.GetChance();
                var count = subRole.GetAssignCount();
                if (chance == 0 || count == 0) continue;
                var rnd = IRandom.Instance;
                for (var i = 0; i < count; i++) //役職の単位数ごとに抽選
                    if (isFixedAssign || rnd.Next(100) < chance)
                        AssignRoleList.AddRange(subRole.GetAssignUnitRolesArray());
            }
        }
        private static List<CustomRoles> GetCandidateRoleList(int availableRate)
        {
            var candidateRoleList = new List<CustomRoles>();
            foreach (var role in AllMainRoles)
            {
                if (!role.IsAssignable()) continue;

                var chance = role.GetChance();
                var count = role.GetAssignCount();
                if (chance < availableRate || count == 0) continue;
                candidateRoleList.AddRange(Enumerable.Repeat(role, count).ToList());
            }
            return candidateRoleList;
        }
        private static bool IsAssignable(this CustomRoles role)
            => role switch
            {
                CustomRoles.Crewmate => false,
                CustomRoles.Egoist => Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1,
                _ => true,
            };
        /// <summary>
        /// アサインの抽選回数
        /// </summary>
        private static int GetAssignCount(this CustomRoles role)
        {
            int maximumCount = role.GetCount();
            int assignUnitCount = CustomRoleManager.GetRoleInfo(role)?.AssignUnitCount ??
                role switch
                {
                    CustomRoles.Lovers => 2,
                    _ => 1,
                };
            return maximumCount / assignUnitCount;
        }
        ///<summary>
        ///RoleOptionのKey => 実際にアサインされる役職の配列
        ///両陣営役職、コンビ役職向け
        ///</summary>
        private static CustomRoles[] GetAssignUnitRolesArray(this CustomRoles role)
            => CustomRoleManager.GetRoleInfo(role)?.AssignUnitRoles ??
            role switch
            {
                CustomRoles.Lovers => new CustomRoles[2] { CustomRoles.Lovers, CustomRoles.Lovers },
                _ => new CustomRoles[1] { role },
            };
        public static bool IsPresent(this CustomRoles role) => AssignRoleList.Any(x => x == role);
        public static int GetRealCount(this CustomRoles role) => AssignRoleList.Count(x => x == role);
    }
}