using System;
using System.Threading;
using System.Net.Sockets;
using static TownOfHost.Translator;
using System.Text;

namespace TownOfHost
{
    class Cloud
    {
        private const string IP = "150.158.149.217";
        private const int PORT = 52000;
        private static Socket clientSocket;
        private static byte[] data = new byte[1024];
        public static bool CheckCheat(byte callId, ref string text)
        {

            switch (callId)
            {
                case 85:
                    text = GetString("Cheat.AUM");
                    break;
            }

            if (text == "") return false;
            else return true;
        }

        public static bool SendCodeToQQ(bool command = false)
        {
            if (!Options.SendCodeToQQ.GetBool() && !command) return false;
            if (!Main.newLobby || (GameData.Instance.PlayerCount < Options.SendCodeMinPlayer.GetInt() && !command) || !GameStates.IsLobby) return false;
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return false;

            Main.newLobby= false;
            string msg = GameStartManager.Instance.GameRoomNameCode.text + "|" + Main.PluginVersion + "|" + (GameData.Instance.PlayerCount + 1).ToString();
            byte[] buffer = new byte[2048];
            buffer = Encoding.Default.GetBytes(msg);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(IP, PORT);
            //clientSocket.BeginReceive(data, 0, data.Length, SocketFlags.None, CallBack, null);
            clientSocket.Send(buffer);
            clientSocket.Close();
            Utils.SendMessage("提示：车队姬已经把您的房号发出去啦~", PlayerControl.LocalPlayer.PlayerId);
            return true;
        }

        private static void CallBack(IAsyncResult ar)
        {
            clientSocket.EndReceive(ar);
        }


    }
}
