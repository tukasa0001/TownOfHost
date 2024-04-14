using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TownOfHostForE.Attributes;
using TownOfHostForE.Roles.AddOns.Common;

namespace TownOfHostForE.OneTimeAbillitys;

public static class OneTimeAbilittyController
{
    public static readonly int Id = 230000;

    //今付与されている能力管理
    public static Dictionary<byte, HashSet<OneTimeAbility>> nowSettingAbilitys = new();

    //発動出来る能力
    public enum OneTimeAbility
    {
        petKill,    //ドッグシェリフ
        etc = -1
    }


    [GameModuleInitializer]
    public static void GameInit()
    {
        nowSettingAbilitys = new();
    }

    public static void SetOneTimeAbility(PlayerControl pc, OneTimeAbility[] setAbillitys)
    {
        HashSet<OneTimeAbility> tempAbilitys;

        //死んでるなら関係なし
        if (!pc.IsAlive()) return;
        //対象がなければ処理しない
        if (setAbillitys.Count() == 0) return;

        //まず現在の保持に自分のものが存在するか確認
        if (nowSettingAbilitys.ContainsKey(pc.PlayerId))
        {
            //あるならそれを取り出す
            tempAbilitys = nowSettingAbilitys[pc.PlayerId];
        }
        else
        {
            //ないなら新規
            tempAbilitys = new();
        }

        foreach(var setAbility in setAbillitys)
        {
            //無邪気に追加(HashSetなんで重複は入らん)
            tempAbilitys.Add(setAbility);
        }

        //んでセット
        nowSettingAbilitys[pc.PlayerId] = tempAbilitys;
    }

    //ただリムーブする奴
    public static void ResetAbilityAll(byte playerId)
    {
        if (nowSettingAbilitys.ContainsKey(playerId))
            nowSettingAbilitys.Remove(playerId);
    }
    public static void ResetAbility(byte playerId, OneTimeAbility targetAbility)
    {
        if (nowSettingAbilitys.ContainsKey(playerId) == false) return;
        //あるなら外す
        if (nowSettingAbilitys[playerId].Contains(targetAbility))
            nowSettingAbilitys[playerId].Remove(targetAbility);

    }

    public static bool CheckSettingAbilitys(byte playerId,OneTimeAbility targetAbility)
    {
        return nowSettingAbilitys.ContainsKey(playerId) ? nowSettingAbilitys[playerId].Any(ta => ta == targetAbility) : false;
    }

    //ペット系アビリティ実行を纏めるメソッド
    //複数のペットアビリティは同時に付与させてはならない
    public static void ActivationPetAbillitys(PlayerControl pc)
    {
        if (CheckSettingAbilitys(pc.PlayerId, OneTimeAbility.petKill)) PetKill.CheckMurderAsPet(pc);
    }

}
