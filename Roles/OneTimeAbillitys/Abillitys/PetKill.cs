using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TownOfHostForE.Attributes;
using TownOfHostForE.Roles.Neutral;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.AddOns.Common;


namespace TownOfHostForE.OneTimeAbillitys;

public static class PetKill
{
    //ペットキル能力詳細
    public static Dictionary<byte, HashSet<KillTargetSettings>> petKillSettings = new ();
    //キルできるプレイヤーを指定する奴
    public static Dictionary<byte, byte[]> petKillTargets = new();


    const float killRadius = 2f;

    [GameModuleInitializer]
    public static void GameInit()
    {
        petKillSettings = new();
        petKillTargets = new();
    }
    //発動出来る能力
    public enum KillTargetSettings
    {
        All,
        Crewmate,       //クルーメイト
        Imposter,       //インポスター
        Madmate,        //マッドメイト
        Neutral,        //第3陣営
        Animals,        //アニマルズ
        KillerOnly,     //キルできる奴のみに縛りたいときはプラスでこれを付ける
        other = -1
    }

    public static void SetPetKillsAbillity(PlayerControl pc, KillTargetSettings[] settings,byte[] playerIds = null)
    {
        if (settings.Count() == 0) return;
        if (!pc.IsAlive()) return;

        //ある場合は一旦リセット
        if(petKillSettings.ContainsKey(pc.PlayerId))
        {
            petKillSettings.Remove(pc.PlayerId);
        }

        //んで付与
        petKillSettings.Add(pc.PlayerId, settings.ToHashSet());

        //プレイヤー指定があれば
        if (playerIds != null)
        {
            petKillTargets.Add(pc.PlayerId, playerIds);
        }

    }

    //ペットを撫でた時に呼ばれる奴
    public static void CheckMurderAsPet(PlayerControl killer)
    {
        Dictionary<float, PlayerControl> KillDic = new();

        //範囲に入っている人算出
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (!pc.IsAlive()) continue;

            if (pc == killer) continue;

            if(returnCheckTargetKills(killer.PlayerId,pc.PlayerId) == false) continue;

            float targetDistance = Vector2.Distance(killer.transform.position, pc.transform.position); ;

            bool checker = targetDistance <= killRadius && pc.CanMove;

            if (!checker) continue;

            KillDic.Add(targetDistance, pc);

        }

        if (KillDic.Count == 0) return;
        //距離が一番近い人算出
        var killTargetKeys = KillDic.Keys.OrderBy(x => x).FirstOrDefault();
        var target = KillDic[killTargetKeys];

        if (!CanBeKilledBy(target))
        {
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
            return;
        }

        target.SetRealKiller(killer);
        target.RpcMurderPlayer(target);
        Utils.MarkEveryoneDirtySettings();
        Utils.NotifyRoles();

        //能力を使ったのでワンタイム設定を外す
        OneTimeAbilittyController.ResetAbility(killer.PlayerId,OneTimeAbilittyController.OneTimeAbility.petKill);
        petKillSettings.Remove(killer.PlayerId);
    }

    /// <summary>
    /// 対象をキルして問題ないか確認するメソッド
    /// </summary>
    /// <param name="killerId"></param>
    /// <param name="targetId"></param>
    /// <returns> true:OK false:NG </returns>
    private static bool returnCheckTargetKills(byte killerId,byte targetId)
    {
        //そもそも指定がなければ良し
        if (petKillTargets.ContainsKey(killerId) == false) return true;

        //指定があればその値
        if(petKillTargets[killerId].Any(id => id == targetId)) return true;

        return false;
    }

    public static bool CanBeKilledBy(PlayerControl player)
    {
        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", "PetKills");
                return false;
            }
            return schrodingerCat.Team switch
            {
                SchrodingerCat.TeamType.Mad => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Madmate),
                SchrodingerCat.TeamType.Crew => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Crewmate),
                _ => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Neutral),
            };
        }

        var cRole = player.GetCustomRole();

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Imposter),
            CustomRoleTypes.Madmate => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Madmate),
            CustomRoleTypes.Neutral => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Neutral),
            CustomRoleTypes.Animals => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Animals),
            CustomRoleTypes.Crewmate => petKillSettings[player.PlayerId].Any(st => st == KillTargetSettings.All || st == KillTargetSettings.Crewmate),
            _ => false,
        };
    }


}
