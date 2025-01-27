using System.Globalization;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using Hazel;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
    class MakePublicPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // 定数設定による公開ルームブロック
            if (!Main.AllowPublicRoom)
            {
                var message = GetString("DisabledByProgram");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            if (ModUpdater.isBroken || ModUpdater.hasUpdate || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)
            {
                var message = "";
                if (!Main.IsPublicAvailableOnThisVersion) message = GetString("PublicNotAvailableOnThisVersion");
                if (!VersionChecker.IsSupported) message = GetString("UnsupportedVersion");
                if (ModUpdater.isBroken) message = GetString("ModBrokenMessage");
                if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
    class MMOnlineManagerStartPatch
    {
        public static void Postfix(MMOnlineManager __instance)
        {
            if (!(ModUpdater.hasUpdate || ModUpdater.isBroken || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)) return;
            var obj = GameObject.Find("FindGameButton");
            if (obj)
            {
                obj?.SetActive(false);
                var parentObj = obj.transform.parent.gameObject;
                var textObj = Object.Instantiate<TMPro.TextMeshPro>(obj.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>());
                textObj.transform.position = new Vector3(1f, -0.3f, 0);
                textObj.name = "CanNotJoinPublic";
                textObj.DestroyTranslator();
                string message = "";
                if (ModUpdater.hasUpdate)
                {
                    message = GetString("CanNotJoinPublicRoomNoLatest");
                }
                else if (ModUpdater.isBroken)
                {
                    message = GetString("ModBrokenMessage");
                }
                else if (!VersionChecker.IsSupported)
                {
                    message = GetString("UnsupportedVersion");
                }
                else if (!Main.IsPublicAvailableOnThisVersion)
                {
                    message = GetString("PublicNotAvailableOnThisVersion");
                }
                textObj.text = $"<size=2>{Utils.ColorString(Color.red, message)}</size>";
            }
        }
    }
    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    class SplashLogoAnimatorPatch
    {
        public static void Prefix(SplashManager __instance)
        {
            if (DebugModeManager.AmDebugger)
            {
                __instance.sceneChanger.AllowFinishLoadingScene();
                __instance.startedSceneLoad = true;
            }
        }
    }
    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
    class RunLoginPatch
    {
        public static void Prefix(ref bool canOnline)
        {
#if DEBUG
            if (CultureInfo.CurrentCulture.Name != "ja-JP") canOnline = false;
#endif
        }
    }
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
    class BanMenuSetVisiblePatch
    {
        public static bool Prefix(BanMenu __instance, bool show)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
            __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
            __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
            __instance.MenuButton.gameObject.SetActive(show);
            return false;
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.CanBan))]
    class InnerNetClientCanBanPatch
    {
        public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
        {
            __result = __instance.AmHost;
            return false;
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.KickPlayer))]
    class KickPlayerPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, int clientId, bool ban)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
    class InnerNetObjectSerializePatch
    {
        public static bool Prefix(InnerNetClient __instance, ref bool __result)
        {
            if (AmongUsClient.Instance.AmHost)
                GameOptionsSender.SendAllGameOptions();

            //9人以上部屋で落ちる現象の対策コード
            if (!Options.FixSpawnPacketSize.GetBool()) return true;

            var sended = false;
            __result = false;
            var obj = __instance.allObjects;
            lock (obj)
            {
                for (int i = 0; i < __instance.allObjects.Count; i++)
                {
                    InnerNetObject innerNetObject = __instance.allObjects[i];
                    if (innerNetObject && innerNetObject.IsDirty && (innerNetObject.AmOwner ||
                        (innerNetObject.OwnerId == -2 && __instance.AmHost)))
                    {
                        var messageWriter = __instance.Streams[(byte)innerNetObject.sendMode];
                        if (messageWriter.Length > 500)
                        {
                            if (!sended)
                            {
                                if (DebugModeManager.IsDebugMode)
                                {
                                    Logger.Info($"SendAllStreamedObjects: Start", "InnerNetClient");
                                }
                                sended = true;
                            }
                            messageWriter.EndMessage();
                            __instance.SendOrDisconnect(messageWriter);
                            messageWriter.Clear(innerNetObject.sendMode);
                            messageWriter.StartMessage(5);
                            messageWriter.Write(__instance.GameId);
                        }
                        messageWriter.StartMessage(1);
                        messageWriter.WritePacked(innerNetObject.NetId);
                        try
                        {
                            if (innerNetObject.Serialize(messageWriter, false))
                            {
                                messageWriter.EndMessage();
                            }
                            else
                            {
                                messageWriter.CancelMessage();
                            }
                            if (innerNetObject.Chunked && innerNetObject.IsDirty)
                            {
                                Logger.Info($"SendAllStreamedObjects: Chunked", "InnerNetClient");
                                __result = true;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Logger.Info($"Exception:{ex.Message}", "InnerNetClient");
                            messageWriter.CancelMessage();
                        }
                    }
                }
            }
            for (int j = 0; j < __instance.Streams.Length; j++)
            {
                MessageWriter messageWriter2 = __instance.Streams[j];
                if (messageWriter2.HasBytes(7))
                {
                    if (!sended)
                    {
                        if (DebugModeManager.IsDebugMode)
                        {
                            Logger.Info($"SendAllStreamedObjects: Start", "InnerNetClient");
                        }
                        sended = true;
                    }
                    messageWriter2.EndMessage();
                    __instance.SendOrDisconnect(messageWriter2);
                    messageWriter2.Clear((SendOption)j);
                    messageWriter2.StartMessage(5);
                    messageWriter2.Write(__instance.GameId);
                }
            }
            if (DebugModeManager.IsDebugMode && sended) Logger.Info($"SendAllStreamedObjects: End", "InnerNetClient");
            return false;
        }
    }
    [HarmonyPatch]
    class InnerNetClientPatch
    {
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleMessage)), HarmonyPrefix]
        public static bool HandleMessagePatch(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
        {
            if (DebugModeManager.IsDebugMode)
            {
                Logger.Info($"HandleMessagePatch:Packet({reader.Length}) ,SendOption:{sendOption}", "InnerNetClient");
            }
            else if (reader.Length > 1000)
            {
                Logger.Info($"HandleMessagePatch:Large Packet({reader.Length})", "InnerNetClient");
            }
            return true;
        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect)), HarmonyPrefix]
        public static bool SendOrDisconnectPatch(InnerNetClient __instance, MessageWriter msg)
        {
            //分割するサイズ。大きすぎるとリトライ時不利、小さすぎると受信パケット取りこぼしが発生しうる。
            var limitSize = 500;

            if (DebugModeManager.IsDebugMode)
            {
                Logger.Info($"SendOrDisconnectPatch:Packet({msg.Length}) ,SendOption:{msg.SendOption}", "InnerNetClient");
            }
            else if (msg.Length > limitSize)
            {
                Logger.Info($"SendOrDisconnectPatch:Large Packet({msg.Length})", "InnerNetClient");
            }

            if (!Options.FixSpawnPacketSize.GetBool()) return true;

            //ラージパケットを分割(9人以上部屋で落ちる現象の対策コード)

            //メッセージが大きすぎる場合は分割して送信を試みる
            if (msg.Length > limitSize)
            {
                var writer = MessageWriter.Get(msg.SendOption);
                var reader = MessageReader.Get(msg.ToByteArray(false));

                //Tagレベルの処理
                while (reader.Position < reader.Length)
                {
                    //Logger.Info($"SendOrDisconnectPatch:reader {reader.Position} / {reader.Length}", "InnerNetClient");

                    var partMsg = reader.ReadMessage();
                    var tag = partMsg.Tag;

                    //Logger.Info($"SendOrDisconnectPatch:partMsg Tag={tag} Length={partMsg.Length}", "InnerNetClient");

                    //TagがGameData,GameDataToの場合のみ分割処理
                    //それ以外では多分分割しなくても問題ない
                    if (tag is 5 or 6 && partMsg.Length > limitSize)
                    {
                        //分割を試みる
                        DivideLargeMessage(__instance, writer, partMsg);
                    }
                    else
                    {
                        //そのまま追加
                        WriteMessage(writer, partMsg);
                    }

                    //送信サイズが制限を超えた場合は送信
                    if (writer.Length > limitSize)
                    {
                        Send(__instance, writer);
                        writer.Clear(writer.SendOption);
                    }
                }

                //残りの送信
                if (writer.HasBytes(7))
                {
                    Send(__instance, writer);
                }

                writer.Recycle();
                reader.Recycle();
                return false;
            }
            return true;
        }
        private static void DivideLargeMessage(InnerNetClient __instance, MessageWriter writer, MessageReader partMsg)
        {
            var tag = partMsg.Tag;
            var GameId = partMsg.ReadInt32();
            var ClientId = -1;

            //元と同じTagを開く
            writer.StartMessage(tag);
            writer.Write(GameId);
            if (tag == 6)
            {
                ClientId = partMsg.ReadPackedInt32();
                writer.WritePacked(ClientId);
            }

            //Flag単位の処理
            while (partMsg.Position < partMsg.Length)
            {
                var subMsg = partMsg.ReadMessage();
                var subLength = subMsg.Length;

                //加算すると制限を超える場合は先に送信
                if (writer.Length + subLength > 500)
                {
                    writer.EndMessage();
                    Send(__instance, writer);
                    //再度Tagを開く
                    writer.Clear(writer.SendOption);
                    writer.StartMessage(tag);
                    writer.Write(GameId);
                    if (tag == 6)
                    {
                        writer.WritePacked(ClientId);
                    }
                }
                //メッセージの出力
                WriteMessage(writer, subMsg);
            }
            writer.EndMessage();
        }

        private static void WriteMessage(MessageWriter writer, MessageReader reader)
        {
            writer.Write((ushort)reader.Length);
            writer.Write(reader.Tag);
            writer.Write(reader.ReadBytes(reader.Length));
        }

        private static void Send(InnerNetClient __instance, MessageWriter writer)
        {
            Logger.Info($"SendOrDisconnectPatch: SendMessage Length={writer.Length}", "InnerNetClient");
            var err = __instance.connection.Send(writer);
            if (err != SendErrors.None)
            {
                Logger.Info($"SendOrDisconnectPatch: SendMessage Error={err}", "InnerNetClient");
            }
        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendInitialData)), HarmonyPrefix]
        public static bool SendInitialDataPatch(InnerNetClient __instance, int clientId)
        {
            if (!Options.FixSpawnPacketSize.GetBool()) return true;
            if (DebugModeManager.IsDebugMode)
            {
                Logger.Info($"SendInitialData: Start", "InnerNetClient");
            }
            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.StartMessage(6);
            messageWriter.Write(__instance.GameId);
            messageWriter.WritePacked(clientId);

            var obj = __instance.allObjects;
            lock (obj)
            {
                var hashSet = new System.Collections.Generic.HashSet<GameObject>();
                //まずはGameManagerを送信
                GameManager gameManager = GameManager.Instance;
                __instance.SendGameManager(clientId, gameManager);
                hashSet.Add(gameManager.gameObject);

                for (int i = 0; i < __instance.allObjects.Count; i++)
                {
                    InnerNetObject innerNetObject = __instance.allObjects[i];
                    if (innerNetObject && (innerNetObject.OwnerId != -4 || __instance.AmModdedHost) && hashSet.Add(innerNetObject.gameObject))
                    {
                        if (messageWriter.Length > 500)
                        {
                            messageWriter.EndMessage();
                            __instance.SendOrDisconnect(messageWriter);
                            messageWriter.Clear(SendOption.Reliable);
                            messageWriter.StartMessage(6);
                            messageWriter.Write(__instance.GameId);
                            messageWriter.WritePacked(clientId);

                        }
                        __instance.WriteSpawnMessage(innerNetObject, innerNetObject.OwnerId, innerNetObject.SpawnFlags, messageWriter);
                    }
                }
            }
            messageWriter.EndMessage();
            __instance.SendOrDisconnect(messageWriter);
            messageWriter.Recycle();
            if (DebugModeManager.IsDebugMode)
            {
                Logger.Info($"SendInitialData: End", "InnerNetClient");
            }
            return false;
        }
        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Spawn)), HarmonyPostfix]
        public static void SpawnPatch(InnerNetClient __instance, InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
        {
            if (DebugModeManager.IsDebugMode)
            {
                Logger.Info($"SpawnPatch", "InnerNetClient");
            }
            var messageWriter = __instance.Streams[(byte)SendOption.Reliable];
            if (messageWriter.Length > 500)
            {
                messageWriter.EndMessage();
                __instance.SendOrDisconnect(messageWriter);
                messageWriter.Clear(SendOption.Reliable);
                messageWriter.StartMessage(5);
                messageWriter.Write(__instance.GameId);
            }
        }
    }
}