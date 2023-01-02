/*namespace TownOfHost;

public class OLDSETUPSETTINGS
{
    case CustomRoles.Painter:
                    options.SetVision(player, Options.PaintersHaveImpVision.GetBool());
                    break;
                case CustomRoles.Marksman:
                    options.KillDistance = Main.MarksmanKills;
                    options.SetVision(player, true);
                    break;
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                // case CustomRoles.ShapeMaster:
                //     options.RoleOptions.ShapeshifterCooldown = 0.1f;
                //     options.RoleOptions.ShapeshifterLeaveSkin = false;
                //     options.RoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                //     break;
                case CustomRoles.Bastion:
                    engineerOptions.EngineerCooldown = 25;
                    engineerOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Transporter:
                    engineerOptions.EngineerInVentMaxTime = 1;
                    if (Main.TransportsLeft != 0)
                        engineerOptions.EngineerCooldown = Options.TransportCooldown.GetFloat();
                    else
                        engineerOptions.EngineerCooldown = 99999;
                    break;
                case CustomRoles.Warlock:
                    shapeshifterOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyGameOptions(options);
                    break;
                case CustomRoles.BountyHunter:
                    BountyHunter.ApplyGameOptions(options);
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.Investigator:
                case CustomRoles.Janitor:
                case CustomRoles.Arsonist:
                case CustomRoles.Amnesiac:
                case CustomRoles.Crusader:
                case CustomRoles.Escort:
                    options.SetVision(player, false);
                    break;
                case CustomRoles.PlagueBearer:
                    options.SetVision(player, false);
                    break;
                case CustomRoles.CorruptedSheriff:
                case CustomRoles.Pestilence:
                    options.SetVision(player, true);
                    break;
                case CustomRoles.Medium:
                    engineerOptions.EngineerCooldown = Options.MediumCooldown.GetFloat();
                    engineerOptions.EngineerInVentMaxTime = 0.5f;
                    break;
                case CustomRoles.BloodKnight:
                case CustomRoles.EgoSchrodingerCat:
                    options.SetVision(player, true);
                    break;
                case CustomRoles.Doctor:
                    scientistOptions.ScientistCooldown = 0f;
                    scientistOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                    break;
                case CustomRoles.Camouflager:
                    shapeshifterOptions.ShapeshifterCooldown = Camouflager.CamouflagerCamouflageCoolDown.GetFloat();
                    shapeshifterOptions.ShapeshifterDuration = Camouflager.CamouflagerCamouflageDuration.GetFloat();
                    break;
                case CustomRoles.Juggernaut:
                    options.SetVision(player, true);
                    if (Options.JuggerCanVent.GetBool())
                        goto InfinityVent;
                    break;
                case CustomRoles.Freezer:
                    shapeshifterOptions.ShapeshifterCooldown = Options.FreezerCooldown.GetFloat();
                    shapeshifterOptions.ShapeshifterDuration = Options.FreezerDuration.GetFloat();
                    break;
                case CustomRoles.Disperser:
                    shapeshifterOptions.ShapeshifterCooldown = Options.DisperseCooldown.GetFloat();
                    shapeshifterOptions.ShapeshifterDuration = 1;
                    break;
                case CustomRoles.Vulture:
                    options.SetVision(player, Options.VultureHasImpostorVision.GetBool());
                    if (Options.VultureCanVent.GetBool())
                        goto InfinityVent;
                    break;
                case CustomRoles.Mayor:
                    engineerOptions.EngineerCooldown =
                        Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt()
                        ? options.EmergencyCooldown
                        : 300f;
                    engineerOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Veteran:
                    //5 lines of code calculating the next Vet CD.
                    if (Main.IsRoundOne)
                    {
                        engineerOptions.EngineerCooldown = 10f;
                        Main.IsRoundOne = false;
                    }
                    else if (!Main.VettedThisRound)
                        engineerOptions.EngineerCooldown = Options.VetCD.GetFloat();
                    else
                        engineerOptions.EngineerCooldown = Options.VetCD.GetFloat() + Options.VetDuration.GetFloat();
                    engineerOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Survivor:
                    engineerOptions.EngineerInVentMaxTime = 1;
                    foreach (var ar in Main.SurvivorStuff)
                    {
                        if (ar.Key != player.PlayerId) break;
                        // now we set it to true
                        var stuff = Main.SurvivorStuff[player.PlayerId];
                        if (stuff.Item1 != Options.NumOfVests.GetInt())
                        {
                            if (stuff.Item5)
                            {
                                engineerOptions.EngineerCooldown = 10;
                                stuff.Item5 = false;
                                Main.SurvivorStuff[player.PlayerId] = stuff;
                            }
                            else if (!stuff.Item4)
                                engineerOptions.EngineerCooldown = Options.VestCD.GetFloat();
                            else
                                engineerOptions.EngineerCooldown = Options.VestCD.GetFloat() + Options.VestDuration.GetFloat();
                        }
                        else
                        {
                            engineerOptions.EngineerCooldown = 999;
                        }
                    }
                    break;
                case CustomRoles.Opportunist:
                    engineerOptions.EngineerInVentMaxTime = 1;
                    engineerOptions.EngineerCooldown = 999999;
                    break;
                case CustomRoles.GuardianAngelTOU:
                    if (Main.IsRoundOneGA)
                    {
                        engineerOptions.EngineerCooldown = 10f;
                        Main.IsRoundOneGA = false;
                    }
                    else if (!Main.ProtectedThisRound)
                        engineerOptions.EngineerCooldown = Options.GuardCD.GetFloat();
                    else
                        engineerOptions.EngineerCooldown = Options.GuardCD.GetFloat() + Options.GuardDur.GetFloat();
                    engineerOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Jester:
                    player.AdjustLighting();
                    options.SetVision(player, Options.JesterHasImpostorVision.GetBool());
                    if (Utils.IsActive(SystemTypes.Electrical) && Options.JesterHasImpostorVision.GetBool())
                        options.CrewLightMod *= 5;
                    if (Options.JesterCanVent.GetBool())
                        goto InfinityVent;
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(options, player.PlayerId);
                    break;
                case CustomRoles.Ninja:
                    shapeshifterOptions.ShapeshifterCooldown = 0.1f;
                    shapeshifterOptions.ShapeshifterDuration = 0f;
                    break;
                case CustomRoles.Grenadier:
                    shapeshifterOptions.ShapeshifterCooldown = Options.FlashCooldown.GetFloat();
                    shapeshifterOptions.ShapeshifterDuration = Options.FlashDuration.GetFloat();
                    break;
                case CustomRoles.Hitman:
                    options.SetVision(player, Options.HitmanHasImpVision.GetBool());
                    break;
                case CustomRoles.Werewolf:
                    options.SetVision(player, Main.IsRampaged);
                    goto InfinityVent;
                //break;
                case CustomRoles.TheGlitch:
                    options.SetVision(player, true);
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.Sidekick:
                case CustomRoles.JSchrodingerCat:
                    options.SetVision(player, Options.JackalHasImpostorVision.GetBool());
                    break;


                InfinityVent:
                    engineerOptions.EngineerCooldown = 0;
                    engineerOptions.EngineerInVentMaxTime = 0;
                    break;
}*/