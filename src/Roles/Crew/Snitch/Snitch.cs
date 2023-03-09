using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

public class Snitch: CustomRole
{
    public bool snitchCanTrackNK;
    public bool snitchCanTrackCoven;
    public bool enableTargetArrow;
    public bool arrowIsColored;
    public bool evilCanTrackSnitch = true;
    public int snitchWarningTasks = 2;

    //private Dictionary<byte, NameUI[]> arrowComponents = new();
    private int totalTasks = -1;
    private int tasksComplete;

    /*DynamicName dynamicName = player.GetDynamicName();

        dynamicName.CreateMainUI(out NameUI taskUI);
        taskUI.SetColor(Color.yellow);
        taskUI.SetFormat("({0})");
        taskUI.SetValueSupplier(() => $"{tasksComplete}/{totalTasks}");*/
    protected override void Setup(PlayerControl player) => totalTasks = player.Data?.Tasks?.Count ?? -1;

    [RoleAction(RoleActionType.TaskComplete)]
    public void SnitchFinishTask(PlayerControl player)
    {
        if (player.PlayerId != MyPlayer.PlayerId) return;
        tasksComplete++;
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    public void SnitchFixedUpdate()
    {
        DynamicName dynamicName = MyPlayer.GetDynamicName();
        totalTasks = totalTasks >= 0 ? totalTasks : MyPlayer.Data?.Tasks?.Count ?? -1;
        if (totalTasks - tasksComplete > snitchWarningTasks)
        {
            dynamicName.Render();
            return;
        }

        /*if (MyPlayer.Data.IsDead && arrowComponents.Count != 0) {
            foreach (byte playerId in arrowComponents.Keys)
            {
                arrowComponents[playerId][0]?.Destroy();
                arrowComponents[playerId][1]?.Destroy();
                arrowComponents.Remove(playerId);
            }
        }*/

        foreach (PlayerControl player in Game.GetAllPlayers())
        {
            /*CustomRole role = player.GetCustomRole();
            bool canTrack = role.VirtualRole is RoleTypes.Impostor or RoleTypes.Shapeshifter;
            canTrack = canTrack && ((role.IsNeutralKilling() && snitchCanTrackNK) || !role.IsNeutralKilling());
            canTrack = canTrack || (role.Factions.Contains(Faction.Coven) && snitchCanTrackCoven);
            if (!canTrack) continue;
            if (!arrowComponents.ContainsKey(player.PlayerId))
                arrowComponents[player.PlayerId] = new NameUI[2];
            // Item1 is for snitch tracking enemy // Item 2 is for enemy tracking snitch
            var components = arrowComponents[player.PlayerId];

            if (player.Data.IsDead) {
                components[0]?.Destroy();
                components[1]?.Destroy();
                continue;
            }
            if (totalTasks - tasksComplete == 0 && components[0] == null) {
                dynamicName.CreateSuffix(out NameUI playerSuffix);
                components[0] = playerSuffix;
                components[0].SetFormat(" {0}");
                components[0].SetValueSupplier(() =>
                    Helpers.ColorString(
                        arrowIsColored ? role.RoleColor : Color.white ,
                        RoleUtils.CalculateArrow(MyPlayer, player).ToString()
                    ));
            }

            DynamicName evilDynamicName = player.GetDynamicName();
            evilDynamicName.Render();
            if (components[1] != null) continue;
            dynamicName.CreateSuffix(out NameUI suffix);
            components[1] = suffix;
            components[1].SetValueSupplier(() =>
                Helpers.ColorString(
                    arrowIsColored ? RoleColor : Color.white ,
                    RoleUtils.CalculateArrow(player, MyPlayer).ToString()
                ));*/
        }
        dynamicName.Render();
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#b8fb4f");

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(s => s.Name("Enable Arrow")
                .Bind(v => enableTargetArrow = (bool)v)
                .AddOnOffValues()
                .ShowSubOptionPredicate(o => (bool)o)
                .SubOption(arrow => arrow.Name("Colored Arrow")
                    .Bind(v => arrowIsColored = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .Build())
            .SubOption(s => s.Name("Snitch Can Track Neutral Killing")
                .Bind(v => snitchCanTrackNK = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(s => s.Name("Snitch Can Track Coven")
                .Bind(v => snitchCanTrackCoven = (bool)v)
                .AddOnOffValues()
                .Build());
}