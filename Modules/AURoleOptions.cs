using AmongUs.GameOptions;

namespace TownOfHost
{
    public static class AURoleOptions
    {
        private static NormalGameOptionsV07 Normal => Main.NormalOptions;
        public static float ScientistCooldown
        {
            get => Normal.GetFloat(FloatOptionNames.ScientistCooldown);
            set => Normal.SetFloat(FloatOptionNames.ScientistCooldown, value);
        }
        public static float ScientistBatteryCharge
        {
            get => Normal.GetFloat(FloatOptionNames.ScientistBatteryCharge);
            set => Normal.SetFloat(FloatOptionNames.ScientistBatteryCharge, value);
        }
        public static float EngineerCooldown
        {
            get => Normal.GetFloat(FloatOptionNames.EngineerCooldown);
            set => Normal.SetFloat(FloatOptionNames.EngineerCooldown, value);
        }
        public static float EngineerInVentMaxTime
        {
            get => Normal.GetFloat(FloatOptionNames.EngineerInVentMaxTime);
            set => Normal.SetFloat(FloatOptionNames.EngineerInVentMaxTime, value);
        }
        public static float GuardianAngelCooldown
        {
            get => Normal.GetFloat(FloatOptionNames.GuardianAngelCooldown);
            set => Normal.SetFloat(FloatOptionNames.GuardianAngelCooldown, value);
        }
        public static float ProtectionDurationSeconds
        {
            get => Normal.GetFloat(FloatOptionNames.ProtectionDurationSeconds);
            set => Normal.SetFloat(FloatOptionNames.ProtectionDurationSeconds, value);
        }
        public static bool ImpostorsCanSeeProtect
        {
            get => Normal.GetBool(BoolOptionNames.ImpostorsCanSeeProtect);
            set => Normal.SetBool(BoolOptionNames.ImpostorsCanSeeProtect, value);
        }
        public static float ShapeshifterDuration
        {
            get => Normal.GetFloat(FloatOptionNames.ShapeshifterDuration);
            set => Normal.SetFloat(FloatOptionNames.ShapeshifterDuration, value);
        }
        public static float ShapeshifterCooldown
        {
            get => Normal.GetFloat(FloatOptionNames.ShapeshifterCooldown);
            set => Normal.SetFloat(FloatOptionNames.ShapeshifterCooldown, value);
        }
        public static bool ShapeshifterLeaveSkin
        {
            get => Normal.GetBool(BoolOptionNames.ShapeshifterLeaveSkin);
            set => Normal.SetBool(BoolOptionNames.ShapeshifterLeaveSkin, value);
        }
    }
}