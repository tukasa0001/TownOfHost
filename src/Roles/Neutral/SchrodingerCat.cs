using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class SchrodingerCat: NotImplemented
{
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        return new SmartOptionBuilder();
    }

}