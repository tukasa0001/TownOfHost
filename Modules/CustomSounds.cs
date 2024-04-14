using Hazel;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TownOfHostForE.Modules;

public static class CustomSoundsManager
{
    private static AudioSource audio = null;

    private static string CreateWavPath(string name)
    {
        return $"TownOfHost_ForE.Resources.Sounds.{name}.wav";
    }

    public static void PlaySoundManager()
    {
        //SoundManager.Instance.PlaySound(WavManager.loadAudioClipFromWavResources("TownOfHost_ForE.Resources.Sounds.SuisoSound.wav"),true);
        //SoundManager.Instance.PlaySound(WavManager.loadAudioClipFromWavResources(CreateWavPath("SuisoSound")),false);

    }

    public static void PlayBGMForWav(string name)
    {
        if(audio != null)audio.Stop();
        audio = SoundManager.Instance.PlaySound(WavManager.loadAudioClipFromWavResources(CreateWavPath(name)), true);
        audio.Play();
    }


    public static void StopBGM()
    {
        audio.Stop();
    }

    public static void RPCPlayCustomSound(this PlayerControl pc, string sound, bool force = false)
    {
        if (!force) if (!AmongUsClient.Instance.AmHost || !pc.IsModClient()) return;
        if (pc == null || PlayerControl.LocalPlayer.PlayerId == pc.PlayerId)
        {
            //Play(sound);
            BGMSettings.PlaySoundSERPC(sound);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, pc.GetClientId());
        writer.Write(sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RPCPlayCustomSoundAll(string sound)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, -1);
            writer.Write(sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            //Play(sound);
        }
        BGMSettings.PlaySoundSERPC(sound);
    }
    //public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString());
    public static void ReceiveRPC(MessageReader reader) => BGMSettings.PlaySoundSERPC(reader.ReadString());


    public static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/BepInEx/resources/";

    public static void PlayBGM(string sound,int valume)
    {
        if (!Constants.ShouldPlaySfx()) return;

        var path = BGMSettings.WAV_SETTING__PATH + sound + ".wav";

        if (!Directory.Exists(BGMSettings.WAV_SETTING__PATH)) Directory.CreateDirectory(BGMSettings.WAV_SETTING__PATH);
        DirectoryInfo folder = new(BGMSettings.WAV_SETTING__PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TownOfHostForE.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Warn($"ならせねぇ！：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }
        Logger.Msg($"鳴らすぜ！：{sound}", "CustomSounds");

        StartPlay(path);
    }

    ////改造前
    //public static void Play(string sound)
    //{
    //    if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value) return;
    //    var path = SOUNDS_PATH + sound + ".wav";
    //    if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
    //    DirectoryInfo folder = new(SOUNDS_PATH);
    //    if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
    //        folder.Attributes = FileAttributes.Hidden;
    //    if (!File.Exists(path))
    //    {
    //        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TownOfHostForE.Resources.Sounds." + sound + ".wav");
    //        if (stream == null)
    //        {
    //            Logger.Warn($"ならせねぇ！：{sound}", "CustomSounds");
    //            return;
    //        }
    //        var fs = File.Create(path);
    //        stream.CopyTo(fs);
    //        fs.Close();
    //    }
    //    Logger.Msg($"鳴らすぜ！：{sound}", "CustomSounds");
    //    StartPlay(path);
    //}

    [DllImport("winmm.dll")]
    public static extern bool PlaySound(string Filename, int Mod, int Flags);

    [DllImport("winmm.dll")]
    public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

    [DllImport("winmm.dll")]
    public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    public static void StartPlay(string path) => PlaySound(@$"{path}", 0, 9); //第3个形参，把1换为9，连续播放
    public static void StopSound() => PlaySound(null, 0,  0x0040); 
}
