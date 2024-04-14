using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using static TownOfHostForE.Utils;
using static UnityEngine.GraphicsBuffer;
using MS.Internal.Xml.XPath;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Metaton : RoleBase, IKiller
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Metaton),
            player => new Metaton(player),
            CustomRoles.Metaton,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            21700,
            SetupOptionItem,
            "メタトン",
            "#c71585",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Metaton(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CurrentKillCooldown = KillCooldown.GetFloat();
        fillingOfTime = OptionFillingOfTime.GetInt();
        YTCommentFIllingOfTime = OptionYTCommentFillingOfTime.GetInt();
        fillingOfMax = OptionFillingMax.GetInt();
        MetatonBGM = OptionMetatonBGM.GetBool();
        UpdateTime = 0;
        metatonCount = 0;
        ShotLimit = 0;
    }

    private static OptionItem KillCooldown;
    private static OptionItem OptionFillingOfTime;
    private static OptionItem OptionYTCommentFillingOfTime;
    private static OptionItem OptionFillingMax;
    private static OptionItem OptionMetatonBGM;


    public static int getYTCommentCount = 0;

    enum OptionName
    {
        FillingOfTime,
        YTCommentFillingOfTime,
        FillingMax,
        MetatonBGM
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    private static float UpdateTime;
    private int metatonCount = 0;

    private int fillingOfTime = 0;
    private int YTCommentFIllingOfTime = 0;
    private int fillingOfMax = 0;
    public static bool MetatonBGM = false;

    public static bool SetSPBGM = false;

    float colorchange = 0;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionFillingOfTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.FillingOfTime, new(0, 10, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionYTCommentFillingOfTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.YTCommentFillingOfTime, new(0, 10, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionFillingMax = IntegerOptionItem.Create(RoleInfo, 13, OptionName.FillingMax, new(20, 600, 20), 500, false)
            .SetValueFormat(OptionFormat.Times);
        OptionMetatonBGM = BooleanOptionItem.Create(RoleInfo, 14, OptionName.MetatonBGM, true, false);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SyncMetaton);
        sender.Writer.Write(ShotLimit);
        sender.Writer.Write(metatonCount);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncMetaton) return;

        ShotLimit = reader.ReadInt32();
        metatonCount = reader.ReadInt32();
        if (ShotLimit > 0)
        {
            if (MetatonBGM && Player == PlayerControl.LocalPlayer)
            {
                BGMSettings.spBGM = true;
                BGMSettings.SetTaskBGM();
            }
        }
        else
        {
            var newOutfit = Camouflage.PlayerSkins[Player.PlayerId];
            Player.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
            if (MetatonBGM && BGMSettings.spBGM)
            {
                BGMSettings.spBGM = false;
            }

        }
    }
    public float CalculateKillCooldown() => CurrentKillCooldown;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (CanUseKillButton())
        {
            if (Is(info.AttemptKiller) && !info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;
                ShotLimit--;
                metatonCount = 0;

                var newOutfit = Camouflage.PlayerSkins[killer.PlayerId];
                killer.SetSkin(newOutfit.SkinId, newOutfit.ColorId);

                if (MetatonBGM && BGMSettings.spBGM)
                {
                    BGMSettings.spBGM = false;
                }

                SendRPC();
                Utils.NotifyRoles(SpecifySeer: killer);
                killer.ResetKillCooldown();
            }
        }
        //キルできない時
        else
        {
            info.DoKill = false;
        }
    }

    public override string GetProgressText(bool comms = false) => CanUseKillButton() ? "<color=#ff0000>EX</color>" : $"({metatonCount}/{fillingOfMax})";

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsMeeting) return;

        if (ShotLimit > 0)
        {
            ChangeSkin();
        }

        //if (YouTubeReader.liveId == "") return;
        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1.0f; // 負荷軽減の為1秒ごとの更新

        if (UpdateTime == 1.0f)
        {
            SetMetatonCount();
        }
    }

    private void ChangeSkin()
    {
        var player = Player;
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask || !player.IsAlive()) return;

        colorchange %= 18;
        if (colorchange is >= 0 and < 1) player.RpcSetColor(8);
        else if (colorchange is >= 1 and < 2) player.RpcSetColor(1);
        else if (colorchange is >= 2 and < 3) player.RpcSetColor(10);
        else if (colorchange is >= 3 and < 4) player.RpcSetColor(2);
        else if (colorchange is >= 4 and < 5) player.RpcSetColor(11);
        else if (colorchange is >= 5 and < 6) player.RpcSetColor(14);
        else if (colorchange is >= 6 and < 7) player.RpcSetColor(5);
        else if (colorchange is >= 7 and < 8) player.RpcSetColor(4);
        else if (colorchange is >= 8 and < 9) player.RpcSetColor(17);
        else if (colorchange is >= 9 and < 10) player.RpcSetColor(0);
        else if (colorchange is >= 10 and < 11) player.RpcSetColor(3);
        else if (colorchange is >= 11 and < 12) player.RpcSetColor(13);
        else if (colorchange is >= 12 and < 13) player.RpcSetColor(7);
        else if (colorchange is >= 13 and < 14) player.RpcSetColor(15);
        else if (colorchange is >= 14 and < 15) player.RpcSetColor(6);
        else if (colorchange is >= 15 and < 16) player.RpcSetColor(12);
        else if (colorchange is >= 16 and < 17) player.RpcSetColor(9);
        else if (colorchange is >= 17 and < 18) player.RpcSetColor(16);
        colorchange += Time.fixedDeltaTime;
    }

    public static void SetCount(int count)
    {
        Object lockObj = new();
        lock (lockObj)
        {
            getYTCommentCount = count;
        }
    }

    private void SetMetatonCount()
    {
        if (ShotLimit > 0) return;
        metatonCount += fillingOfTime;
        if (getYTCommentCount != 0)
        {
            int plusCount = getYTCommentCount * YTCommentFIllingOfTime;
            metatonCount += plusCount;
            //クリア
            SetCount(0);
        }

        //充填完了したらキル可能に
        if (metatonCount >= fillingOfMax)
        {
            ShotLimit = 1;
            SendRPC();
            Player.RpcProtectedMurderPlayer();
            if (MetatonBGM && Player == PlayerControl.LocalPlayer)
            {
                BGMSettings.spBGM = true;
                BGMSettings.SetTaskBGM();
            }
        }
        Utils.NotifyRoles();
    }

    public static string retBGMName()
    {
        var cRole = PlayerControl.LocalPlayer.GetCustomRole();
        if (cRole != CustomRoles.Metaton) return "";
        return "metatonEX";
    }
}