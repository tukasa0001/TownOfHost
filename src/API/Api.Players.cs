using System.Collections.Generic;

namespace TOHTOR.API;

public partial class Api
{
    public class Players
    {
        public static IEnumerable<PlayerControl> GetAllPlayers() => Game.GetAllPlayers();


    }


}