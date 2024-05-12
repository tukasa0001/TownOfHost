using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AsmResolver.Collections;
using Hazel;
using TMPro;
using TownOfHostForE.Attributes;

namespace TownOfHostForE
{
    class BetWinTeams
    {
        private static readonly int Id = 252525;
        private static readonly string BET_SETTING__PATH = @"./TOH_DATA/UserPoint.csv";
        private static readonly string SHOP_SETTING__PATH = @"./TOH_DATA/BetPointShop.csv";

        private static Dictionary<string, string> BetTeamName = new();
        public static Dictionary<string, BetPointData> BetPoint = new();
        private static Dictionary<string, int> PlusBetPoint = new();
        private static Dictionary<string, ShopData> ShopDataDic = new();
        public static List<string> winnerCode = new();

        //private static readonly int maxData = 99999;

        public static OptionItem BetWinTeamMode;
        public static OptionItem VoteMaxPlayer;
        public static OptionItem DisableShogo;
        private static bool FirstInit = true;

        private static bool SetPointFlag = false;
        private static bool SetWinnnerTeamPointFlag = false;

        public static bool readedCSV = false;

        public class BetPointData
        {
            private string _playerFriendCode;
            private string _auName;
            private byte _playerUTId;
            private int _playerPoint;
            private bool _agree;
            private string _syougou;
            private KeepShogo _kShogo;

            public string PlayerFriendCode
            {
                set { _playerFriendCode = value; }
                get { return _playerFriendCode; }
            }
            public string AUName
            {
                set { _auName = value; }
                get { return _auName; }
            }
            public byte PlayerUTId
            {
                set { _playerUTId = value; }
                get { return _playerUTId; }
            }
            public int PlayerPoint
            {
                set { _playerPoint = value; }
                get { return _playerPoint; }
            }
            public bool Agree
            {
                set { _agree = value; }
                get { return _agree; }
            }
            public string Syougo
            {
                set { _syougou = value; }
                get { return _syougou; }
            }
            public KeepShogo KeepShogo
            {
                set { _kShogo = value; }
                get { return _kShogo; }
            }
        }
        public class KeepShogo
        {
            private string _syougou1;
            private string _syougou2;
            private string _syougou3;

            public string Syougo1
            {
                set { _syougou1 = value; }
                get { return _syougou1; }
            }
            public string Syougo2
            {
                set { _syougou2 = value; }
                get { return _syougou2; }
            }
            public string Syougo3
            {
                set { _syougou3 = value; }
                get { return _syougou3; }
            }
        }
        private class ShopData
        {
            private string _itemName;
            private int _price;
            private string _category;
            private string _setName;

            public string ItemName
            {
                set { _itemName = value; }
                get { return _itemName; }
            }
            public int Price
            {
                set { _price = value; }
                get { return _price; }
            }
            public string Category
            {
                set { _category = value; }
                get { return _category; }
            }
            public string SetName
            {
                set { _setName = value; }
                get { return _setName; }
            }
        }

        //0:クルー 1:インポスター 2:アニマルズ 他:現在の第3陣営数
        private static Dictionary<string, int> BetTeamCount = new();

        public static bool IsCamofuluge()
        {
            return (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool());
        }

        [PluginModuleInitializer]
        public static void Init()
        {
            try
            {
                if (FirstInit)
                {
                    FirstInit = false;
                    BetPoint.Clear();
                }
                winnerCode.Clear();
                BetTeamName.Clear();
                BetTeamCount.Clear();
                SetPointFlag = false;
                SetWinnnerTeamPointFlag = false;
                PlusBetPoint.Clear();
                ReadCsvToDictionaryForPlayerData();
                ReadCsvToDictionaryForShopData();
            }
            catch (Exception ex)
            {
                Logger.Info("ポイントの初期化に失敗しました。st:" + ex.Message + "/" + ex.StackTrace, "betwin");
            }
        }

        [GameModuleInitializer]
        public static void GameInit()
        {
            try
            {
                winnerCode.Clear();
                BetTeamName.Clear();
                BetTeamCount.Clear();
                SetPointFlag = false;
                SetWinnnerTeamPointFlag = false;
                PlusBetPoint.Clear();
                SendRPC();
            }
            catch (Exception ex)
            {
                Logger.Info("ポイントの初期化に失敗しました。st:" + ex.Message + "/" + ex.StackTrace, "betwin");
            }
        }

        public static void SendRPC()
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncBetwinShogo, Hazel.SendOption.Reliable, -1);

                //送信用
                Dictionary<string, BetPointData> sendData = new();

                foreach (var pc in Main.AllPlayerControls)
                {
                    if (BetPoint.ContainsKey(pc.FriendCode))
                    {
                        sendData.Add(pc.FriendCode, BetPoint[pc.FriendCode]);
                    }
                }

