using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TownOfHostForE.Attributes;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.Win32;
using TownOfHostForE.Modules;
using Hazel;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.GameMode;

namespace TownOfHostForE
{
    class BGMSettings
    {
        private static readonly int Id = 252526;

        public static OptionItem BGMMode;
        public static OptionItem ClimaxCount;

        private static readonly string BGM_SETTING__PATH = @"./TOH_DATA/BgmSetting.csv";
        public  static readonly string WAV_SETTING__PATH = @"./TOH_DATA/BGM/";
        //private static readonly int LOBBY_INDEX = 0;
        //private static readonly int GAME_INDEX = 1;
        //private static readonly int MEETING_INDEX = 2;
        //private static readonly int CLIMAX_INDEX = 3;
        //private static readonly int RESULT_INDEX = 4;

        private static List<BGMData> csvData = new();

        private static string NowBGM = "";

        private static bool InitFinished = false;
        public static bool spBGM = false;

        // XAudio2関連
        private static XAudio2 xaDevice;
        // 全てのボイスデータを合成・生成して、マスターボイスとしてサウンドカードデバイスにデータを送るクラス
        private static MasteringVoice xaMaster;
        // 各効果音をボイスデータとして管理するためのクラス
        private static List<SourceVoice> xaSESourceList = new List<SourceVoice>();

        //static WaveOutEvent outputDevice = new();
        ////static IWavePlayer player;

        [PluginModuleInitializer]
        public static void Init()
        {
            try
            {
                // XAudio初期化
                xaDevice = new XAudio2();
                xaMaster = new MasteringVoice(xaDevice);

                //csvData = ReadCsvFilesFromFolder();
                NowBGM = "";
                InitFinished = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "LoadBGMData");
            }

        }

