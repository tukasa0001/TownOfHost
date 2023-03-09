using VentLib.Options.Game;

namespace TOHTOR.Extensions;

public static class GameOptionBuilderExtensions
{
    public static GameOptionBuilder AddOnOffValues(this GameOptionBuilder builder, bool defaultOn = true)
    {
        return builder.Value(val =>
                    val.Text(defaultOn ? "ON" : "OFF")
                        .Value(defaultOn)
                        .Color(defaultOn ? UnityEngine.Color.cyan : UnityEngine.Color.red)
                        .Build())
                .Value(val =>
                    val.Text(defaultOn ? "OFF" : "ON")
                        .Value(!defaultOn)
                        .Color(defaultOn ? UnityEngine.Color.red : UnityEngine.Color.cyan)
                        .Build());
    }
}