using System;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }

        public override IGameOptions BuildGameOptions()
        {
            var opt = BasedGameOptions;

            return opt;
        }
    }
}