        [GameModuleInitializer]
        public static void GameInit()
        {
            try
            {
                spBGM = false;
            }
            catch (Exception ex)
            {
                Logger.Info("BGM設定初期化に失敗しました。st:" + ex.Message + "/" + ex.StackTrace, "betwin");
            }
        }
        public static void SetupCustomOption()
        {
            if (!InitFinished) return;

            BGMMode = BooleanOptionItem.Create(Id, "BGMMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            ClimaxCount = IntegerOptionItem.Create(Id + 10, "BGMModeClimax", new(2, 15, 1), 6, TabGroup.MainSettings, false).SetParent(BGMMode)
                .SetValueFormat(OptionFormat.Players);
        }

        public static void SetLobbyBGM()
        {
            if (!BGMMode.GetBool()) return;
            NowBGM = "lobby";
            CustomSoundsManager.PlayBGM(NowBGM,1);
        }
        public static void SetMeetingBGM()
        {
            if (!BGMMode.GetBool()) return;

            NowBGM = "meeting";
            CustomSoundsManager.PlayBGM(NowBGM, 1);
        }
        public static void SetEndingBGM()
        {
            if (!BGMMode.GetBool()) return;
            NowBGM = "result";
            CustomSoundsManager.PlayBGM(NowBGM, 1);
        }

        public static void SetTaskBGM()
        {
            if (!BGMMode.GetBool()) return;

            if (Options.CurrentGameMode == CustomGameMode.SuperBombParty)
            {
                //CustomSoundsManager.StopBGM();
                CustomSoundsManager.StopSound();
                if (SuperBakuretsuBros.ChangeBGM) return;
                if (Main.AllAlivePlayerControls.Count() <= ClimaxCount.GetInt())
                {
                    NowBGM = "sbpcx";
                    //CustomSoundsManager.StopBGM();
                    CustomSoundsManager.PlayBGMForWav(NowBGM);
                    SuperBakuretsuBros.ChangeBGM = true;
                }
                else
                {
                    NowBGM = "sbp";
                    CustomSoundsManager.PlayBGMForWav(NowBGM);
                }
            }
            //かくれんぼ、若しくは通常
            else
            {
                if (spBGM)
                {
                    string spBGMName = Metaton.retBGMName();

                    CustomSoundsManager.PlayBGM(spBGMName, 1);
                }
                else
                {
                    if (Main.AllAlivePlayerControls.Count() <= ClimaxCount.GetInt())
                    {
                        NowBGM = "climax";
                        CustomSoundsManager.PlayBGM(NowBGM, 1);
                    }
                    else
                    {
                        NowBGM = "intask";
                        CustomSoundsManager.PlayBGM(NowBGM, 1);
                    }
                }
            }
        }

        public static List<BGMData> ReadCsvFilesFromFolder()
        {
            List<BGMData> csvData = new();

            // 指定のフォルダ内のCSVファイルをすべて取得
            try
            {
                using (System.IO.StreamReader reader = new(BGM_SETTING__PATH))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] fields = line.Split(',');

                        // CSVの各列の値をオブジェクトにマッピングする
                        var bgmdata = new BGMData
                        {
                            Timeing = fields[0],
                            FileName = fields[1],
                            Volume = int.Parse(fields[2])
                        };

                        // リストに追加する
                        csvData.Add(bgmdata);
                    }
                }
                // CSVファイルの読み込み処理などを行う
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                throw new Exception($"BGM パスが存在しません");
            }
            catch (UnauthorizedAccessException)
            {
                throw new Exception($"BGM アクセス権限がありません");
            }
            catch (Exception ex)
            {
                //Logger.Info($"エラーが発生しました:" + ex.Message, "BGM");
                throw new Exception($"BGM CSVエラー:" + ex.Message);
            }

            return csvData;
        }
        public class BGMData
        {
            public string Timeing { get; set; }
            public string FileName { get; set; }
            public int Volume { get; set; }
        }


        public static void PlaySoundSE(string soundFileName,byte playerId = byte.MaxValue)
        {
            if (!InitFinished) return;
            if (!Main.EnableCustomSoundEffect.Value) return;
            //効果音を鳴らす対象でないなら再生しない。
            if (playerId != byte.MaxValue && playerId != PlayerControl.LocalPlayer.PlayerId) return;

            Logger.Info("soundfile:" + soundFileName,"BGM");
            var path = CustomSoundsManager.SOUNDS_PATH + soundFileName + ".wav";
            try
            {
                switch (Path.GetExtension(path))
                {
                    case ".wav":
                    {
                        // サウンドデバイスが無い場合処理しない
                        if (xaDevice == null) { return; }

                        // WAVEファイルを読み込んで設定
                        using (SoundStream xaStream = new SoundStream(File.OpenRead(path)))
                        {
                            SourceVoice xaSource = new SourceVoice(xaDevice, xaStream.Format);
                            AudioBuffer xaBuffer = new AudioBuffer()
                            {
                                // 読み込む容量
                                AudioBytes = (int)xaStream.Length,
                                // WaveStreamを渡す
                                Stream = xaStream,
                                // ループ設定
                                LoopCount = XAudio2.NoLoopRegion,
                                // ループ開始位置
                                LoopBegin = 0,
                                // 再生する長さ。0だと全部。
                                LoopLength = 0,
                                // 再生開始位置
                                PlayBegin = 0,
                                PlayLength = 0,
                                Flags = BufferFlags.EndOfStream,
                            };
                            xaStream.Close();

                            if (xaSource != null)
                            {
                                xaSource.SubmitSourceBuffer(xaBuffer, xaStream.DecodedPacketsInfo);
                                (xaSESourceList).Add(xaSource);
                                xaSource.Start();
                                new LateTask(() =>
                                {
                                    xaSource.DestroyVoice();
                                }, xaStream.Length + 1, "SEDestroy");
                            }
                        }
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Info(path + "がありません。" + ex.Message + "/" + ex.StackTrace,"PlaySoundSE");
            }
        }
        public static void PlaySoundSERPC(string soundFileName,byte playerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySoundSERPC, Hazel.SendOption.Reliable, -1);

                writer.Write(playerId);
                writer.Write(soundFileName);

                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            PlaySoundSE(soundFileName,playerId);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            string fileName = reader.ReadString();

            PlaySoundSE(fileName,playerId);

        }

        public static void PlaySoundVoice(byte[] buffer)
        {
            if (!InitFinished) return;
            // サウンドデバイスが無い場合処理しない
            if (xaDevice == null) { return; }

            MemoryStream httpStream = new (buffer);

            // WAVEファイルを読み込んで設定
            using (SoundStream xaStream = new (httpStream))
            {
                SourceVoice xaSource = new (xaDevice, xaStream.Format);
                AudioBuffer xaBuffer = new ()
                {
                    // 読み込む容量
                    AudioBytes = (int)xaStream.Length,
                    // WaveStreamを渡す
                    Stream = xaStream,
                    // ループ設定
                    LoopCount = XAudio2.NoLoopRegion,
                    // ループ開始位置
                    LoopBegin = 0,
                    // 再生する長さ。0だと全部。
                    LoopLength = 0,
                    // 再生開始位置
                    PlayBegin = 0,
                    PlayLength = 0,
                    Flags = BufferFlags.EndOfStream,
                };
                xaStream.Close();

                if (xaSource != null)
                {
                    xaSource.SubmitSourceBuffer(xaBuffer, xaStream.DecodedPacketsInfo);
                    (xaSESourceList).Add(xaSource);
                    xaSource.Start();
                    new LateTask(() =>
                    {
                        xaSource.DestroyVoice();
                    }, xaStream.Length, "SEDestroy");
                }
            }
        }

        //封印
        //public static void PlaySound(string soundFileName,int volumeParam)
        //{
        //    Logger.Info("soundfile:" + soundFileName,"BGM");

        //    switch (Path.GetExtension(soundFileName))
        //    {
        //        case ".wav":
        //            {
        //                // サウンドデバイスが無い場合処理しない
        //                if (xaDevice == null) { return; }

        //                // WAVEファイルを読み込んで設定
        //                using (SoundStream xaStream = new SoundStream(File.OpenRead(soundFileName)))
        //                {
        //                    SourceVoice xaSource = new SourceVoice(xaDevice, xaStream.Format);
        //                    AudioBuffer xaBuffer = new AudioBuffer()
        //                    {
        //                        // 読み込む容量
        //                        AudioBytes = (int)xaStream.Length,
        //                        // WaveStreamを渡す
        //                        Stream = xaStream,
        //                        // ループ設定
        //                        LoopCount = XAudio2.NoLoopRegion,
        //                        // ループ開始位置
        //                        LoopBegin = 0,
        //                        // 再生する長さ。0だと全部。
        //                        LoopLength = 0,
        //                        // 再生開始位置
        //                        PlayBegin = 0,
        //                        PlayLength = 0,
        //                        Flags = BufferFlags.EndOfStream,
        //                    };
        //                    xaStream.Close();

        //                    if (xaSource != null)
        //                    {
        //                        xaSource.SubmitSourceBuffer(xaBuffer, xaStream.DecodedPacketsInfo);
        //                        float volume = volumeParam / 100f;
        //                        Logger.Info("volume:" + volume, "BGM");
        //                        xaSource.SetVolume(volume);
        //                        (xaSESourceList).Add(xaSource);
        //                        xaSource.Start();
        //                    }
        //                }
        //            }
        //            break;
        //    }
        //}
        /// 効果音の停止
        public static void StopSound()
        {
            if (!InitFinished) return;
            CustomSoundsManager.StopSound();

            //OnPlay = false;

            //// サウンドデバイスが無い場合処理しない
            //if (xaDevice == null) { return; }

            //foreach (var n in xaSESourceList)
            //{
            //    n.Stop();
            //    n.Dispose();
            //}
            //xaSESourceList.Clear();
        }

        ///// <summary>
        ///// 解放処理
        ///// </summary>
        //public new void Dispose()
        //{
        //    foreach (var n in xaSESourceList) { n.Dispose(); }
        //    if (xaMaster != null) { xaMaster.Dispose(); }
        //    if (xaDevice != null) { xaDevice.Dispose(); }

        //    base.Dispose();
        //}

        //public static Dictionary<string, AudioClip> CachedAudioClips = new();
        //private static AudioClip loadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
        //{
        //    // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity® to export)
        //    try
        //    {
        //        if (CachedAudioClips.TryGetValue(path, out var audio)) return audio;
        //        Assembly assembly = Assembly.GetExecutingAssembly();
        //        Stream stream = assembly.GetManifestResourceStream(path);
        //        var byteAudio = new byte[stream.Length];
        //        _ = stream.Read(byteAudio, 0, (int)stream.Length);
        //        float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
        //        int offset;
        //        for (int i = 0; i < samples.Length; i++)
        //        {
        //            offset = i * 4;
        //            samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / int.MaxValue;
        //        }
        //        int channels = 2;
        //        int sampleRate = 48000;
        //        AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
        //        audioClip.SetData(samples, 0);
        //        audioClip.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        //        return CachedAudioClips[path] = audioClip;
        //    }
        //    catch
        //    {
        //        System.Console.WriteLine("Error loading AudioClip from resources: " + path);
        //    }
        //    return null;

        //    /* Usage example:
        //    AudioClip exampleClip = Helpers.loadAudioClipFromResources("TheOtherRoles.Resources.exampleClip.raw");
        //    if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        //    */
        //}
    }
}

//public class LoopStream : WaveStream
//{
//    WaveStream sourceStream;

//    public LoopStream(WaveStream sourceStream)
//    {
//        this.sourceStream = sourceStream;
//        this.EnableLooping = true;
//    }

//    public bool EnableLooping { get; set; }

//    public override WaveFormat WaveFormat
//    {
//        get { return sourceStream.WaveFormat; }
//    }

//    public override long Length
//    {
//        get { return sourceStream.Length; }
//    }

//    public override long Position
//    {
//        get { return sourceStream.Position; }
//        set { sourceStream.Position = value; }
//    }

//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        int totalBytesRead = 0;

//        while (totalBytesRead < count)
//        {
//            int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
//            if (bytesRead == 0)
//            {
//                if (sourceStream.Position == 0 || !EnableLooping)
//                {
//                    // something wrong with the source stream
//                    break;
//                }
//                // loop
//                sourceStream.Position = 0;
//            }
//            totalBytesRead += bytesRead;
//        }
//        return totalBytesRead;
//    }
//}

