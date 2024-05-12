using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.GameMode;

static class SuperBakuretsuBros
{
    public static readonly int Id = 220000;

    //合計爆弾数(但し進行により減る)
    public static OptionItem MaxBomb;
    //爆裂半径
    public static OptionItem BombRadius;
    //爆発するまでの時間の上限値
    //0から上限値内でランダムに算出する。
    //SBPKillCoolが明けてから5秒後にカウント開始
    public static OptionItem SBPBombTime;
    public static OptionItem SBPHideName;

    private static OptionItem SBPReportTime;

    //爆弾持ちのId
    static HashSet<byte> bombPlayerId = new();
    //時間観測用
    private static float UpdateTime;
    //経過時間観測用
    private static int TotalTime;
    //爆発までのLimit
    private static float BombLimit;
    //初回判定
    private static bool startGame = true;
    //爆裂確定演出
    private static bool bakuretsuKettei = false;
    public static bool ChangeBGM = false;
    //待ち時間
    private static Dictionary<byte, PlayerStopCS> PlayerStopTimeDic = new();
    private static HashSet<byte> watchIds = new();
    private static int stopTime = 0;


    [GameModuleInitializer]
    public static void GameInit()
    {
        startGame = true;
        UpdateTime = 0.9f;
        TotalTime = 0;
        bakuretsuKettei = false;
        ChangeBGM = false;
        //stopTime = SBPReportTime.GetInt();
        ResetBombPlayerAndTime();
    }

    public static void SetupCustomOption()
    {
        MaxBomb = IntegerOptionItem.Create(Id + 1000, "SBPMaxBomb", new(1, 3, 1), 1, TabGroup.MainSettings, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Pieces)
            .SetGameMode(CustomGameMode.SuperBombParty);
        BombRadius = FloatOptionItem.Create(Id + 1001, "SuicideBomberRadius", new(1, 3, 1), 1, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetGameMode(CustomGameMode.SuperBombParty);
        SBPBombTime = IntegerOptionItem.Create(Id + 1002, "SBPBombTime", new(30, 300, 10), 60, TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.SuperBombParty);
        SBPHideName = BooleanOptionItem.Create(Id + 1003, "SBPHideName", false,TabGroup.MainSettings, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.SuperBombParty);
        //SBPReportTime = IntegerOptionItem.Create(Id + 1004, "SBPReportTime", new(0, 30, 1), 5, TabGroup.MainSettings, false)
        //    .SetValueFormat(OptionFormat.Seconds)
        //    .SetGameMode(CustomGameMode.SuperBombParty);
    }

    public static void Add()
    {
        if(stopTime != 0)  ResetWaitTimeDic();
    }

    private static void ResetWaitTimeDic()
    {
        PlayerStopTimeDic = new();
        foreach (var pc in Main.AllPlayerControls)
        {
            PlayerStopCS tempCS = new()
            {
                position = pc.transform.position,
                waitTime = 0
            };
            PlayerStopTimeDic.Add(pc.PlayerId,tempCS);
            watchIds.Add(pc.PlayerId);
            SetTarget(pc.PlayerId);
        }
    }

    public static void CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null)
            return;
        if (!killer.IsAlive() || !target.IsAlive())
            return;

        //確定中は爆弾の受け渡し不可
        if (bakuretsuKettei)
            return;

