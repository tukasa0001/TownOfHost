using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;

namespace TownOfHost.ReduxOptions;

public static class DesyncOptions
{
    public static IGameOptions OriginalHostOptions {
        set => originalHostOptions = value;
        get => originalHostOptions ?? GameOptionsManager.Instance?.CurrentGameOptions;
    }

    private static IGameOptions originalHostOptions;

    public static void SyncToAll(IGameOptions options) => Game.GetAllPlayers().Do(p => SyncToPlayer(options, p));

    public static void SyncToPlayer(IGameOptions options, PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
        {
            SyncToClient(options, AmongUsClient.Instance.GetClientFromCharacter(player).Id);
            return;
        }

        GameOptionsManager.Instance.currentGameOptions = options;
    }

    public static void SyncToClient(IGameOptions options, int clientId)
    {
        GameOptionsFactory optionsFactory = GameOptionsManager.Instance.gameOptionsFactory;

        MessageWriter messageWriter = MessageWriter.Get(SendOption.None); // Start message writer
        messageWriter.StartMessage(6); // Initial control-flow path for packet receival (Line 1352 InnerNetClient.cs) || This can be changed to "5" and remove line 20 to sync options to everybody
        messageWriter.Write(AmongUsClient.Instance.GameId); // Write 4 byte GameId
        messageWriter.WritePacked(clientId); // Target player ID

        messageWriter.StartMessage(1); // Second control-flow path specifically for changing game options
        messageWriter.WritePacked(GetManagerClientId()); // Packed ID for game manager

        messageWriter.StartMessage(4); // Index of logic component in GameManager (4 is current LogicOptionsNormal)
        optionsFactory.ToNetworkMessageWithSize(messageWriter, options); // Write options to message

        messageWriter.EndMessage(); // Finish message 1
        messageWriter.EndMessage(); // Finish message 2
        messageWriter.EndMessage(); // Finish message 3
        AmongUsClient.Instance.SendOrDisconnect(messageWriter); // Wrap up send
        messageWriter.Recycle(); // Recycle
    }

    public static int GetTargetedClientId(string name)
    {
        int clientId = -1;
        var allClients = AmongUsClient.Instance.allObjectsFast;
        var allClientIds = allClients.Keys;

        foreach (uint id in allClientIds)
            if (clientId == -1 && allClients[id].name.Contains(name))
                clientId = (int)id;
        return clientId;
    }

    // This method is used to find the "GameManager" client which is now needed for synchronizing options
    public static int GetManagerClientId() => GetTargetedClientId("Manager");

    public static void SendModifiedOptions(IEnumerable<GameOptionOverride> overrides, PlayerControl player)
    {
        IGameOptions clonedOptions = OriginalHostOptions.DeepCopy();
        overrides.Where(o => o != null).Do(optionOverride => optionOverride.ApplyTo(clonedOptions));
        SyncToPlayer(clonedOptions, player);
    }

}