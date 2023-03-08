using Hazel;

namespace TOHE.Modules;

public enum CustomSounds
{
    Boom,
    GunShoot,
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
        Logger.Msg($"播放声音：{sound}", "CustomSounds");

        //未完成

        //AudioClip audio = Resources.Load<AudioClip>($"{sound}.wav");
        //SoundManager.Instance.PlaySound(audio, false);
    }
}
