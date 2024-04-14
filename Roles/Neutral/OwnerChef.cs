using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral
{
    public sealed class OwnerChef : RoleBase, IKiller
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(OwnerChef),
                player => new OwnerChef(player),
                CustomRoles.OwnerChef,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                50700,
                SetupOptionItem,
                "オーナーシェフ",
                "#00b4eb",
                true,
                countType: CountTypes.Crew,
                assignInfo: new RoleAssignInfo(CustomRoles.Jackal, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public OwnerChef(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            missionCount = OptionMissionCount.GetInt();
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionMissionCount;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static int missionCount;


        //時間観測用
        private float UpdateTime;

        private PlayerControl target = new();
        private string targetLocationName = "";
        private Dictionary<int, string> OrderDic = new()
        {
            {0,"を襲い、食材を入手せよ"},
            {1,"から食材を調達せよ"},
            {2,"料理の時間だ！"},
            {3,"料理を振舞おう！"},
            {4,"生き残れ"}
        };

        private enum Stage
        {
            Mission = 0,
            Alive,
        }
        private enum MissionList
        {
            Raid = 0,
            Harvest,
            Cook,
            Offer,
            Alive,
            Nothing
        }


        private Stage nowStage = Stage.Mission;
        private MissionList nowMission = MissionList.Nothing;

        private uint stageCount = 0;

        private bool nowMissioning = false;

        private string nowString = "";

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
        }

        public override void Add()
        {
            SelectMission();
        }

        public override string GetProgressText(bool comms = false) => nowString;
        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public bool CanUseImpostorVentButton() => false;
        public bool CanUseSabotageButton() => false;

        private void SelectMission()
        {
            if (NowMissioning()) return;

            if (nowStage != Stage.Mission) return;

            if (stageCount < missionCount)
            {
                stageCount++;
                nowMissioning = true;

                System.Random rand = new();
                //01
                int mission = rand.Next(1);

                if (mission == (int)MissionList.Raid)
                {
                    SetMissionRaid();
                }
                //Harvest
                else
                {
                    SetMissionHarvest();
                }
            }
            else
            {
                nowStage = Stage.Alive;
            }
        }

        private void SetMissionRaid()
        {
            List<byte> alivePlayerIds = new();
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId == Player.PlayerId) continue;
                alivePlayerIds.Add(pc.PlayerId);
            }

            System.Random rand = new();
            target = Utils.GetPlayerById(alivePlayerIds[rand.Next(alivePlayerIds.Count)]);

            nowMission = MissionList.Raid;
            nowString = $"{target.name}{OrderDic[0]}";
        }
        private void SetMissionHarvest()
        {

            nowMission = MissionList.Harvest;
        }

        private bool NowMissioning()
        {
            return nowMissioning;
        }

        public override void OnFixedUpdate(PlayerControl player)
        {
            UpdateTime -= Time.fixedDeltaTime;
            if (UpdateTime < 0) UpdateTime = 1f; //1秒ごとの更新

            if (UpdateTime == 1f)
            {

            }
        }

    }
}