using VentLib.Options;

namespace TownOfHost.Extensions;

public static class OptionBuilderExtensions
{
    public static OptionBuilder IsHeader(this OptionBuilder builder, bool isHeader)
    {
        builder.Attribute("IsHeader", isHeader);
        return builder;
    }
}