        //相手が爆弾を所持していなければ爆弾の受け渡し
        if (!bombPlayerId.Contains(target.PlayerId) && bombPlayerId.Contains(killer.PlayerId))
        {
            bombPlayerId.Add(target.PlayerId);
            bombPlayerId.Remove(killer.PlayerId);
            //渡したことを分からせるためとキルクの調整
            target.RpcProtectedMurderPlayer();
        }
        killer.SetKillCooldown();
        Utils.NotifyRoles();
    }
    public static void OnFixedUpdate()
    {

        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1.0f; //1秒ごとの更新

        if (UpdateTime == 1.0f)
        {
            CheckBombLimit();

            CheckStopPlayer();
        }
    }

    private static void CheckStopPlayer()
    {
        //0秒なら居場所を教えない
        if (stopTime == 0) return;

        HashSet<byte> deleteKey = new();
        foreach (var data in PlayerStopTimeDic)
        {
            var pc = Utils.GetPlayerById(data.Key);
            if (pc.IsAlive())
            {
                data.Value.waitTime++;

                //表示まで溜まっていなくて移動している場合
                if (data.Value.waitTime < stopTime && pc.transform.position != data.Value.position)
                {
                    data.Value.waitTime = 0;
                    data.Value.position = pc.transform.position;
                    continue;
                }

                //発見から3秒経過していたら
                if (watchIds.Contains(data.Key) && data.Value.waitTime > stopTime + 3)
                {
                    watchIds.Remove(data.Key);
                    data.Value.waitTime = 0;
                    data.Value.position = pc.transform.position;
                }
                //指定時間同じ時間で止まっていた
                else if (data.Value.waitTime == stopTime)
                {
                    watchIds.Add(data.Key);
                }
            }
            //死んでいる場合
            else
            {
                deleteKey.Add(data.Key);
            }
        }

        foreach (var id in deleteKey)
        {
            PlayerStopTimeDic.Remove(id);
            watchIds.Remove(id);
        }

        if (watchIds.Count > 0)
        {
            Utils.NotifyRoles();
        }
    }

    private static void SetTarget(byte targetId)
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            //本人には通知されない
            if (pc.PlayerId == targetId) return;
            TargetArrow.Add(pc.PlayerId, targetId);

        }
    }

    private static void CheckBombLimit()
    {
        //まず1秒経ってるはずなのでインクリメント
        TotalTime++;
        if (TotalTime >= BombLimit)
        {
            if (!bakuretsuKettei) bakuretsuKettei = true;
            //確定から5秒経過で本爆裂
            if (TotalTime >= BombLimit + 5)
            {
                Dictionary<float, PlayerControl> KillDic = new();

                foreach (var bombId in bombPlayerId)
                {
                    //何らかの事情で生存者が残り1人以下なら抜けて勝者判定へ
                    if (Main.AllAlivePlayerControls.Count() <= 1) break;

                    var pc = Utils.GetPlayerById(bombId);
                    if (!pc.IsAlive()) continue;
                    var pos = pc.transform.position;
                    //まずは自爆優先
                    pc.RpcMurderPlayer(pc);

                    //爆弾持ちが自爆し、生存者が残り1人以下なら抜けて勝者判定へ
                    if (Main.AllAlivePlayerControls.Count() <= 1) break;

                    //その後範囲内の人を滅ぼす為の処理
                    foreach (var target in Main.AllAlivePlayerControls)
                    {
                        var dis = Vector2.Distance(pos, target.transform.position);
                        if (dis > BombRadius.GetFloat()) continue;
                        if (target != pc)
                        {
                            KillDic.Add(dis,target);
                        }
                    }

                }

                //距離が近い順からキル
                foreach (var killsKey in KillDic.Keys.OrderBy(x => x))
                {
                    var target = KillDic[killsKey];
                    PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    //target.SetRealKiller(pc);
                    target.RpcMurderPlayer(target);

                    //生存者が残り1人以下なら抜けて勝者判定へ
                    if (Main.AllAlivePlayerControls.Count() <= 1)
                    {
                        break;
                    }
                }

                bakuretsuKettei = false;

                //勝者判定
                CheckWin();
                //リセット
                ResetBombPlayerAndTime();
                BGMSettings.SetTaskBGM();
            }
            Utils.NotifyRoles();
        }
    }

    private static void ResetBombPlayerAndTime()
    {
        TotalTime = 0;
        bombPlayerId.Clear();
        int maxBomb = MaxBomb.GetInt();
        //現在の生き残り人数と最大爆弾設定数から爆弾の残数を決める。
        //10人以上生き残り
        if (Main.AllAlivePlayerControls.Count() >= 10)
        {
            //10人以上なら設定値通り
        }
        //6人以上生き残り
        else if (Main.AllAlivePlayerControls.Count() >= 6)
        {
            //3つは許さない
            if (maxBomb == 3)
            {
                maxBomb = 2;
            }
        }
        //5人以下
        else
        {
            //問答無用で1
            maxBomb = 1;
        }

        List<PlayerControl> rndList = new();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            rndList.Add(pc);
        }
        System.Random rand = new();
        //爆弾持ち確定
        do
        {
            var pc = rndList[rand.Next(rndList.Count)];
            bombPlayerId.Add(pc.PlayerId);
            rndList.Remove(pc);
            maxBomb--;
        }
        while (maxBomb > 0);

        //爆発時間の確定 最低値30秒
        BombLimit = rand.Next(30,SBPBombTime.GetInt());
        if (startGame)
        {
            //起爆時間にキルク+5秒をプラスする。
            BombLimit = BombLimit + Main.NormalOptions.KillCooldown + 5;
            startGame = false;
        }
    }

    private static void CheckWin()
    {
        if (Main.AllAlivePlayerControls.Count() <= 1)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BAKURETSU);
            foreach (var AlivePlayer in Main.AllAlivePlayerControls)
            {
                CustomWinnerHolder.WinnerIds.Add(AlivePlayer.PlayerId);
            }
        }
    }

    public static string ChatchedBombs(byte playerId)
    {
        string returnString = $"({Main.AllAlivePlayerControls.Count()})";
        foreach (var id in bombPlayerId)
        {
            if (id == playerId)
            {
                if (bakuretsuKettei)
                {
                    int countTime = TotalTime - (int)BombLimit;
                    switch (countTime)
                    {
                        case 0:
                            returnString = "\n <size=100%>!5!</size>";
                            break;
                        case 1:
                            returnString = "\n <size=150%>!4!</size>";
                            break;
                        case 2:
                            returnString = "\n <size=200%>!3!</size>";
                            break;
                        case 3:
                            returnString = "\n <size=250%>!2!</size>";
                            break;
                        case 4:
                            returnString = "\n <size=300%>!1!</size>";
                            break;
                        case 5:
                            returnString = "\n <size=350%>0</size>";
                            break;
                    }
                }
                else
                {
                    //string arrowString = GetArrows(playerId, watchIds.ToArray());
                    //if (arrowString != "") returnString += $"({arrowString})";

                    returnString += "\n魔力が...満ちていく...!";
                }
            }
        }
        return returnString;
    }
    private static string GetArrows(byte seerId,byte[] targetId)
    {
        return Utils.ColorString(Color.white, TargetArrow.GetArrows(Utils.GetPlayerById(seerId), targetId));
    }

    class PlayerStopCS
    {
        public Vector3 position;
        public int waitTime;
    }
}
