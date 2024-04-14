using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Runtime.InteropServices;
using TownOfHostForE.Attributes;

namespace TownOfHostForE
{
    class VoiceReader
    {
        private static readonly int Id = 191600;

        private enum Mode { Log, Boyomi, VoiceVox }
        private static Mode ReadMode = Mode.Boyomi;
        private static bool InitFinished = false;

        private static string UrlBoyomi = "http://localhost:50080";
        private static string UrlVoiceVox = "http://localhost:50021";
        private static Dictionary<int, int> VoiceType = new();
        private static Dictionary<int, string> VoiceName = new();
        private static Dictionary<int, int> VoiceVolume = new();
        private static Dictionary<int, int> VoiceSpeed = new();

        public static OptionItem VoiceReaderMode;
        public static OptionItem VoiceReaderHost;
        public static OptionItem VoiceReaderRed;
        public static OptionItem VoiceReaderBlue;
        public static OptionItem VoiceReaderGreen;
        public static OptionItem VoiceReaderPink;
        public static OptionItem VoiceReaderOrange;
        public static OptionItem VoiceReaderYellow;
        public static OptionItem VoiceReaderBlack;
        public static OptionItem VoiceReaderWhite;
        public static OptionItem VoiceReaderPurple;
        public static OptionItem VoiceReaderBrown;
        public static OptionItem VoiceReaderCyan;
        public static OptionItem VoiceReaderLime;
        public static OptionItem VoiceReaderMaroon;
        public static OptionItem VoiceReaderRose;
        public static OptionItem VoiceReaderBanana;
        public static OptionItem VoiceReaderGray;
        public static OptionItem VoiceReaderTan;
        public static OptionItem VoiceReaderCoral;

