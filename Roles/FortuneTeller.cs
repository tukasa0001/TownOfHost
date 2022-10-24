namespace TownOfHost
{
    public static class FortuneTeller
    {
        private static readonly int Id = 21100;
 
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        }
    }
}