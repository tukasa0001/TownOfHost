using TOHTOR.Gamemodes;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

// I HAVE NO CLUE HOW TOH HNS WORKS LOL
public class Troll : NotImplemented
{



    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}