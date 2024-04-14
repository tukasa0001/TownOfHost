using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TownOfHostForE.Attributes;
using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using UnityEngine;
using YoutubeLiveChatSharp;
using static Il2CppSystem.Globalization.CultureInfo;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE
{
    class ImposterChat
    {
        private enum nowState
        {
            //誰が or 何処
            SetWhoWhere,
            //具体的にどこか、誰か
            SetTarget,
            //何をした
            SetWhat,
            //終わり 使わないなら消す
            End
        }
        private enum WhoWhere
        {
            //誰
            Who,
            //何処
            Where
        }
        private class chatData
        {
            private nowState _state;
            private int _nowPage;
            private WhoWhere _target;
            private string _sendData;

            public int NowPage
            {
                set { _nowPage = value; }
                get { return _nowPage; }
            }
            public nowState NowState
            {
                set { _state = value; }
                get { return _state; }
            }
            public WhoWhere Target
            {
                set { _target = value; }
                get { return _target; }
            }
            public string SendData
            {
                set { _sendData = value; }
                get { return _sendData; }
            }
        }

        private static HashSet<byte> imposterIds = new();
        private static Dictionary<byte, chatData> ChatDatas = new();
        private static Dictionary<byte, List<List<string>>> PageDatas = new();

        public static OptionItem OptionImposterChat;

        readonly static string NEXT_PAGES = "次ページへ";

        [GameModuleInitializer]
        public static void InitData()
        {
            ChatDatas = new();
            PageDatas = new();
            imposterIds = new();
        }

        public static void SetupCustomOption()
        {
            OptionImposterChat = BooleanOptionItem.Create(4_252526, "OptionImposterChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard);
        }
        public static void Add()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.GetCustomRole().GetCustomRoleTypes() == CustomRoleTypes.Impostor)
                {
                    imposterIds.Add(pc.PlayerId);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetRole"></param>
        /// <returns>true:チャット対象</returns>
        public static bool CheckImposterChat(CustomRoles targetRole)
        {
            return OptionImposterChat.GetBool() && targetRole.GetCustomRoleTypes() == CustomRoleTypes.Impostor;
        }

        /// <summary>
        /// インポスターチャット処理
        /// </summary>
        /// <param name="voter"></param>
        /// <param name="voted"></param>
        /// <returns>true:投票を実行 / false:投票キャンセル</returns>
        public static bool ImposterChats(PlayerControl voter,PlayerControl voted)
        {
            //インポスターチャットが無効なら使わない。
            if (OptionImposterChat.GetBool() == false) return true;

            //インポスター以外なら関係なし
            if(voter.GetCustomRole().GetCustomRoleTypes() != CustomRoleTypes.Impostor) return true;

            //生存人数が2人以下(人外生存のみ)の場合は関係なし
            if(Main.AllAlivePlayerControls.Count() <= 2) return true;

            //skip投票
            if(voted == null)
            {
                //チャット登録済みなら
                if (ChatDatas.ContainsKey(voter.PlayerId))
                {
                    //初回以外は解除させる為のフラグ
                    bool returnBool = ChatDatas[voter.PlayerId].NowState == nowState.SetWhoWhere;

                    //チャットデータを削除
                    ChatDatas.Remove(voter.PlayerId);
                    //初回じゃないとき
                    if(returnBool == false)
                    {
                        SendImposterChat(voter.PlayerId,"キャンセルしました。");
                    }

                    return returnBool;
                }
                else
                {
                    //初期化
                    chatData tempChatData = new()
                    {
                        NowState = nowState.SetWhoWhere,
                        NowPage = 0,
                        SendData = ""
                    };
                    //登録
                    ChatDatas.Add(voter.PlayerId,tempChatData);
                    //相手or場所選択指示
                    CreateWhoWhereWords(voter.PlayerId);
                }
                //どちらも一度状態解除

                return false;
            }
            //skip以外だけど、インポスターチャット状態じゃない奴
            else if (!ChatDatas.ContainsKey(voter.PlayerId))
            {
                return true;
            }
            //インポスターチャット状態
            else
            {
                //プレイヤー選択
                switch (ChatDatas[voter.PlayerId].NowState)
                {
                    case nowState.SetWhoWhere:
                        SetWhoWhere(voter.PlayerId,voted.PlayerId);
                        break;
                    case nowState.SetTarget:
                        SetTarget(voter.PlayerId, voted.PlayerId);
                        break;
                    case nowState.SetWhat:
                        SetWhat(voter.PlayerId, voted.PlayerId);
                        break;
                    case nowState.End:
                    default:
                        break;


                }

                return false;
            }

        }

        /// <summary>
        /// 送りたい内容が相手か場所か決める
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        private static void SetWhoWhere(byte voterId,byte votedId)
        {

            byte picNum = GetPicPlayerNumber(voterId, votedId);

            switch (picNum)
            {
                //相手
                case 0:
                    ChatDatas[voterId].NowState = nowState.SetTarget;
                    ChatDatas[voterId].Target = WhoWhere.Who;
                    CreateTargetWordsForPlayer(voterId);
                    break;
                //場所
                case 1:
                    ChatDatas[voterId].NowState = nowState.SetTarget;
                    ChatDatas[voterId].Target = WhoWhere.Where;
                    CreateTargetWordsForPlace(voterId);
                    break;
                default:
                    break;
            }


        }

        /// <summary>
        /// 投票先の番号取得(IDじゃないよ)
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        /// <returns>投票先の番号</returns>
        private static byte GetPicPlayerNumber(byte voterId, byte votedId)
        {
            byte counter = 0;

            var targetList = Main.AllAlivePlayerControls.Where(x => !x.Data.Disconnected).OrderBy(x => x.PlayerId);

            foreach (var pc in targetList)
            {
                //idが0から撮られるか確認用
                Logger.Info("Ids:" + pc.PlayerId, "impChat");

                if (pc.PlayerId == votedId)
                {
                    return counter;
                }

                //インクリメント
                counter++;
            }

            //決まらなかったら(基本ないけど)bytemax
            return byte.MaxValue;
        }
        /// <summary>
        /// 場所の名前 or プレイヤーを選択する
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        private static void SetTarget(byte voterId, byte votedId)
        {
            //相手
            if (ChatDatas[voterId].Target == WhoWhere.Who)
            {
                SetTargetsForPlayer(voterId,votedId);
            }
            //場所
            else
            {
                byte picNum = GetPicPlayerNumber(voterId, votedId);
                SetTargetsForPlace(voterId, picNum);

            }
        }

        /// <summary>
        /// 場所の名前 or プレイヤーを選択する
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        private static void SetWhat(byte voterId, byte votedId)
        {
            byte picNum = GetPicPlayerNumber(voterId, votedId);
            int nowPage = ChatDatas[voterId].NowPage;

            //次ページ対象なら次へ
            if (CheckPages(voterId, picNum) == false)
            {
                switch (ChatDatas[voterId].Target)
                {
                    case WhoWhere.Who:
                        CreateWhatWordsForPlayer(voterId, ChatDatas[voterId].SendData, false);
                        break;
                    case WhoWhere.Where:
                        CreateWhatWordsForPlace(voterId, ChatDatas[voterId].SendData, false);
                        break;
                }
                return;
            }

            string sendString = PageDatas[voterId][nowPage][picNum];
            var info = Utils.GetPlayerInfoById(voterId);
            foreach (var impId in imposterIds)
            {
                if (impId == voterId)
                {
                    SendImposterChat(impId,$"「{sendString}」を送信しました。");
                }
                else
                {
                    SendImposterChat(impId, sendString, Utils.ColorString(info.Color, $"【†{info.PlayerName}†】"));
                }
            }
            ChatDatas.Remove(voterId);
            PageDatas.Remove(voterId);
        }

        /// <summary>
        /// プレイヤーを選択する
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        private static void SetTargetsForPlayer(byte voterId, byte votedId)
        {
            //消して色が付いた名前ではなく、色名
            string colorName = Utils.GetPlayerInfoById(votedId).ColorName;
            ChatDatas[voterId].SendData =  $"{colorName}";
            ChatDatas[voterId].NowState = nowState.SetWhat;
            CreateWhatWordsForPlayer(voterId, colorName);
        }

        /// <summary>
        /// 場所を選択する
        /// </summary>
        /// <param name="voterId"></param>
        /// <param name="votedId"></param>
        private static void SetTargetsForPlace(byte voterId, byte picNum)
        {

            //次ページ対象なら次へ
            if (CheckPages(voterId, picNum) == false)
            {
                CreateTargetWordsForPlace(voterId,false);
                return;
            }

            //このページで選ばれた場合
            //今のページ
            int nowPage = ChatDatas[voterId].NowPage;
            //選ばれたやつ
            string roomName = PageDatas[voterId][nowPage][picNum];
            ChatDatas[voterId].SendData =  $"{roomName}";
            ChatDatas[voterId].NowState = nowState.SetWhat;
            CreateWhatWordsForPlace(voterId, roomName);
        }

        private static bool CheckPages(byte voterId, byte picNum)
        {
            //今のページ
            int nowPage = ChatDatas[voterId].NowPage;

            if (PageDatas[voterId].Count() <= nowPage)
            {
                return false;
            }
            if (PageDatas[voterId][nowPage].Count() <= picNum)
            {
                Logger.Info($"指定異常:{PageDatas[voterId][nowPage].Count()}/{picNum}", "debug");
                return false;
            }
            //選ばれたやつ
            string targetString = PageDatas[voterId][nowPage][picNum];

            //次ページ対象なら
            if (targetString.Contains(NEXT_PAGES))
            {
                nowPage++;
                if (nowPage == PageDatas[voterId].Count)
                {
                    nowPage = 0;
                }

                ChatDatas[voterId].NowPage = nowPage;

                return false;
            }

            return true;
        }

        private static List<string> GetPlaceList()
        {
            List<string> returnData = new ();

            foreach (var room in ShipStatus.Instance.AllRooms)
            {
                returnData.Add($"{DestroyableSingleton<TranslationController>.Instance.GetString(room.RoomId)}");
            }
            return returnData;
        }

        /// <summary>
        /// 送信主にのみ次が場所か相手か決めるように促す
        /// </summary>
        /// <param name="voterId"></param>
        private static void CreateWhoWhereWords(byte voterId,bool isFirst = true)
        {
            string sendWord = $"インポスターチャットを開始します。\n内容を選択してください。\n";

            if (isFirst)
            {
                List<string> data = new()
                {
                    "プレイヤーを選択する",
                    "場所を選択する"
                };
                //ページ情報記憶
                //ページ作成
                CreatePageDatasList(voterId, data);
                //初期ページ設定
                ChatDatas[voterId].NowPage = 0;
            }
            //反映
            sendWord += CreatePageWords(PageDatas[voterId][ChatDatas[voterId].NowPage]);
            sendWord += "Skip:チャットを行わずスキップに投票する。";
            SendImposterChat(voterId, sendWord);
        }
        /// <summary>
        /// 送信主にのみ主語のプレイヤーを選択させる。
        /// </summary>
        /// <param name="voterId"></param>
        private static void CreateTargetWordsForPlayer(byte voterId)
        {
            string sendWord = $"主語になる生存プレイヤーを選択してください。\n\n";
            sendWord += "Skip:インポスターチャットを中断する。";
            SendImposterChat(voterId, sendWord);
        }
        /// <summary>
        /// 送信主にのみ主語のプレイヤーを選択させる。
        /// </summary>
        /// <param name="voterId"></param>
        private static void CreateTargetWordsForPlace(byte voterId,bool isFirst = true)
        {
            string sendWord = $"報告する場所を選択してください。\n\n";

            if (isFirst)
            {
                List<string> data = GetPlaceList();
                //ページ情報記憶
                //ページ作成
                CreatePageDatasList(voterId, data);
                //初期ページ設定
                ChatDatas[voterId].NowPage = 0;
            }
            //反映
            sendWord += CreatePageWords(PageDatas[voterId][ChatDatas[voterId].NowPage]);
            sendWord += "Skip:インポスターチャットを中断する。";
            SendImposterChat(voterId, sendWord);
        }
        /// <summary>
        /// 送信主にのみ主語のプレイヤーが何なのかを選択させる。
        /// </summary>
        /// <param name="voterId"></param>
        private static void CreateWhatWordsForPlayer(byte voterId, string name,bool isFirst = true)
        {
            string sendWord = $"送信する内容をを選択してください。";
            if (isFirst)
            {
                List<string> data = new()
                {
                    $"{name}に気をつけろ",
                    $"{name}を狙おう",
                    $"{name}と一緒にいたことにして"
                };
                //ページ情報記憶
                //ページ作成
                CreatePageDatasList(voterId, data);
                //初期ページ設定
                ChatDatas[voterId].NowPage = 0;
            }
            //反映
            sendWord += CreatePageWords(PageDatas[voterId][ChatDatas[voterId].NowPage]);
            sendWord += "Skip:インポスターチャットを中断する。";
            SendImposterChat(voterId, sendWord);
        }
        /// <summary>
        /// 送信主にのみ主語のプレイヤーが何なのかを選択させる。
        /// </summary>
        /// <param name="voterId"></param>
        private static void CreateWhatWordsForPlace(byte voterId, string room, bool isFirst = true)
        {
            string sendWord = $"送信する内容をを選択してください。";
            if (isFirst)
            {
                List<string> data = new()
                {
                    $"{room}にいた",
                    $"{room}でヤった",
                    $"{room}付近でヤった",
                    $"{room}にいたことにして"
                };
                //ページ情報記憶
                //ページ作成
                CreatePageDatasList(voterId, data);
                //初期ページ設定
                ChatDatas[voterId].NowPage = 0;
            }
            //反映
            sendWord += CreatePageWords(PageDatas[voterId][ChatDatas[voterId].NowPage]);
            sendWord += "Skip:インポスターチャットを中断する。";
            SendImposterChat(voterId, sendWord);
        }

        /// <summary>
        /// 1ページ作成する奴。利用する前に生存人数から精査してリストを作ること
        /// </summary>
        /// <param name="pageList"></param>
        /// <returns>完成品テキスト</returns>
        private static string CreatePageWords(List<string> pageList)
        {
            string returnText = "\n";

            byte counter = 0;
            var targetList = Main.AllAlivePlayerControls.Where(x => !x.Data.Disconnected).OrderBy(x => x.PlayerId);

            foreach (var pc in targetList)
            {
                //避け
                if (pageList.Count() <= counter) break;

                returnText += $"{pc.PlayerId}:{pageList[counter]}\n";

                //インクリメント
                counter++;
            }

            return returnText;
        }

        private static void CreatePageDatasList(byte voterId,List<string> pageList)
        {
            //生存人数
            float alivePlayerCount = Main.AllAlivePlayerControls.Count();

            //保管する奴
            List<List<string>> pageDatas = new();

            byte counter = 0;
            var targetList = Main.AllAlivePlayerControls.Where(x => !x.Data.Disconnected).OrderBy(x => x.PlayerId);

            //作るときに使いまわすIdリスト
            List<byte> aliveIds = new();
            //IDリスト作成
            foreach (var pc in targetList)
            {
                aliveIds.Add(pc.PlayerId);
            }

            //実際のページ作成
            do
            {
                //ページリスト
                List<string> dataList = new();

                for (int idCount = 0; idCount < aliveIds.Count; idCount++)
                {

                    int nokori = pageList.Count() - counter;

                    if (nokori <= 0)
                    {
                        if (pageDatas.Count() != 0)
                        {
                            dataList.Add($"{NEXT_PAGES}");
                        }
                        break;
                    }

                    //埋まらない時 = 選択肢(生存人数)より入力したい内容の方が多い時
                    bool nextPage = (idCount == aliveIds.Count() - 1) && nokori > 1 ;

                    //最後の場合
                    if (nextPage)
                    {
                        dataList.Add($"{NEXT_PAGES}");
                        break;
                    }
                    else
                    {
                        if (counter < pageList.Count())
                        {
                            dataList.Add($"{pageList[counter]}");
                        }
                    }
                    //インクリメント
                    counter++;
                }

                pageDatas.Add(dataList);

            }
            while (counter < pageList.Count());

            //セット
            PageDatas[voterId] = pageDatas;
        }

        private static void SendImposterChat(byte playerId,string data,string name = "")
        {
            if (name == "") name = Utils.ColorString(Color.red, "†インポスターチャット†");
            Utils.SendMessage(data, playerId, name);
        }
    }


}
