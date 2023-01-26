

namespace TownOfHost
{
    public static class GuessManager
    {

        public static bool isGuesser(byte playerId) =>
            Utils.GetPlayerById(playerId).GetCustomRole() == CustomRoles.NiceGuesser || Utils.GetPlayerById(playerId).GetCustomRole() == CustomRoles.EvilGuesser;

        public static bool isRealRole(byte playerId, CustomRoles type) =>
            Utils.GetPlayerById(playerId).GetCustomRole() == type;

        public static bool isGood(byte playerId) =>
            Utils.GetPlayerById(playerId).GetCustomRole() == CustomRoles.NiceGuesser && isGuesser(playerId);

        public static PlayerControl GetPlayerByNum(int i) {
            int num = 0;
            foreach (var p in Main.AllAlivePlayerControls) {
                num ++;
                if (num == i) {
                    return p;
                }
            }

            return null;
        }

        public static string getFormatString() {
            string str = "";
            int i = 0;
            foreach (var player in Main.AllAlivePlayerControls)
            {
                i++;
                str = str + player.GetRealName() + " - " + i + "\n";
            }

            return str;
        }
        
    }
}
