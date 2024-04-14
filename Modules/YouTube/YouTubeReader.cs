using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TownOfHostForE.Attributes;
using YoutubeLiveChatSharp;
using UnityEngine;
using TownOfHostForE.Roles.Crewmate;

namespace TownOfHostForE
{
    class YouTubeReader
    {
        private static readonly string YT_SETTING__PATH = @"./TOH_DATA/YouTubeLiveId.csv";

        private static bool InitFinished = false;

        public static string liveId = "";

        public static ChatFetch chat;
        private static List<Comment> youtubeComment = new();
        private static float UpdateTime;
        private static string tempComment = "";

        [PluginModuleInitializer]
        public static void Init()
        {
            try
            {
                chat = new ChatFetch(liveId);
                UpdateTime = 1.0f;
                ReadCsvData();
                InitFinished = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "YouTube");
            }

        }

        private static void ReadCsvData()
        {
            try
            {
                Logger.Msg("YouTube読み取り開始", "YouTube");
                using (StreamReader reader = new(YT_SETTING__PATH))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        liveId = line;
                        chat = new ChatFetch(liveId);
                        Logger.Info(liveId,"YouTube");
                    }
                }
                Logger.Msg("YouTube読み取り終了", "YouTube");
            }
            catch (Exception e)
            {
                throw new Exception("YouTube読み取り例外：" + e.Message + "/" + e.StackTrace);
            }
        }

        public static void FixedUpdatePatch()
        {
            try
            {
                if (!InitFinished) return;
                if (liveId == "") return;
                UpdateTime -= Time.fixedDeltaTime;
                if (UpdateTime < 0) UpdateTime = 0.5f; // 負荷軽減の為1秒ごとの更新

                if (UpdateTime == 0.5f)
                {
                    Task task = Task.Run(getComment);
                }
            }
            catch (Exception e)
            {
                liveId = "";
                Logger.Info("対象の配信を取得できませんでした。","YouTube");
            }
        }

        private static void getComment()
        {
            var chatList = chat.Fetch().ToList().Distinct();
            Metaton.SetCount(chatList.Count());

            if (!GameStates.IsLobby) return;
            foreach (var data in chatList)
            {
                ShowComment(data);
            }
        }

        private static void ShowComment(Comment c)
        {
            Logger.Info($"name: {c.userName}\n" +
                $"    message: {c.text}","YouTube");

            Logger.SendInGame("【" + c.userName + "】" + c.text);
        }
    }
}