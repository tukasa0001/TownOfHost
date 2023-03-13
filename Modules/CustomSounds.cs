using Hazel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TOHE.Modules;

public enum CustomSounds
{
    Boom,
    GunSet,
}
public class CustomSoundsManager
{
    public static void RPCPlay(byte playerId, CustomSounds sound)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            Play(sound);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RPCPlayAll(CustomSounds sound)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSoundAll, Hazel.SendOption.Reliable, -1);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Play(sound);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        CustomSounds Sound = (CustomSounds)reader.ReadByte();
        if (PlayerId == PlayerControl.LocalPlayer.PlayerId)
            Play(Sound);
    }
    public static void ReceiveRPCAll(MessageReader reader)
    {
        CustomSounds Sound = (CustomSounds)reader.ReadByte();
        Play(Sound);
    }
    public static void Play(CustomSounds sound)
    {
        if (soundEffects.TryGetValue($"TOHE.Resources.Sounds.{sound}.raw", out var clip))
        {
            SoundManager.Instance.PlaySound(clip, false);
            Logger.Msg($"播放声音：{sound}", "CustomSounds");
        }
        else
            Logger.Warn($"声音不存在：{sound}", "CustomSounds");
    }

    //以下全部代码来源：https://github.com/music-discussion/TownOfHost-TheOtherRoles/blob/main/Modules/SoundEffectsManager.cs
    private static Dictionary<string, AudioClip> soundEffects;
    public static void Load()
    {
        soundEffects = new Dictionary<string, AudioClip>();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();
        foreach (string resourceName in resourceNames)
        {
            if (resourceName.Contains("TOHE.Resources.Sounds.") && resourceName.Contains(".raw"))
            {
                Logger.Info($"Loading sound: {resourceName}", "CustomSoundsManager");
                soundEffects.Add(resourceName, LoadAudioClipFromResources(resourceName));
            }
        }
        foreach (var pair in soundEffects)
        {
            Logger.Info($"Pair.Key: {pair.Key}", "Pair.Key");
            Logger.Info($"Pair.Value: {pair.Value.ToString()}", "Pair.Value");
            if (pair.Value == null)
            {
                Logger.Info($"Pair.Value is equal to null.", "Pair.Value");
            }
        }
    }
    public static AudioClip Get(string path)
    {
        // Convenience: As as SoundEffects are stored in the same folder, allow using just the name as well
        if (!path.Contains(".")) path = "TOHE.Resources.Sounds." + path + ".raw";
        Logger.Info($"Song Path: {path}", "CustomSoundsManager");
        if (soundEffects.TryGetValue(path, out AudioClip returnValue))
        {
            if (returnValue == null)
            {
                Logger.Info($"Clip is Equal to null.", "CustomSoundsManager (get function)");
                soundEffects[path] = LoadAudioClipFromResources(path, "exampleClip");
                returnValue = soundEffects[path];
            }
            return returnValue;
        }
        else
        {
            Logger.Info($"Clip is Equal to null.", "CustomSoundsManager (get function)");
            return null;
        }
    }
    public static void Play(string path, bool loop = false, float volume = 0.8f)
    {
        AudioClip clipToPlay = Get(path);
        if (clipToPlay == null)
        {
            Logger.Info($"Clip is Equal to null.", "CustomSoundsManager (play function)");
            var newpath = path;
            if (!newpath.Contains(".")) newpath = "TOHE.Resources.Sounds." + newpath + ".raw";
            //clipToPlay = Helpers.loadAudioClipFromResources(newpath, "exampleClip");
        }
        // if (false) clipToPlay = get("exampleClip"); for april fools?
        Stop(path);
        SoundManager.Instance.PlaySound(clipToPlay, loop, volume);
    }
    public static void Stop(string path)
    {
        AudioClip clipToStop = Get(path);
        if (clipToStop == null)
        {
            Logger.Info($"Clip is Equal to null.", "CustomSoundsManager (stop function)");
            var newpath = path;
            if (!newpath.Contains(".")) newpath = "TOHE.Resources.Sounds." + newpath + ".raw";
            //clipToStop = Helpers.loadAudioClipFromResources(newpath, "exampleClip");
        }
        SoundManager.Instance.StopSound(clipToStop);
    }
    public static void StopAll()
    {
        if (soundEffects == null) return;
        foreach (var path in soundEffects.Keys) Stop(path);
    }
    public static AudioClip LoadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
    {
        // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity� to export)
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteAudio = new byte[stream.Length];
            _ = stream.Read(byteAudio, 0, (int)stream.Length);
            float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
            int offset;
            for (int i = 0; i < samples.Length; i++)
            {
                offset = i * 4;
                samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / int.MaxValue;
            }
            int channels = 2;
            int sampleRate = 48000;
            AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            Logger.Info("Got audio clip successfully.", "LoadAudioClipFromResources");
            return audioClip;
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString(), "Error Loading Audio Clip.");
            Logger.SendInGame($"Error loading audio clip \\/\n{ex}");
            System.Console.WriteLine("Error loading AudioClip from resources: " + path);
            return null;
        }
        /* Usage example:
        AudioClip exampleClip = Helpers.loadAudioClipFromResources("TOHE.Resources.exampleClip.raw");
        SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        */
    }
}
