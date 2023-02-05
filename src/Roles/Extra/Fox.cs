using TownOfHost.Gamemodes;
using VentLib.Options;

namespace TownOfHost.Roles;

public class Fox : NotImplemented
{

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}