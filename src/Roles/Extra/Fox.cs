using TownOfHost.Gamemodes;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Fox : NotImplemented
{

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}