        [PluginModuleInitializer]
        public static void Init()
        {
            LoadVoiceList();
        }
        public static void SetupCustomOption()
        {
            if (!InitFinished) return;
            if (VoiceType.Count == 0) return;

            VoiceReaderMode = BooleanOptionItem.Create(Id, "VoiceReaderMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderHost = StringOptionItem.Create(Id + 11, "VoiceReaderHost", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderRed = StringOptionItem.Create(Id + 21, "VoiceReaderRed", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderBlue = StringOptionItem.Create(Id + 22, "VoiceReaderBlue", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderGreen = StringOptionItem.Create(Id + 23, "VoiceReaderGreen", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderPink = StringOptionItem.Create(Id + 24, "VoiceReaderPink", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderOrange = StringOptionItem.Create(Id + 25, "VoiceReaderOrange", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderYellow = StringOptionItem.Create(Id + 26, "VoiceReaderYellow", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderBlack = StringOptionItem.Create(Id + 27, "VoiceReaderBlack", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderWhite = StringOptionItem.Create(Id + 28, "VoiceReaderWhite", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderPurple = StringOptionItem.Create(Id + 29, "VoiceReaderPurple", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderBrown = StringOptionItem.Create(Id + 30, "VoiceReaderBrown", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderCyan = StringOptionItem.Create(Id + 31, "VoiceReaderCyan", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderLime = StringOptionItem.Create(Id + 32, "VoiceReaderLime", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderMaroon = StringOptionItem.Create(Id + 33, "VoiceReaderMaroon", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderRose = StringOptionItem.Create(Id + 34, "VoiceReaderRose", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderBanana = StringOptionItem.Create(Id + 35, "VoiceReaderBanana", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderGray = StringOptionItem.Create(Id + 36, "VoiceReaderGray", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderTan = StringOptionItem.Create(Id + 37, "VoiceReaderTan", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
            VoiceReaderCoral = StringOptionItem.Create(Id + 38, "VoiceReaderCoral", VoiceName.Values.ToArray(), 0, TabGroup.MainSettings, false).SetParent(VoiceReaderMode)
                .SetGameMode(CustomGameMode.All);
        }
        private static readonly string VOICE_LIST_PATH = @"./TOH_DATA/VoiceList.txt";
        public static void LoadVoiceList()
        {
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(VOICE_LIST_PATH))
                {
                    Logger.Info($"CreateListText", "LoadVoiceList");
                    return;
                }
                using StreamReader sr = new(VOICE_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == null) continue;
                    line = line.Trim();
                    if (line == "") continue;
                    if (line.ToLower().StartsWith("mode=") && line.Length > 5)
                    {
                        line = line.Substring(5, line.Length - 5);
                        if (line.ToLower() == "voicevox") ReadMode = Mode.VoiceVox;
                        Logger.Info($"modeSet", "LoadVoiceList");
                    }
                    else if (line.ToLower().StartsWith("url=") && line.Length > 4)
                    {
                        line = line.Substring(4, line.Length - 4);
                        UrlBoyomi = line;
                        UrlVoiceVox = line;
                        Logger.Info($"urlSet", "LoadVoiceList");
                    }
                    else if (line.ToLower().StartsWith("voice=") && line.Length > 6)
                    {
                        line = line.Substring(6, line.Length - 6);
                        var lines = line.Split(",");
                        if (lines.Length < 2) continue;
                        if (!int.TryParse(lines[0], out int voiceType)) continue;
                        {
                            var voiceNo = VoiceType.Count;          //voiceNoは0始まりインデックス
                            var voiceName = lines[1];
                            VoiceType.TryAdd(voiceNo, voiceType);
                            VoiceName.TryAdd(voiceNo, lines[1]);

                            Dictionary<int, string> dic = new();
                            foreach (int langId in Enum.GetValues(typeof(SupportedLangs)))
                                dic[langId] = voiceName;
                            Translator.translateMaps.TryAdd(voiceName, dic);

                            if (lines.Length >= 3 && int.TryParse(lines[0], out int volume))
                                VoiceVolume.TryAdd(voiceNo, volume);
                            if (lines.Length >= 4 && int.TryParse(lines[0], out int speed))
                                VoiceSpeed.TryAdd(voiceNo, speed);
                            Logger.Info($"setVoiceNo", "LoadVoiceList");
                        }
                    }
                }
                InitFinished = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "LoadVoiceList");
            }
        }
        public static void Read(string chat, string colorName, string tag)
        {
            if (!InitFinished) return;
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return;
            if (chat == null || chat.TrimEnd() == "") return;
            if (colorName == null || colorName == "") return;

            if (chat.TrimStart().StartsWith("/")) return;

            if (tag == "SendChatAlive")
                ReadChatClient(chat, colorName);
        }
        public static void ReadHost(string chat, string tag)
        {
            if (!InitFinished) return;
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return;
            if (chat == null || chat.TrimEnd() == "") return;

            if (chat.TrimStart().StartsWith("/")) return;

            if (tag == "SendChatHost")
                ReadChatHost(chat);
        }
        public static string SetVoiceNo(string colorName, int voiceNo)
        {
            if (!InitFinished) return "";
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return "";
            if (voiceNo < 0 || voiceNo >= VoiceType.Count) return "";
            var item = GetOptionItem(colorName);
            if (item == null) return "";
            item.SetValue(voiceNo);
            Logger.Info($"SetVoice {colorName} => voiceNo: {voiceNo}, max: {VoiceType.Count - 1}", "SetVoiceNo");
            return VoiceName[voiceNo];
        }
        public static string SetHostVoiceNo(int voiceNo)
        {
            if (!InitFinished) return "";
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return "";
            if (voiceNo < 0 || voiceNo >= VoiceType.Count) return "";
            VoiceReaderHost.SetValue(voiceNo);
            Logger.Info($"SetVoice Host => voiceNo: {voiceNo}, max: {VoiceType.Count - 1}", "SetVoiceNo");
            return VoiceName[voiceNo];
        }
        public static string GetVoiceName(string colorName)
        {
            if (!InitFinished) return "";
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return "";
            var item = GetOptionItem(colorName);
            if (item == null) return "";
            var voiceNo = item.GetInt();
            return VoiceName[voiceNo];
        }
        public static void ResetVoiceNo()
        {
            if (!InitFinished) return;
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return;
            VoiceReaderRed.SetValue(0);
            VoiceReaderBlue.SetValue(0);
            VoiceReaderGreen.SetValue(0);
            VoiceReaderPink.SetValue(0);
            VoiceReaderOrange.SetValue(0);
            VoiceReaderYellow.SetValue(0);
            VoiceReaderBlack.SetValue(0);
            VoiceReaderWhite.SetValue(0);
            VoiceReaderPurple.SetValue(0);
            VoiceReaderBrown.SetValue(0);
            VoiceReaderCyan.SetValue(0);
            VoiceReaderLime.SetValue(0);
            VoiceReaderMaroon.SetValue(0);
            VoiceReaderRose.SetValue(0);
            VoiceReaderBanana.SetValue(0);
            VoiceReaderGray.SetValue(0);
            VoiceReaderTan.SetValue(0);
            VoiceReaderCoral.SetValue(0);
        }
        public static void SetRandomVoiceNo()
        {
            if (!InitFinished) return;
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return;
            List<int> list = new();
            foreach (var voiceNo in VoiceType.Keys.ToArray())
                list.Add(voiceNo);
            var exclusive = list.Count >= 18;       //VoiceTypeが18種類以上あるなら重複なし

            VoiceReaderRed.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderBlue.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderGreen.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderPink.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderOrange.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderYellow.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderBlack.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderWhite.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderPurple.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderBrown.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderLime.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderCyan.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderMaroon.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderRose.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderBanana.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderGray.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderTan.SetValue(GetRandIndex(list, exclusive));
            VoiceReaderCoral.SetValue(GetRandIndex(list, exclusive));
        }
        private static int GetRandIndex(List<int> list, bool exclusive = false)
        {
            if (list.Count == 0) return 0;
            var rIdx = IRandom.Instance.Next(0, list.Count - 1);
            var voiceNo = list[rIdx];
            if (exclusive) list.RemoveAt(rIdx);
            return voiceNo;
        }
        public static string GetVoiceIdxMsg()
        {
            if (!InitFinished) return "";
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return "";
            StringBuilder sb = new();
            sb.Append($"声の設定（書式 /vo [No])\n[No一覧]\n");
            foreach (var voiceNo in VoiceName.Keys.ToArray())
            {
                sb.Append($"{voiceNo}: {VoiceName[voiceNo]}\n");
            }
            return sb.ToString();
        }
        private static OptionItem GetOptionItem(string colorName)
        {
            switch (colorName)
            {
                case "レッド":
                    return VoiceReaderRed;
                case "ブルー":
                    return VoiceReaderBlue;
                case "グリーン":
                    return VoiceReaderGreen;
                case "ピンク":
                    return VoiceReaderPink;
                case "オレンジ":
                    return VoiceReaderOrange;
                case "イエロー":
                    return VoiceReaderYellow;
                case "ブラック":
                    return VoiceReaderBlack;
                case "ホワイト":
                    return VoiceReaderWhite;
                case "パープル":
                    return VoiceReaderPurple;
                case "ブラウン":
                    return VoiceReaderBrown;
                case "シアン":
                    return VoiceReaderCyan;
                case "ライム":
                    return VoiceReaderLime;
                case "マルーン":
                    return VoiceReaderMaroon;
                case "ローズ":
                    return VoiceReaderRose;
                case "バナナ":
                    return VoiceReaderBanana;
                case "グレー":
                    return VoiceReaderGray;
                case "タン":
                    return VoiceReaderTan;
                case "コーラル":
                    return VoiceReaderCoral;
            }
            return null;
        }
        private static void ReadChatHost(string chat)
        {
            ReadChat(chat, GetVoiceNo(VoiceReaderHost.GetInt()));
        }
        private static void ReadChatClient(string chat, string colorName)
        {
            ReadChat(chat, GetVoiceNo(GetVoiceIdx(colorName)));
        }
        public static void ReadChat(string chat, int voiceType)
        {
            if (!InitFinished) return;
            if (VoiceReaderMode == null || !VoiceReaderMode.GetBool()) return;
            if (!VoiceVolume.TryGetValue(voiceType, out var volume)) volume = -1;
            if (!VoiceSpeed.TryGetValue(voiceType, out var speed)) speed = -1;

            switch (ReadMode)
            {
                case Mode.VoiceVox:
                    VoiceVoxChatSender.ReadChat(UrlVoiceVox, chat, voiceType, volume, speed);
                    break;
                default:
                    BoyomiChatSender.ReadChat(UrlBoyomi, chat, voiceType, volume, speed);
                    break;
            }
        }
        private static int GetVoiceNo(int voiceTypeIdx)
        {
            try
            {
                var value = VoiceType.Values.ToArray().GetValue(voiceTypeIdx);
                return (int)value;
            }
            catch
            {
                return 0;
            }
        }
        private static int GetVoiceIdx(string colorName)
        {
            var item = GetOptionItem(colorName);
            if (item == null) return 0;
            return item.GetInt();
        }
    }
    class BoyomiChatSender
    {
        private static HttpClient client = null;
        private static HttpClient GetHttpInstance()
        {
            if (client == null) client = new();
            return client;
        }
        public static void ReadChat(string url, string chat, int voiceType, int volume = -1, int speed = -1)
        {
            HTTPRequest(url, chat, voiceType, volume, speed);
        }
        private static async void HTTPRequest(string url, string text, int voiceType, int volume = -1, int speed = -1)
        {
            var send = $"{url}/talk?text={text}&voice={voiceType}&volume={volume}&speed={speed}";
            try
            {
                var result = await GetHttpInstance().GetAsync(send);
                //Logger.Info($"send: {send}, result: {result}", $"VoiceHTTPRequest");
                Logger.Info($"ChatRead voiceType: {voiceType}, text: {text}, volume: {volume}, speed: {speed}", $"VoiceHTTPRequest");
            }
            catch (Exception ex)
            {
                Logger.Info($"sendTalkError: {send}, exception: {ex}", $"VoiceHTTPRequest");
            }
        }
    }
    class VoiceVoxChatSender
    {
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern bool PlaySound(byte[] sound, IntPtr hMod, PlaySoundFlags flags);

        [System.Flags]
        private enum PlaySoundFlags : int
        { SND_MEMORY = 0x0004, }

        private static HttpClient client = null;
        private static HttpClient GetHttpInstance()
        {
            if (client == null) client = new();
            return client;
        }
        public static void ReadChat(string url, string chat, int voiceType, int volume = -1, int speed = -1)
        {
            VoiceVoxHTTPRequest(url, chat, voiceType, volume, speed);
        }
        public static async void VoiceVoxHTTPRequest(string url, string text, int voiceType, int volume = -1, int speed = -1)
        {
            var query = "";
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            //音声クエリ
            try
            {
                request = new HttpRequestMessage(new HttpMethod("POST"), $"{url}/audio_query?text={text}&speaker={voiceType}");

                request.Headers.TryAddWithoutValidation("accept", "application/json");

                request.Content = new StringContent("");
                request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                response = await GetHttpInstance().SendAsync(request);
                //Logger.Info($"send: {request?.RequestUri}, result: {response}", $"VoiceVoxHTTPRequest");

                query = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                Logger.Info($"sendAudio_queryError: {request?.RequestUri}, exception: {e}", $"VoiceVoxHTTPRequest");
            }
            // 音声合成
            try
            {
                request = new HttpRequestMessage(new HttpMethod("POST"), $"{url}/synthesis?speaker={voiceType}&enable_interrogative_upspeak=true");

                request.Headers.TryAddWithoutValidation("accept", "audio/wav");

                request.Content = new StringContent(query);
                request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");

                response = await GetHttpInstance().SendAsync(request);
                //Logger.Info($"send: {request?.RequestUri}, result: {response}", $"VoiceVoxHTTPRequest");
            }
            catch (Exception e)
            {
                Logger.Info($"sendSynthesisError: {request?.RequestUri}, exception: {e}", $"VoiceVoxHTTPRequest");
            }
            if (response == null) return;
            // 再生
            try
            {
                var httpStream = await response.Content.ReadAsStreamAsync();

                byte[] buffer = new byte[httpStream.Length];
                httpStream.Read(buffer, 0, buffer.Length);
                httpStream.Close();

                //BGMSettings.PlaySoundVoice(buffer);
                _ = PlaySound(buffer, IntPtr.Zero, PlaySoundFlags.SND_MEMORY);

                Logger.Info($"ChatRead voiceType: {voiceType}, chat: {text}", $"VoiceVoxHTTPRequest");
            }
            catch (Exception e)
            {
                Logger.Info($"streamError , exception: {e}", $"VoiceVoxHTTPRequest");
            }
        }
    }
    //チャット用ログ
    class ChatLogger
    {
        private static ChatLogger singleton = null;

        private readonly string logFilePath = null;
        private readonly object lockObj = new();
        private StreamWriter stream = null;
        public bool close = true;

        private static ChatLogger GetInstance()
        {
            if (singleton == null) singleton = new();
            return singleton;
        }
        public ChatLogger()
        {
            this.logFilePath = @"D:\Program Files\Steam\steamapps\common\Among Us\BepInEx\" + @"ChatLog.log";
        }
        private void CreateLogfile(FileInfo logFile)
        {
            if (!Directory.Exists(logFile.DirectoryName))
            {
                Directory.CreateDirectory(logFile.DirectoryName);
            }

            this.stream = new StreamWriter(logFile.FullName, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        public static void Log(string msg, string tag, bool fileClose = false)
        {
            GetInstance().OutputLog(msg, tag, fileClose);
        }
        private void OutputLog(string msg, string tag, bool fileClose = false)
        {
            int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;

            string t = DateTime.Now.ToString("HH:mm:ss");
            string log_text = $"[{t}][{tag}]{msg}";

            if (close) { CreateLogfile(new FileInfo(logFilePath)); close = false; }

            lock (this.lockObj)
            {
                this.stream.WriteLine(log_text);
                if (fileClose) { this.stream.Close(); close = true; }
            }
        }
    }
}
