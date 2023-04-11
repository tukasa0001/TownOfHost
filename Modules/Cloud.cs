using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace TOHE;

internal class Cloud
{
    private const string IP = "150.158.149.217";
    public static int Remote_int = 0;
    private static readonly int LOBBY_PORT = Remote_int;
    private static readonly int EAC_PORT = Remote_int + 5;
    private static Socket ClientSocket;
    private static Socket EacClientSocket;
    private static long LastRepotTimeStamp = 0;
    public static bool SendCodeToQQ(bool command = false)
    {
        try
        {
            if (!Options.SendCodeToQQ.GetBool() && !command) return false;
            if (!Main.newLobby || (GameData.Instance.PlayerCount < Options.SendCodeMinPlayer.GetInt() && !command) || !GameStates.IsLobby) return false;
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return false;

            if (LOBBY_PORT == 0) throw new("Get remote port faild");

            Main.newLobby = false;
            string msg = $"{GameStartManager.Instance.GameRoomNameCode.text}|{Main.PluginVersion}|{GameData.Instance.PlayerCount + 1}";
            if (!LOBBY_PORT.ToString().EndsWith("1"))
                msg += $"|{TranslationController.Instance.currentLanguage.languageID}|{DataManager.player.customization.name}";
            byte[] buffer = Encoding.Default.GetBytes(msg);

            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket.Connect(IP, LOBBY_PORT);
            ClientSocket.Send(buffer);
            ClientSocket.Close();
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
                Utils.SendMessage("已请求车队姬群发您的房号", PlayerControl.LocalPlayer.PlayerId);
        }
        catch (Exception e)
        {
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
                Utils.SendMessage("车队姬似乎不在线捏", PlayerControl.LocalPlayer.PlayerId);
            Logger.Exception(e, "SentLobbyToQQ");
            throw e;
        }
        return true;
    }

    private static bool connecting = false;
    public static void StartConnect()
    {
        if (connecting || EacClientSocket != null && EacClientSocket.Connected) return;
        connecting = true;
        new LateTask(() =>
        {
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame)
            {
                connecting = false;
                return;
            }
            try
            {
                LastRepotTimeStamp = Utils.GetTimeStamp(DateTime.Now);
                EacClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EacClientSocket.Connect(IP, EAC_PORT);
                Logger.Warn("已连接至TOHE服务器", "EAC Cloud");
            }
            catch (Exception e)
            {
                connecting = false;
                Logger.Exception(e, "EAC Cloud");
                throw e;
            }
            connecting = false;
        }, 3.5f, "EAC Cloud Connect");
    }
    public static void StopConnect()
    {
        if (EacClientSocket != null && EacClientSocket.Connected)
            EacClientSocket.Close();
    }
    public static void SendData(string msg)
    {
        StartConnect();
        if (EacClientSocket == null || !EacClientSocket.Connected)
        {
            Logger.Warn("未连接至TOHE服务器，报告被取消", "EAC Cloud");
            return;
        }
        EacClientSocket.Send(Encoding.Default.GetBytes(msg));
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class EACConnectTimeOut
    {
        public static void Postfix()
        {
            if (LastRepotTimeStamp != 0 && LastRepotTimeStamp + 8 < Utils.GetTimeStamp(DateTime.Now))
            {
                LastRepotTimeStamp = 0;
                StopConnect();
                Logger.Warn("超时自动断开与TOHE服务器的连接", "EAC Cloud");
            }
        }
    }
}