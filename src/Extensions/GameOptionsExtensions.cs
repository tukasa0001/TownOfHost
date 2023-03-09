#nullable enable
using System;
using AmongUs.GameOptions;

namespace TOHTOR.Extensions;

public static class GameOptionsExtensions
{
    [Obsolete("Looking into new TOH code to figure out a better way to do this")]
    public static NormalGameOptionsV07? AsNormalOptions(this IGameOptions options)
    {
        return options.Cast<NormalGameOptionsV07>();
    }

    public static Byte[] ToBytes(this IGameOptions gameOptions)
    {
        return GameOptionsManager.Instance.gameOptionsFactory.ToBytes(gameOptions);
    }

    public static IGameOptions DeepCopy(this IGameOptions opt)
    {
        return GameOptionsManager.Instance.gameOptionsFactory.FromBytes(opt.ToBytes());
    }
}