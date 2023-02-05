using TownOfHost.Gamemodes;
using VentLib.Options;

namespace TownOfHost.Roles;

// I HAVE NO CLUE HOW TOH HNS WORKS LOL
public class Troll : NotImplemented
{



    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}