using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace TownOfHostForE.Modules;

//SP thx SNR!!
public static class ContentManager
{
    private readonly static Dictionary<string, DownloadedContent> Contents = new();
    //private readonly static string BasePath = $@"{Path.GetDirectoryName(Application.dataPath)}\SuperNewRoles\DownloadContent\";
    //private readonly static DirectoryInfo directory = new(BasePath);
    //private const string ContentURL = "https://raw.githubusercontent.com/SuperNewRoles/SupernewRolesData/main/Contents";
    public static MD5 MD5Hash = MD5.Create();

    public static Dictionary<string, AudioClip> CachedAudioClips = new();
    //public static void Load()
    //{
    //    //ディレクトリがないなら作る
    //    if (!directory.Exists)
    //        directory.Create();
    //}
    private static Stream GetStream(DownloadedContent content)
    {
        //暗号化されているファイルの場合は復号化して返す
        if (content.Encrypted)
        {
            using (Aes aes = Aes.Create())
            {
                // 復号器を用意
                using (var decryptor = aes.CreateDecryptor(GK(content.WebPath), aes.IV))
                {
                    // 入力ファイルストリーム
                    using (FileStream in_fs = new FileStream(content.file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        // 復号して一定サイズずつ読み出し、出力ファイルストリームに書き出す
                        using (CryptoStream cs = new CryptoStream(in_fs, decryptor, CryptoStreamMode.Read))
                        {
                            // 先頭16バイトは不要なのでまず復号して破棄
                            byte[] dummy = new byte[16];
                            cs.Read(dummy, 0, 16);
                            //そのままではoffsetなどを指定しないといけなく、面倒くさいのでMemoryStreamで処理する
                            MemoryStream out_stream = new();
                            // 一定量ずつ処理していく
                            byte[] buffer = new byte[8192];
                            int len = 0;
                            while ((len = cs.Read(buffer, 0, 8192)) > 0)
                            {
                                out_stream.Write(buffer, 0, len);
                            }
                            return out_stream;
                        }
                    }
                }
            }
        }
        //暗号化してないなら普通にFileStreamで返す
        return new FileStream(content.file.FullName, FileMode.Open, FileAccess.Read);
    }
    /// <summary>
    /// コンテンツのパスを指定して特定の型で返す。対応させたい型があったらswitchに追記。
    /// </summary>
    /// <typeparam name="T">返す型</typeparam>
    /// <param name="path">サーバー側に記入したパス(WebPath)</param>
    /// <param name="defaultvalue">エラーが発生したときなどに返される値。デフォルトはdefault</param>
    /// <returns></returns>
    public static AudioClip GetContent(string path, AudioClip defaultvalue = default)
    {
        //存在チェック あればそれを返す
        if (Contents.TryGetValue(path, out DownloadedContent content)) return (AudioClip)content.Value;

        //ない
        //content.Valueにキャッシュする
        string[] pathes = path.Split(".");
        //後ろから-1はつまり最後を見てるね！
        switch (pathes[pathes.Length - 1])
        {
            case "wav":
                AudioClip clip = WavManager.ToAudioClip(GetStream(content));
                content.Value = clip;
                break;
            default:
                Logger.Info($"このタイプは対応していません。対応してください。パス：{path}、拡張子:{path[path.Length - 1]}", "wav");
                return defaultvalue;
        }
        //if (content.Value == null || content.Value is not T)
        //{
        //    Logger.Info("正常に取得できませんでした。", "wav");
        //    return defaultvalue;
        //}
        return (AudioClip)content.Value;
    }
    //public static T GetContent<T>(string path, T defaultvalue = default)
    //{
    //    //存在チェック
    //    if (!Contents.TryGetValue(path, out DownloadedContent content))
    //    {
    //        Logger.Info("一覧からの取得に失敗しました。", "wav");
    //        return defaultvalue;
    //    }
    //    //content.Valueにキャッシュする
    //    if (content.Value == null || content.Value is not T)
    //    {
    //        //ファイルを削除された場合のエラー対策
    //        if (!content.file.Exists)
    //            return defaultvalue;
    //        string[] pathes = path.Split(".");
    //        switch (pathes[pathes.Length - 1])
    //        {
    //            case "wav":
    //                AudioClip clip = WavManager.ToAudioClip(GetStream(content));
    //                content.Value = clip;
    //                break;
    //            default:
    //                Logger.Info($"このタイプは対応していません。対応してください。パス：{path}、拡張子:{path[path.Length - 1]}", "wav");
    //                return defaultvalue;
    //        }
    //    }
    //    if (content.Value == null || content.Value is not T)
    //    {
    //        Logger.Info("正常に取得できませんでした。", "wav");
    //        return defaultvalue;
    //    }
    //    return (T)content.Value;
    //}
    //public static AudioClip loadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
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
    //static bool Downloading = false;
    //[HarmonyPatch(typeof(AmongUsClient),nameof(AmongUsClient.Awake))]
    //class AmongUsClientAwakePatch
    //{
    //    public static void Postfix(AmongUsClient __instance)
    //    {
    //        //処理がもう走った場合は処理しない
    //        if (Downloading)
    //            return;
    //        Downloading = true;
    //        __instance.StartCoroutine(Download().WrapToIl2Cpp());
    //    }
    //}
    //public static IEnumerator Download()
    //{
    //    //コンテンツ一覧を取得する
    //    var request = UnityWebRequest.Get($"{ContentURL}/DownloadData.json");
    //    yield return request.SendWebRequest();
    //    if (request.isNetworkError || request.isHttpError)
    //    {
    //        Logger.Info("ContentItiranError:一覧の取得に失敗しました。", "wav");
    //        yield break;
    //    }
    //    //Jsonを処理する
    //    var json = JObject.Parse(request.downloadHandler.text);
    //    for (var ct = json["Contents"].First; ct != null; ct = ct.Next)
    //    {
    //        DownloadedContent dc = new()
    //        {
    //            WebPath = ct["path"]?.ToString(),
    //            hash = ct["hash"]?.ToString(),
    //            Encrypted = (bool)ct["encrypted"]
    //        };
    //        //万が一すでにあった場合を対策する
    //        if (!Contents.TryAdd(dc.WebPath, dc))
    //        {
    //            Logger.Info(dc.WebPath+"の追加に失敗しました。", "wav");
    //        }
    //    }
    //    foreach (DownloadedContent content in Contents.Values)
    //    {
    //        //FileInfoを使いまわし
    //        content.file = new(BasePath + content.WebPath);
    //        if (content.file.Exists)
    //        {
    //            FileStream stream = content.file.Open(FileMode.Open);
    //            //ファイルが違う、やファイルの改ざんをチェック
    //            content.Downloaded = content.hash == BitConverter.ToString(MD5Hash.ComputeHash(stream)).Replace("-","");
    //            Logger.Info($"ファイル：{content.WebPath}が存在しました。ハッシュチェック：{content.Downloaded}", "wav");
    //            //ちゃんと閉じる
    //            stream.Close();
    //            stream.Dispose();
    //        }
    //    }
    //    foreach (DownloadedContent content in Contents.Values)
    //    {
    //        //ダウンロード済みじゃない場合はダウンロード処理
    //        if (!content.Downloaded)
    //        {
    //            request = UnityWebRequest.Get($"{ContentURL}/{content.WebPath}");
    //            yield return request.SendWebRequest();
    //            if (request.isNetworkError || request.isHttpError)
    //            {
    //                //失敗したのでフリーズ対策に1フレーム待機して次の処理へ移る
    //                Logger.Info("Contentダウンロードに失敗しました。:"+content.WebPath, "wav");
    //                yield return null;
    //                continue;
    //            }
    //            //とりあえずバイトで書き込んどく
    //            BinaryWriter writer = new(content.file.Open(FileMode.OpenOrCreate));
    //            writer.Write(request.downloadHandler.data);
    //            writer.Close();
    //            //ダウンロードしたで！
    //            content.Downloaded = true;
    //        }
    //    }
    //}
    public static byte[] GK(string k)
    {
        int l = 32;var r = new System.Random(k.GetHashCode());
        var s = new StringBuilder();
        for (int i = 0; i < l; i++)
        {
            char c = (char)r.Next(97, 122);
            s.Append(c);
        }
        return Encoding.ASCII.GetBytes(s.ToString());
    }
    //特定のファイルのMD5ハッシュを求める。UEからのみ使う。
    //public static void ToHashFile(string Ex)
    //{
    //    FileStream o_stream = new FileStream(BasePath + "hashcheck." + Ex, FileMode.Open, FileAccess.Read);
    //    Logger.Info("HASH:" + BitConverter.ToString(MD5Hash.ComputeHash(o_stream)).Replace("-", ""), "wav");
    //    o_stream.Close();
    //}
    ////暗号化をする。コードでは使わない。
    //public static void Encrypt(string WebPath, string Ex)
    //{
    //    using (Aes aes = Aes.Create())
    //    {
    //        // Encryptorを用意
    //        using (ICryptoTransform encryptor = aes.CreateEncryptor(GK(WebPath), aes.IV))
    //        {
    //            // 入力ファイルストリーム
    //            using (FileStream in_stream = new FileStream(BasePath + "base." + Ex, FileMode.Open, FileAccess.Read))
    //            {
    //                // 暗号化したデータを書き出すための出力ファイルストリーム
    //                string out_filepath = BasePath + "ato." + Ex;
    //                using (FileStream out_fs = new FileStream(out_filepath, FileMode.Create, FileAccess.Write))
    //                {
    //                    // 一定サイズずつ暗号化して出力ファイルストリームに書き出す
    //                    using (CryptoStream cs = new CryptoStream(out_fs, encryptor, CryptoStreamMode.Write))
    //                    {
    //                        // 先頭16バイトは適当な値(いまはゼロ)で埋める
    //                        byte[] dummy = new byte[16];
    //                        cs.Write(dummy, 0, 16);

    //                        // 一定量ずつ暗号化して書き込み
    //                        byte[] buffer = new byte[8192];
    //                        int len = 0;
    //                        while ((len = in_stream.Read(buffer, 0, 8192)) > 0)
    //                        {
    //                            cs.Write(buffer, 0, len);
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    FileStream o_stream = new FileStream(BasePath + "ato." + Ex, FileMode.Open, FileAccess.Read);
    //    Logger.Info("HASH:" + BitConverter.ToString(MD5Hash.ComputeHash(o_stream)).Replace("-", ""), "wav");
    //    o_stream.Close();
    //}
}
public class DownloadedContent
{
    public bool Downloaded = false;
    public string hash;
    public bool Encrypted;
    public string path;
    public string WebPath;
    public object Value;
    public FileInfo file;
}