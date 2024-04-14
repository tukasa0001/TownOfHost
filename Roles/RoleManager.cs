using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices.Achievements;

namespace TownOfHostForE.Roles
{
    internal class BalancerManagement
    {
        public static bool CheckBalancerSkills(byte balncerId, byte targetId, byte killBonus, bool equal)
        {
            var balancer = Utils.GetPlayerById(balncerId);
            var target = Utils.GetPlayerById(targetId);

            int balancerCount = GetCount(balancer, killBonus);
            int targetCount = GetCount(target, killBonus);

            Logger.Info($"カウント1:{balancerCount}", "debug");
            Logger.Info($"カウント2:{targetCount}", "debug");

            return equal ? balancerCount >= targetCount : balancerCount > targetCount;
        }

        private static int GetCount(PlayerControl pc,byte killBonus)
        {
            try
            {
                //戻り値初期
                int returnByte = 0;

                var cRole = pc.GetCustomRole();
                //何れかのキル役職
                if (pc.IsNeutralKiller() ||
                    pc.IsAnimalsKiller() ||
                    cRole.GetCustomRoleTypes() == Core.CustomRoleTypes.Impostor ||
                    pc.IsCrewKiller())
                {
                    Logger.Info($"キルカウント", "debug");
                    returnByte = Main.killCount.ContainsKey(pc.PlayerId) ? Main.killCount[pc.PlayerId] * (int)killBonus : 0;
                }
                //キラーじゃないならタスク数参照
                else
                {
                    Logger.Info($"タスクカウント", "debug");
                    returnByte = pc.GetPlayerTaskState().CompletedTasksCount;
                }

                return returnByte;
            }
            catch(Exception ex)
            {
                Logger.Info($"{ex.Message}:{ex.StackTrace}", "debug");
                return 0;
            }
        }
    }
}
