using TownOfHost.Gamemodes;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

// I HAVE NO CLUE HOW TOH HNS WORKS LOL
public class Troll : NotImplemented
{



    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}