                writer.Write(sendData.Count());
                foreach (var data in sendData)
                {
                    string sendString = data.Key + "," + data.Value.Syougo;
                    writer.Write(sendString);
                }

                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        /// <summary>
        /// ロビーにJoinしてきたやつに称号を付ける
        /// ロビーのみ有効
        /// </summary>
        public static void JoinLobbySyougo()
        {
            return;
            //ロビー限定
            if (!GameStates.IsLobby) return;

            //称号付けれたらつけるよ
            if (BetWinTeamMode.GetBool() && !DisableShogo.GetBool())
            {
                _ = new LateTask(() =>
                {
                    foreach (var target in Main.AllPlayerControls)
                    {
                        //ホストは変更しない
                        if (target.PlayerId == 0) continue;
                        string targetName = target.name;

                        if (BetPoint.ContainsKey(target.FriendCode) &&
                        BetPoint[target.FriendCode].Syougo != null &&
                        BetPoint[target.FriendCode].Syougo != "")
                        {
                            string syogoData = BetPoint[target.FriendCode].Syougo;
                            if (!targetName.Contains(syogoData))
                            {
                                targetName += "\r\n" + BetPoint[target.FriendCode].Syougo;
                                target.RpcSetName(targetName);
                            }
                        }
                    }
                }, 1f, "LobbySetShogo");
            }
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                string[] readString = reader.ReadString().Split(",");
                if (!BetPoint.ContainsKey(readString[0]))
                {
                    BetPointData betPointData = new()
                    {
                        PlayerFriendCode = readString[0],
                        AUName = "",
                        PlayerUTId = 0,
                        PlayerPoint = 0,
                        Agree = true,
                        Syougo = readString[1]
                    };
                    BetPoint.Add(readString[0], betPointData);
                }
                else
                {
                    BetPoint[readString[0]].Syougo = readString[1];
                }
            }
        }

        public static void SetupCustomOption()
        {
            BetWinTeamMode = BooleanOptionItem.Create(Id, "BetWinTeamMode", false, TabGroup.MainSettings, false)
                .SetColor(Color.yellow)
                .SetGameMode(CustomGameMode.Standard);

            VoteMaxPlayer = IntegerOptionItem.Create(Id + 5, "VoteMaxPlayer", new(2, 15, 1), 6, TabGroup.MainSettings, false).SetParent(BetWinTeamMode)
                .SetValueFormat(OptionFormat.Players);

            DisableShogo = BooleanOptionItem.Create(Id + 10, "DisableShogo", false, TabGroup.MainSettings, false).SetParent(BetWinTeamMode);
        }
        public static bool BetOnReceiveChat(PlayerControl player, string text)
        {
            if (!BetWinTeamMode.GetBool()) return false;
            string[] args = text.Split(' ');

            if (args[0] == "/BetPoint" || args[0] == "/bp")
            {
                try
                {
                    if (BetPoint.ContainsKey(player.FriendCode))
                    {
                        Utils.SendMessage("君の持ち点は" + BetPoint[player.FriendCode].PlayerPoint + "点だ！", player.PlayerId, "");
                    }
                    else
                        Utils.SendMessage("どうやらまだ勝利陣営を予想していないようだね", player.PlayerId, "");


                    return true;
                }
                catch
                {
                    Utils.SendMessage("どうやらまだ勝利陣営を予想していないようだね", player.PlayerId, "");
                    return true;
                }

            }

            if (args[0] == "/AllBetPoint" || args[0] == "/albp")
            {
                Utils.SendMessage("皆の持ち点はこんな感じ", player.PlayerId, "");

                try
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        string ColorName = getPlayerName(pc.FriendCode);
                        if (BetPoint.ContainsKey(pc.FriendCode))
                        {
                            Utils.SendMessage(ColorName + "の持ち点は" + BetPoint[pc.FriendCode].PlayerPoint + "点だ！", player.PlayerId, "");
                        }
                        else
                        {
                            Utils.SendMessage(ColorName + "はまだポイントを取得していないようだ", player.PlayerId, "");
                        }
                    }
                    return true;
                }
                catch
                {
                    Utils.SendMessage("取得できんかった、すまん(´;ω;｀)", player.PlayerId, "");
                    return true;
                }
            }

            if (args[0] == "/PointShopList" || args[0] == "/psl")
            {
                Utils.SendMessage("ﾍｲﾗｯｼｬｲ", player.PlayerId, "");
                string Temptext = "今はこんな称号が選べるよ！\n";
                int NowCount = 1; //見た目的に0から1上がり

                foreach (var shopdata in ShopDataDic)
                {
                    ShopData tmpdata = new();
                    tmpdata = shopdata.Value;

                    if (tmpdata.Category != "称号") continue;

                    //御店のリストを作るよ
                    string tmpstring = NowCount + ":" + tmpdata.SetName + " | " + tmpdata.Price + "pt";
                    Temptext = Temptext + tmpstring + "\n";
                    NowCount++;
                }

                Utils.SendMessage(Temptext, player.PlayerId, "");
                return true;
            }

            if (args[0] == "/BuyShop" || args[0] == "/bs")
            {
                try
                {
                    string subArgs = args.Length < 2 ? "" : args[1];
                    Logger.Info("SubArgs" + subArgs, "BETBUY");

                    //先ガチャ処理
                    if (subArgs == "ガチャ")
                    {
                        if (BetPoint[player.FriendCode].PlayerPoint >= 100)
                        {
                            Utils.SendMessage("ｶﾞﾗｶﾞﾗｯﾎﾟﾝ!", player.PlayerId, "");
                            var tempName = GetGatchaShougou();
                            if (tempName == "")
                            {
                                Utils.SendMessage("どうやらガチャの箱が空っぽのようだ...", player.PlayerId, "");
                                return true;
                            }
                            BetPoint[player.FriendCode].Syougo = "<size=75%>" + tempName + "</size>";
                            BetPoint[player.FriendCode].PlayerPoint -= 100;
                            //ストック処理
                            SetKeepShogo(player.FriendCode, tempName);
                            WriteCSVPlayerData();

                            Utils.SendMessage("称号に" + tempName + "を付けたよ。\r\nﾏｲﾄﾞｱﾘ！", player.PlayerId, "");
                            JoinLobbySyougo();
                        }
                        else
                        {
                            Utils.SendMessage("ポイントが足りないねぇ！\r\nガチャは1回100ptだよ！\r\n出直して来な！", player.PlayerId, "");
                        }
                        return true;
                    }

                    //その後判定
                    if (ShopDataDic.ContainsKey(subArgs))
                    {
                        if (BetPoint[player.FriendCode].PlayerPoint >= ShopDataDic[subArgs].Price)
                        {
                            if (ShopDataDic[subArgs].Category != "称号") return true;
                            var tempName = ShopDataDic[subArgs].SetName;
                            //ReadCsvToDictionaryForPlayerData();
                            BetPoint[player.FriendCode].Syougo = "<size=75%>" + tempName + "</size>";
                            BetPoint[player.FriendCode].PlayerPoint -= ShopDataDic[subArgs].Price;
                            //ストック処理
                            SetKeepShogo(player.FriendCode, tempName);
                            WriteCSVPlayerData();

                            Utils.SendMessage("称号に" + tempName + "を付けたよ。\r\nﾏｲﾄﾞｱﾘ！", player.PlayerId, "");
                            JoinLobbySyougo();
                        }
                        else
                        {
                            Utils.SendMessage("ポイントが足りない！\r\n出直して来な!", player.PlayerId, "");
                        }
                        return true;
                    }
                    else
                    {
                        Utils.SendMessage("その称号は取り扱ってないね", player.PlayerId, "");
                    }
                }
                catch
                {
                    Utils.SendMessage("不正な値が入力されました。", player.PlayerId, "");
                }
            }

            if (args[0] == "/h" || args[0] == "/help")
            {
                try
                {
                    string subArgs = args.Length < 2 ? "" : args[1];
                    if (subArgs == "b" || subArgs == "bet")
                    {
                        string cmdList = "【勝利陣営予想投票機能専用コマンド一覧】\n";
                        cmdList = cmdList + "利用開始：「/b 承認」" + "\n";
                        cmdList = cmdList + "利用停止：「/b 非承認」" + "\n";
                        cmdList = cmdList + "持ち点確認：「/bp」" + "\n";
                        cmdList = cmdList + "全員の持ち点確認：「/albp」" + "\n";
                        cmdList = cmdList + "称号ショップ：「/psl」" + "\n";
                        cmdList = cmdList + "称号購入：「/bs {買う称号のテキスト}」" + "\n";
                        cmdList = cmdList + "称号ガチャ：「/bs ガチャ」" + "\n";
                        cmdList = cmdList + "所持称号確認：「/msl」" + "\n";
                        cmdList = cmdList + "今の称号を保存する：「/sms {保存先の番号}」" + "\n";
                        cmdList = cmdList + "所持している称号を適用する：「/ss {対象の番号}」" + "\n";
                        cmdList = cmdList + "---霊界のみ---" + "\n";
                        cmdList = cmdList + "投票：「/b {勝利予想陣営名称}」" + "\n";
                        cmdList = cmdList + "追加ベット投票「/b {勝利陣営名称} {追加するポイント数}」" + "\n";
                        Utils.SendMessage(cmdList, player.PlayerId, "");
                    }
                }
                catch
                {
                    Utils.SendMessage("不正な値が入力されました。", player.PlayerId, "");
                }

            }

            if (args[0] == "/SetShougo" || args[0] == "/ss")
            {
                try
                {
                    string subArgs = args.Length < 2 ? "" : args[1];
                    Logger.Info("SubArgs" + subArgs, "BETBUY");
                    int targetNum = Int32.Parse(subArgs);
                    if (!CheckKeepShogo(player.FriendCode, targetNum))
                    {
                        Utils.SendMessage("保存されている称号がありません。", player.PlayerId, "");
                        return true;
                    }

                    SetShogoToKeep(player.FriendCode, targetNum);
                    Utils.SendMessage("称号を適用しました！", player.PlayerId, "");
                    JoinLobbySyougo();
                }
                catch
                {
                    Utils.SendMessage("不正な値が入力されました。", player.PlayerId, "");
                }

            }

            if (args[0] == "/MyShougoList" || args[0] == "/msl")
            {
                SendMyShogouList(player.FriendCode, player.PlayerId);
                return true;
            }

            if (args[0] == "/SetMyShougo" || args[0] == "/sms")
            {
                try
                {
                    string subArgs = args.Length < 2 ? "" : args[1];
                    Logger.Info("SubArgs" + subArgs, "BETBUY");
                    int targetNum = Int32.Parse(subArgs);
                    //ストック処理
                    SetKeepShogo(player.FriendCode, BetPoint[player.FriendCode].Syougo, targetNum);
                    Utils.SendMessage("称号を保存しました！", player.PlayerId, "");
                }
                catch
                {
                    Utils.SendMessage("不正な値が入力されました。", player.PlayerId, "");
                }
                return true;
            }

            if (args[0] == "/resetbp")
            {
                if (!AmongUsClient.Instance.AmHost) return true;
                string subArgs = args.Length < 2 ? "" : args[1];
                Logger.Info("SubArgs" + subArgs, "BETWIN");
                if (subArgs == "write")
                {
                    WriteCSVPlayerData();
                    Utils.SendMessage("CSVを上書き", player.PlayerId, "");
                }
                else if (subArgs == "read")
                {
                    ReadCsvToDictionaryForPlayerData();
                    ReadCsvToDictionaryForShopData();
                    Utils.SendMessage("CSVを読み取り", player.PlayerId, "");
                }
                else if (subArgs == "init")
                {
                    FirstInit = true;
                    Init();
                    Utils.SendMessage("勝利陣営予想関連のデータを初期化", player.PlayerId, "");
                }
                return true;
            }
            if (args[0] == "/Bet" || args[0] == "/b")
            {
                if (Options.CurrentGameMode != CustomGameMode.Standard)
                {
                    Utils.SendMessage("この機能は通常のゲームだけで有効だよ", player.PlayerId, "");
                    return true;
                }

                string subArgs = args.Length < 2 ? "" : args[1];

                if (subArgs == "承認")
                {

                    if (player.FriendCode == null || player.FriendCode == "")
                    {
                        Utils.SendMessage("この機能はフレンドコードがないと遊べない。有効にしてからまた試してくれ", player.PlayerId, "");
                        return true;
                    }
                    if (BetPoint.ContainsKey(player.FriendCode))
                    {
                        if (BetPoint[player.FriendCode].Agree)
                        {
                            Utils.SendMessage("君のデータは保管済みさ。消して欲しければ「/b 非承認」と打ち込んでくれ", player.PlayerId, "");
                            return true;
                        }

                    }
                    //登録処理
                    BetPointData TmpData = new();
                    TmpData.PlayerPoint = 0;
                    TmpData.PlayerFriendCode = player.FriendCode;
                    TmpData.PlayerUTId = 0;
                    TmpData.AUName = RemoveCrLf(getPlayerName(player.FriendCode));
                    TmpData.Agree = true;
                    BetPoint[player.FriendCode] = TmpData;
                    Utils.SendMessage("登録を受け付けたよ、楽しんでくれたまえ", player.PlayerId, "");
                    return true;
                }
                else if (subArgs == "非承認")
                {
                    if (BetPoint.ContainsKey(player.FriendCode))
                    {
                        BetPoint.Remove(player.FriendCode);
                        WriteCSVPlayerData();
                    }
                    Utils.SendMessage("承知した。君のデータを削除するよ。悪かったね。", player.PlayerId, "");
                    return true;
                }


                //if (!GameStates.IsMeeting) return false;
                // 生き残ってる人が6人以下で締め切る
                if (Main.AllAlivePlayerControls.Count() == 2)
                {
                    //残り2人なら熱いので投票を許す。
                }
                else if (Main.AllAlivePlayerControls.Count() <= VoteMaxPlayer.GetInt())
                {
                    Utils.SendMessage("勝負がクライマックスのため投票を締め切りました。", player.PlayerId, "");
                    return true;
                }

                if (!GameStates.IsLobby && !player.IsAlive())
                {
                    //登録されていない = 初回利用者
                    if (!BetPoint.ContainsKey(player.FriendCode))
                    {
                        Utils.SendMessage("本機能は利用者のフレンドコードを取得するため、同意が必要です\r\n機能を利用する場合「/b 承認」と打ち込んで下さい。", player.PlayerId, "");
                        return true;
                    }
                    //登録されているけど同意してない人
                    else if (!BetPoint[player.FriendCode].Agree)
                    {
                        Utils.SendMessage("本機能は利用者のフレンドコードを取得するため、同意が必要です。\r\n機能を利用する場合「/b 承認」と打ち込んで下さい。", player.PlayerId, "");
                        return true;
                    }
                    //フレンドコードがない人
                    else if (player.FriendCode == null || player.FriendCode == "")
                    {
                        Utils.SendMessage("この機能はフレンドコードがないと遊べない。有効にしてからまた試してくれ", player.PlayerId, "");
                        return true;
                    }


                    switch (subArgs)
                    {
                        case "Crewmate":
                        case "Crew":
                        case "クルー":
                        case "クルーメイト":
                        case "Imposter":
                        case "インポスター":
                        case "Imp":
                        case "Animals":
                        case "Anim":
                        case "アニマルズ":
                        case "アニマル":
                        case "アーソニスト":
                        case "エゴイスト":
                        case "エクスキューショナー":
                        case "ジャッカル":
                        case "クライアント":
                        case "ジェスター":
                        case "ラバーズ":
                        case "純愛者":
                        case "姫":
                        case "オポチュニスト":
                        case "テロリスト":
                        case "アンチコンプリート":
                        case "ワーカホリック":
                        case "ダークハイド":
                        case "ラブカッター":
                        case "弁護士":
                        case "追跡者":
                        case "トトカルチョ":
                        case "義賊":
                        case "決闘者":
                        case "ヤンデレ":
                        case "ペスト医師":
                        case "マグロ":
                        case "運営者":
                            if (BetTeamName.ContainsKey(player.FriendCode))
                                BetTeamName[player.FriendCode] = subArgs;
                            else
                                BetTeamName.TryAdd(player.FriendCode, subArgs);

                            subArgs = args.Length < 3 ? "" : args[2];
                            int plusbet = 0;
                            if (subArgs == null || subArgs == "")
                            {
                                string TrueWords = "投票しました！　結果をお楽しみに！";
                                Utils.SendMessage(TrueWords, player.PlayerId, "");
                                break;
                            }

                            try
                            {
                                if (int.TryParse(subArgs, out plusbet))
                                {
                                    if (plusbet == 0)
                                    {
                                        Utils.SendMessage("ベットポイントは1以上にしてください。\r\n通常の投票になりました。", player.PlayerId, "");
                                        break;
                                    }
                                }
                                else
                                {
                                    Utils.SendMessage("追加ベットポイントの値が不正です。\r\n通常の投票になりました。", player.PlayerId, "");
                                    break;
                                }
                            }
                            catch
                            {
                                Utils.SendMessage("追加ベットポイントの値が不正です。\r\n通常の投票になりました。", player.PlayerId, "");
                                break;
                            }

                            if (plusbet <= BetPoint[player.FriendCode].PlayerPoint)
                            {
                                if (PlusBetPoint.ContainsKey(player.FriendCode))
                                {
                                    PlusBetPoint[player.FriendCode] = Int32.Parse(subArgs);
                                }
                                else
                                {
                                    PlusBetPoint.TryAdd(player.FriendCode, Int32.Parse(subArgs));
                                }

                                Utils.SendMessage("追加ベットを承りました！　君に幸あれ！", player.PlayerId, "");
                            }
                            else
                            {
                                Utils.SendMessage("ベットポイントが足りません。", player.PlayerId, "");
                            }
                            break;
                        default:
                            string FalseWords = "投票が出来てません。\r\n正しい役職名で投票してください。";
                            Utils.SendMessage(FalseWords, player.PlayerId, "");
                            break;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 称号をストックする奴
        /// 飽きがなければストックしない
        /// </summary>
        public static void SetKeepShogo(string targetFriendCode, string shogo, int targetNum = 0)
        {
            if (!BetPoint.ContainsKey(targetFriendCode)) return;
            KeepShogo data = BetPoint[targetFriendCode].KeepShogo;

            if (targetNum == 0)
            {
                //指定なしであれば空き枠を埋める
                if (data.Syougo1 == "")
                {
                    data.Syougo1 = shogo;
                }
                else if (data.Syougo2 == "")
                {
                    data.Syougo2 = shogo;
                }
                else if (data.Syougo3 == "")
                {
                    data.Syougo3 = shogo;
                }
            }
            //直接指定は上書き
            else if (targetNum == 1)
            {
                data.Syougo1 = shogo;
            }
            else if (targetNum == 2)
            {
                data.Syougo2 = shogo;
            }
            else if (targetNum == 3)
            {
                data.Syougo3 = shogo;
            }
        }
        /// <summary>
        /// 対象の称号が空いているか確認するやつ
        /// </summary>
        public static bool CheckKeepShogo(string targetFriendCode, int targetNum)
        {
            if (!BetPoint.ContainsKey(targetFriendCode)) return false;
            KeepShogo data = BetPoint[targetFriendCode].KeepShogo;
            //直接指定は上書き
            if (targetNum == 1)
            {
                return data.Syougo1 != "";
            }
            else if (targetNum == 2)
            {
                return data.Syougo2 != "";
            }
            else if (targetNum == 3)
            {
                return data.Syougo3 != "";
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 称号を適用する奴
        /// </summary>
        public static void SetShogoToKeep(string targetFriendCode, int targetNum)
        {
            if (!BetPoint.ContainsKey(targetFriendCode)) return;
            BetPointData data = BetPoint[targetFriendCode];
            //直接指定は上書き
            if (targetNum == 1)
            {
                data.Syougo = data.KeepShogo.Syougo1;
            }
            else if (targetNum == 2)
            {
                data.Syougo = data.KeepShogo.Syougo2;
            }
            else if (targetNum == 3)
            {
                data.Syougo = data.KeepShogo.Syougo3;
            }
        }

        public static void SendMyShogouList(string targetFriendCode, byte targetId)
        {
            if (!BetPoint.ContainsKey(targetFriendCode))
            {
                Utils.SendMessage("登録してから試してね", targetId, "");
                return;
            }

            KeepShogo data = BetPoint[targetFriendCode].KeepShogo;

            string text1 = data.Syougo1 != "" ? data.Syougo1 : "未登録です";
            string text2 = data.Syougo2 != "" ? data.Syougo2 : "未登録です";
            string text3 = data.Syougo3 != "" ? data.Syougo3 : "未登録です";

            string Temptext = "君の手持ち称号はこんな感じ！\n";

            Temptext = Temptext + "1:" + text1 + "\n";
            Temptext = Temptext + "2:" + text2 + "\n";
            Temptext = Temptext + "3:" + text3;

            Utils.SendMessage(Temptext, targetId, "");
        }

        public static string RemoveCrLf(string input)
        {
            var tempinput = input;
            tempinput.Replace("\r", "");
            tempinput.Replace("\n", "");

            return tempinput;
        }
        private static string getPlayerName(string FriendCode)
        {
            Logger.Warn("名前取得：" + Main.AllPlayerControls.Count(), "BETWIN");
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.FriendCode == FriendCode)
                {
                    return pc?.Data?.PlayerName;
                }
            }
            Logger.Warn("そんな名前の奴などいない。：" + FriendCode, "BETWIN");
            return "豆腐国の王：鏑木・ﾃﾞｨﾎﾞｳｽｷｨ";
        }

        private static void SetBetTeamCount()
        {
            try
            {
                foreach (var betTeamName in BetTeamName)
                {
                    if (betTeamName.Value == null) continue;
                    string TeamName = betTeamName.Value;
                    Logger.Info("投票名：" + TeamName, "betwin");
                    switch (TeamName)
                    {
                        case "Crewmate":
                        case "Crew":
                        case "クルー":
                        case "クルーメイト":
                            if (BetTeamCount.ContainsKey("クルーメイト"))
                            {
                                BetTeamCount["クルーメイト"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("クルーメイト", 1);
                            }
                            break;
                        case "Imposter":
                        case "インポスター":
                        case "Imp":
                            if (BetTeamCount.ContainsKey("インポスター"))
                            {
                                BetTeamCount["インポスター"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("インポスター", 1);
                            }
                            break;
                        case "Animals":
                        case "Animal":
                        case "Anim":
                        case "アニマルズ":
                        case "アニマル":
                            if (BetTeamCount.ContainsKey("アニマルズ"))
                            {
                                BetTeamCount["アニマルズ"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("アニマルズ", 1);
                            }
                            break;
                        case "アーソニスト":
                        case "エゴイスト":
                        case "エクスキューショナー":
                        case "ジェスター":
                        case "ラバーズ":
                        case "オポチュニスト":
                        case "テロリスト":
                        case "アンチコンプリート":
                        case "ワーカホリック":
                        case "ダークハイド":
                        case "ラブカッター":
                        case "弁護士":
                        case "トトカルチョ":
                        case "義賊":
                        case "決闘者":
                        case "ヤンデレ":
                        case "ジャッカル":
                        case "ペスト医師":
                        case "マグロ":
                        case "運営者":
                            if (BetTeamCount.ContainsKey(TeamName))
                            {
                                BetTeamCount[TeamName]++;
                            }
                            else
                            {
                                BetTeamCount.Add(TeamName, 1);
                            }
                            break;
                        case "クライアント":
                            if (BetTeamCount.ContainsKey("ジャッカル"))
                            {
                                BetTeamCount["ジャッカル"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("ジャッカル", 1);
                            }
                            break;
                        case "純愛者":
                        case "姫":
                            if (BetTeamCount.ContainsKey("ラバーズ"))
                            {
                                BetTeamCount["ラバーズ"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("ラバーズ", 1);
                            }
                            break;
                        case "オポチュニストキラー":
                            if (BetTeamCount.ContainsKey("オポチュニスト"))
                            {
                                BetTeamCount["オポチュニスト"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("オポチュニスト", 1);
                            }
                            break;
                        case "追跡者":
                            if (BetTeamCount.ContainsKey("弁護士"))
                            {
                                BetTeamCount["弁護士"]++;
                            }
                            else
                            {
                                BetTeamCount.Add("弁護士", 1);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("投票先集計でで例外が発生。" + ex.Message + "/ST:" + ex.StackTrace, "BETWIN");
            }

        }
        public static string GetManyBetTeam()
        {
            try
            {
                Logger.Info("勝利陣営取得処理開始", "GetManyBetTeam");
                SetBetTeamCount();

                Logger.Info("最大投票チーム取得", "GetManyBetTeam");
                List<string> manyBetTeamName = GetManyBetTeam(BetTeamCount);
                if (manyBetTeamName == null) return "";
                string ResultTeamName = "";
                string WinnerBetText = "";
                int IndexCounter = 0;

                //勝利チームの加点処理
                if (!SetWinnnerTeamPointFlag)
                {
                    Logger.Info("勝利チーム加点処理開始", "GetManyBetTeam");
                    SetPointForWinner();
                    SetWinnnerTeamPointFlag = true;
                }

                //最大投票チームがあるなら
                if (manyBetTeamName != null && manyBetTeamName.Count() > 0)
                {
                    foreach (var teamName in manyBetTeamName)
                    {
                        ResultTeamName += teamName;
                        IndexCounter++;
                        if (IndexCounter < manyBetTeamName.Count)
                        {
                            ResultTeamName += ":";
                        }
                    }

                    Logger.Info("投票数が多いチーム：" + ResultTeamName, "GetManyBetTeam");
                    WinnerBetText = "\n一番投票が多かったのは：" + ResultTeamName + "\nおめでとう！";

                    //実際の勝利チームからの加算処理
                    if (!SetPointFlag)
                    {
                        //lastwinstextから複数あった場合、取り出さないといけない
                        List<string> winnersText = new();
                        string Nowtext = SetEverythingUpPatch.LastWinsText;
                        int TextIndex = 0;
                        bool WinnerCheckFlag = true;
                        do
                        {
                            //LastWinstextから勝利陣営名称を切り抜く作業
                            winnersText.Add(IsStringContained(Nowtext));

                            //勝利陣営名称が取得できた時
                            if (winnersText[TextIndex] != "オワタニエン")
                            {
                                //NowTextから勝利陣営名称を取り除いてNowtextに入れる(文字列から勝利陣営名称を失くす)
                                Nowtext = RemoveKeyword(Nowtext, winnersText[TextIndex]);
                                TextIndex++;
                            }
                            else //勝利陣営取得が終わったとき
                            {
                                WinnerCheckFlag = false;
                            }

                        } while (WinnerCheckFlag);

                        //予想の減点と加点
                        foreach (var WinTeam in winnersText)
                        {
                            SetPoint(WinTeam, manyBetTeamName);
                        }

                        SetPointFlag = true;
                        //WriteCSVPlayerData();
                    }
                }
                else //誰も投票してない時
                {
                    //何もしなくていいや
                }

                return WinnerBetText;
            }
            catch (Exception ex)
            {
                Logger.Info("不明なエラーが発生しました。" + ex.Message + "/ST:" + ex.StackTrace, "BETWIN");
                return "集計に異常が発生したので見せられないよ！";
            }
        }
        public static string RemoveKeyword(string input, string keyword)
        {
            input = input.Replace(keyword, string.Empty);

            return input.Trim();
        }

        public static List<string> GetManyBetTeam(Dictionary<string, int> betData)
        {
            try
            {
                if (betData == null)
                {
                    Logger.Fatal("誰も予想をしなかった", "GetMaxVariableIndices");
                }

                int bestPoints = 0;
                List<string> ManyBetTeamName = new();
                //最大値チェックループ
                foreach (var point in betData.Values)
                {
                    if (point > bestPoints) bestPoints = point;
                }
                //最大値を持ってる奴確認ループ
                foreach (var best in betData)
                {
                    //最大で賭けられたチームなら
                    if (best.Value == bestPoints)
                        ManyBetTeamName.Add(best.Key);

                }
                return ManyBetTeamName;
            }
            catch (Exception ex)
            {
                Logger.Info("最多投票数取得で例外発生" + ex.Message + "/ST:" + ex.StackTrace, "BETWIN");
                return null;
            }
        }
        private static string IsStringContained(string GameWinnerTeamName)
        {

            switch (GameWinnerTeamName)
            {
                case string s when s.Contains("Crewmate") ||
                                   s.Contains("Crew") ||
                                   s.Contains("クルー") ||
                                   s.Contains("クルーメイト"):
                    return "クルーメイト";
                case string s when s.Contains("インポスター") ||
                                   s.Contains("Imposter") ||
                                   s.Contains("Imp"):
                    return "インポスター";
                case string s when s.Contains("Animals") ||
                                   s.Contains("Animal") ||
                                   s.Contains("Anim") ||
                                   s.Contains("アニマルズ") ||
                                   s.Contains("アニマル"):
                    return "アニマルズ";
                case string s when s.Contains("アーソニスト"):
                    return "アーソニスト";
                case string s when s.Contains("エゴイスト"):
                    return "エゴイスト";
                case string s when s.Contains("エクスキューショナー"):
                    return "エクスキューショナー";
                case string s when s.Contains("ジャッカル") ||
                                   s.Contains("クライアント"):
                    return "ジャッカル";
                case string s when s.Contains("ジェスター"):
                    return "ジェスター";
                case string s when s.Contains("ラバーズ") ||
                                   s.Contains("純愛者") ||
                                   s.Contains("姫"):
                    return "ラバーズ";
                case string s when s.Contains("オポチュニスト") ||
                                   s.Contains("オポチュニストキラー"):
                    return "オポチュニスト";
                case string s when s.Contains("テロリスト"):
                    return "テロリスト";
                case string s when s.Contains("アンチコンプリート"):
                    return "アンチコンプリート";
                case string s when s.Contains("ワーカホリック"):
                    return "ワーカホリック";
                case string s when s.Contains("ダークハイド"):
                    return "ダークハイド";
                case string s when s.Contains("弁護士") ||
                                   s.Contains("追跡者"):
                    return "弁護士";
                case string s when s.Contains("トトカルチョ"):
                    return "トトカルチョ";
                case string s when s.Contains("義賊"):
                    return "義賊";
                case string s when s.Contains("決闘者"):
                    return "決闘者";
                case string s when s.Contains("ヤンデレ"):
                    return "ヤンデレ";
                default:
                    return "オワタニエン";
            }
        }

        private static void SetPoint(string WinnerTeam, List<string> ManyBetTeamNames)
        {
            //ポイント付与ループ
            foreach (var SetPlayer in BetTeamName)
            {
                Logger.Info("確認:" + SetPlayer.Key, "BETWIN");
                string PlayerValueBefore = IsStringContained(SetPlayer.Value);

                //予想陣営と勝利が一致してない人は対象外
                if (PlayerValueBefore != WinnerTeam) continue;

                Logger.Info("一致:" + SetPlayer.Key, "BETWIN");
                int PlusValue = 1;
                bool PlusFlag = true;
                foreach (var ManyBetTeamName in ManyBetTeamNames)
                {
                    //一つでも一致したら3点にはならない。
                    if (PlayerValueBefore == ManyBetTeamName)
                    {
                        PlusFlag = false;
                    }
                }
                if (PlusFlag)
                {
                    Logger.Info("ポイントアップ:" + SetPlayer.Key, "BETWIN");
                    PlusValue = 3;
                }

                //賭けポイントを払ってたらその分倍に
                if (PlusBetPoint.ContainsKey(SetPlayer.Key))
                {
                    Logger.Info("更にポイントアップ:" + SetPlayer.Key, "BETWIN");
                    PlusValue = PlusValue * PlusBetPoint[SetPlayer.Key];
                    PlusBetPoint.Remove(SetPlayer.Key);
                }

                //上限を超えていたら上限で調整する
                //if (PlusValue > maxData) PlusValue = maxData;

                Logger.Info("ポイント追加:" + SetPlayer.Key, "BETWIN");
                if (BetPoint.ContainsKey(SetPlayer.Key))
                {
                    BetPoint[SetPlayer.Key].AUName = RemoveCrLf(getPlayerName(SetPlayer.Key));
                    BetPoint[SetPlayer.Key].PlayerPoint += PlusValue;
                }
                else
                {
                    BetPointData TmpData = new();
                    TmpData.PlayerPoint = PlusValue;
                    TmpData.AUName = RemoveCrLf(getPlayerName(SetPlayer.Key));
                    TmpData.PlayerFriendCode = SetPlayer.Key;
                    BetPoint.TryAdd(SetPlayer.Key, TmpData);
                }
                Logger.Info(SetPlayer.Key + "に" + PlusValue + "ポイントを付与しました。", "BETWIN");
                //Utils.SendMessage("予想したチームが勝利したためポイントが付与されました！\r\nおめでとう！！", getFirendCodeToId(SetPlayer.Key), "");
            }

            //追加ポイント払って外した奴を虐めるループ
            try
            {
                //ベットされていて、当ててない人がいる場合
                if (PlusBetPoint.Count() > 0)
                {
                    foreach (var removePoint in PlusBetPoint)
                    {
                        Logger.Info(getPlayerName(removePoint.Key) + "の" + BetPoint[removePoint.Key].PlayerPoint + "から" + removePoint.Value + "ポイントをはく奪しました。", "BETWIN");
                        BetPoint[removePoint.Key].PlayerPoint -= removePoint.Value;
                        PlusBetPoint.Remove(removePoint.Key);
                        //Utils.SendMessage("どうやら予想を外してしまったようだね。\r\n賭けたポイントは貰っていくよ。", getFirendCodeToId(removePoint.Key), "");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Info("ポイントはく奪で例外が発生。" + e.Message + "/ST:" + e.StackTrace, "BETWIN");
            }
        }
        public static byte getFirendCodeToId(string FriendCode)
        {
            byte id = 0;

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.FriendCode == FriendCode)
                {
                    id = pc.PlayerId;
                    break;
                }
            }

            return id;
        }

        private static void SetPointForWinner()
        {
            Logger.Info("勝利した人数：" + winnerCode.Count(), "BETWIN");
            //勝者がポイント取得有効だった場合にポイント付与
            foreach (var friendCode in winnerCode)
            {
                //フレコがない人は処理しない
                if (friendCode == null || friendCode == "") continue;
                //まだ同意していないなら終了
                if (!BetPoint.ContainsKey(friendCode)) continue;
                if (!BetPoint[friendCode].Agree) continue;
                //勝者には3点付与
                BetPoint[friendCode].PlayerPoint += 3;
                Logger.Info("勝利した人にポイント付与：" + friendCode, "BETWIN");
                //Utils.SendMessage("ゲームに勝利したため特別点3点が付与されました！", getFirendCodeToId(friendCode), "");
            }
        }
        public static void WriteCSVPlayerData()
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost) return;
                //出力
                using (StreamWriter writer = new(BET_SETTING__PATH))
                {
                    // データ行の出力
                    foreach (var kvp in BetPoint)
                    {
                        var data = kvp.Value;
                        string line = $"{data.PlayerFriendCode},{RemoveCrLf(data.AUName)},{data.PlayerUTId},{data.PlayerPoint},{data.Agree},{data.Syougo},{data.KeepShogo.Syougo1},{data.KeepShogo.Syougo2},{data.KeepShogo.Syougo3}";
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("CSV書き込み異常：" + ex.Message + ":" + ex.StackTrace, "WRITECSV");
            }
        }

        private static void ReadCsvToDictionaryForPlayerData()
        {
            try
            {
                Logger.Msg("ポイントデータ読み取り開始", "BETREAD");
                using (StreamReader reader = new(BET_SETTING__PATH))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = line.Split(',');

                        //Logger.Msg("ポイントデータを読み取りたい:" + values.Length, "BETREAD");
                        if (values.Length >= 6)
                        {
                            string playerFriendCode = values[0];
                            string AuName = values[1];
                            byte playerUTId;
                            //YTIDのnullcheck
                            if (string.IsNullOrEmpty(values[2]))
                            {
                                playerUTId = 0;
                            }
                            else
                            {
                                playerUTId = Convert.ToByte(values[2]);
                            }
                            int playerPoint = Convert.ToInt32(values[3]);
                            bool agree = Convert.ToBoolean(values[4]);
                            string syougou = values[5];
                            KeepShogo tempKeep = new KeepShogo();
                            if (values.Length > 6)
                            {
                                tempKeep.Syougo1 = values[6];
                                tempKeep.Syougo2 = values[7];
                                tempKeep.Syougo3 = values[8];
                            }
                            else
                            {
                                tempKeep.Syougo1 = "";
                                tempKeep.Syougo2 = "";
                                tempKeep.Syougo3 = "";
                            }

                            BetPointData betPointData = new()
                            {
                                PlayerFriendCode = playerFriendCode,
                                AUName = AuName,
                                PlayerUTId = playerUTId,
                                PlayerPoint = playerPoint,
                                Agree = agree,
                                Syougo = syougou,
                                KeepShogo = tempKeep
                            };

                            BetPoint[playerFriendCode] = betPointData;
                            //Logger.Msg("ポイントデータを読み取りました:" + playerFriendCode+ ":" + AuName,"BETREAD");
                        }
                    }
                }
                readedCSV = true;
                Logger.Msg("ポイントデータ読み取り終了", "BETREAD");
            }
            catch (Exception e)
            {
                Logger.Msg("ポイントデータ読み取り例外：" + e.Message, "BETREAD");
            }
        }

        private static void ReadCsvToDictionaryForShopData()
        {
            try
            {
                //if (!AmongUsClient.Instance.AmHost) return;
                Logger.Msg("ショップデータ読み取り開始", "BETREAD");
                using (StreamReader reader = new(SHOP_SETTING__PATH))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = line.Split(',');

                        if (values.Length == 4)
                        {
                            string itemName = values[0];
                            int price = Convert.ToInt32(values[1]);
                            string category = values[2];
                            string setName = values[3];

                            ShopData shopData = new()
                            {
                                ItemName = itemName,
                                Price = price,
                                Category = category,
                                SetName = setName
                            };
                            ShopDataDic[itemName] = shopData;
                        }
                    }
                }
                Logger.Msg("ショップデータ読み取り終了", "BETREAD");
            }
            catch (Exception e)
            {
                Logger.Msg("ショップデータ読み取り例外：" + e.Message, "BETREAD");
            }
        }

        public static void setSubNameInGame()
        {
            if (!BetWinTeamMode.GetBool()) return;

        }

        public static bool setSubNameFlag(string friendCode)
        {
            if (!BetWinTeamMode.GetBool()) return false;
            if (!BetPoint.ContainsKey(friendCode)) return false;
            if (BetPoint[friendCode].Syougo == null) return false;
            return true;
        }

        //ガチャ
        private static string GetGatchaShougou()
        {
            List<string> leftList = new();
            List<string> middleList = new();
            List<string> rightList = new();
            List<string> colorList = new();
            //準備
            foreach (var shopdata in ShopDataDic)
            {
                ShopData tmpdata = new();
                tmpdata = shopdata.Value;
                if (tmpdata.Category == "称号L")
                {
                    leftList.Add(tmpdata.SetName);
                }
                else if (tmpdata.Category == "称号M")
                {
                    middleList.Add(tmpdata.SetName);
                }
                else if (tmpdata.Category == "称号R")
                {
                    rightList.Add(tmpdata.SetName);
                }
                else if (tmpdata.Category == "称号C")
                {
                    colorList.Add(tmpdata.SetName);
                }
            }

            //一つでもリストが空なら処理しない
            if (leftList.Count == 0 || middleList.Count == 0 || rightList.Count == 0 || colorList.Count == 0) return "";

            string leftWord = GetGachaWords(leftList);
            string middleWord = GetGachaWords(middleList);
            string rightWord = GetGachaWords(rightList);
            string colorCode = GetGachaWords(colorList);

            string createShogo = $"【{leftWord}{middleWord}{rightWord}】";
            Color color = new Color();
            if (ColorUtility.TryParseHtmlString(colorCode, out color))// outキーワードで参照渡しにする
            {
                createShogo = Utils.ColorString(color, createShogo);
            }

            return createShogo;
        }

        private static string GetGachaWords(List<string> targetList)
        {
            System.Random rand = new();
            return targetList[rand.Next(targetList.Count)];
        }
    }
}
