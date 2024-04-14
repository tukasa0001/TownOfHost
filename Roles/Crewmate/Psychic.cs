using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

using AmongUs.GameOptions;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using UnityEngine;
using static TownOfHostForE.Translator;
using Hazel;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Psychic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Psychic),
            player => new Psychic(player),
            CustomRoles.Psychic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            41200,
            SetupOptionItem,
            "霊媒師",
            "#883fd1",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Psychic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();

        VentSelect.Init();
    }
    private static OptionItem OptionCooldown;
    public static OptionItem MaxCheckRole;
    public static OptionItem ConfirmCamp;
    public static OptionItem KillerOnly;
    enum OptionName
    {
        PsychicMaxCheckRole,
        FortuneTellerConfirmCamp,
        FortuneTellerKillerOnly,
    }

    public static float Cooldown = 30;
    int DivinationLeftCount = 0;
    bool IsCoolTimeOn = true;

    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MaxCheckRole = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PsychicMaxCheckRole, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        ConfirmCamp = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FortuneTellerConfirmCamp, false, false);
        KillerOnly = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FortuneTellerKillerOnly, false, false);
    }
    public override void Add()
    {
        IsCoolTimeOn = true;
        DivinationLeftCount = MaxCheckRole.GetInt();
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetPsychicCount);
        sender.Writer.Write(DivinationLeftCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetPsychicCount) return;

        DivinationLeftCount = reader.ReadInt32();
    }
    public override void OnStartMeeting()
    {
        IsCoolTimeOn = true;
        VentSelect.ClearSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseVent() ? (IsCoolTimeOn ? Cooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseVent()
        => Player.IsAlive()
        && DivinationLeftCount > 0;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseVent()) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        VentSelect.PlayerSelect(Player);

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;

        if (VentSelect.OnFixedUpdate(Player, out var selectedId))
        {
            DivinationLeftCount--;
            SendRPC();
            player.MarkDirtySettings();
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen,bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!VentSelect.IsShowTargetRole(Player, seen)) return;
        if (KillerOnly.GetBool() &&
            !(seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.IsCrewKiller() || seen.IsAnimalsKiller()
            || seen.Is(CustomRoles.MadSheriff)|| seen.Is(CustomRoles.GrudgeSheriff))) return;

        enabled = true;

        if (!ConfirmCamp.GetBool()) return;   //役職表示

        //陣営表示
        if (seen.GetCustomRole().IsImpostor() || seen.GetCustomRole().IsMadmate())
        {
            roleColor = Palette.ImpostorRed;
            roleText = GetString("TeamImpostor");
        }
        else if (seen.GetCustomRole().IsNeutral())
        {
            roleColor = Color.gray;
            roleText = GetString("Neutral");
        }
        else if (seen.GetCustomRole().IsAnimals())
        {
            string colorString = "#FF8C00";
            Color AnimalsColor;
            ColorUtility.TryParseHtmlString(colorString, out AnimalsColor);
            roleColor = AnimalsColor;
            roleText = GetString("Animals");
        }
        else
        {
            roleColor = Palette.CrewmateBlue;
            roleText = GetString("TeamCrewmate");
        }
    }
    public bool KnowTargetRoleColor(PlayerControl target)
    {
        if (!VentSelect.IsShowTargetRole(Player, target)) return false;
        if (!ConfirmCamp.GetBool()) return false;   //役職表示
        if (KillerOnly.GetBool() &&
        !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller() || target.IsCrewKiller() || target.IsAnimalsKiller()
        || target.Is(CustomRoles.MadSheriff) || target.Is(CustomRoles.GrudgeSheriff))) return false;
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseVent() || isForMeeting) return string.Empty;

        return VentSelect.GetCheckPlayerText(seer, isForHud);
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"({DivinationLeftCount})");
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");


    //死人がターゲットの特別なセレクト
    public static class VentSelect
    {
        public static Dictionary<byte, PlayerControl> SelectPlayer = new();
        public static Dictionary<byte, bool> SelectFix = new();
        public static Dictionary<byte, float> TergetFixTimer = new();
        public static Dictionary<byte, List<byte>> CheckRolePlayer = new();
        public static void Init()
        {
            SelectPlayer = new();
            SelectFix = new();
            TergetFixTimer = new();
            CheckRolePlayer = new();
        }
        public static void ClearSelect()
        {
            SelectPlayer.Clear();
            SelectFix.Clear();
            TergetFixTimer.Clear();
        }
        public static IEnumerable<PlayerControl> SelectList(byte playerId)
                    => Main.AllDeadPlayerControls.Where(x => !CheckRolePlayer[playerId].Contains(x.PlayerId));
        public static void PlayerSelect(PlayerControl player)
        {
            if (player == null) return;

            var playerId = player.PlayerId;
            if (TergetFixTimer.ContainsKey(playerId)) //タイマーリセット
                TergetFixTimer.Remove(playerId);

            if (SelectFix.TryGetValue(playerId, out var fix) && fix) return;

            PlayerControl first = null;
            SelectPlayer.TryGetValue(playerId, out var selectedPlayer);
            var preSelected = false;
            var selected = false;
            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            foreach (var target in SelectList(playerId))
            {
                if (target == player) continue;

                if (first == null) first = target;

                if (preSelected)
                {
                    SelectPlayer[playerId] = target;
                    selected = true;
                    Logger.Info($"{player.name} PlayerSelectNow:{target.name}, nextTarget", "player");
                    break;
                }

                if (target == selectedPlayer) preSelected = true;
            }
            if (first == null)
            {
                SelectPlayer[playerId] = null;
                Logger.Info($"{player.name} PlayerSelectNow:null, ターゲットなし", "player");
                return;
            }
            if (!selected)
            {
                SelectPlayer[playerId] = first;
                Logger.Info($"{player.name} PlayerSelectNow:{first?.name}, firstTarget", "player");
            }

            TergetFixTimer.Add(player.PlayerId, 3f);
        }
        public static bool OnFixedUpdate(PlayerControl player, out int selectedId)
        {
            selectedId = -1;

            if (player == null) return false;
            if (!GameStates.IsInTask) return false;

            var playerId = player.PlayerId;
            if (!TergetFixTimer.ContainsKey(playerId)) return false;

            TergetFixTimer[playerId] -= Time.fixedDeltaTime;
            if (TergetFixTimer[playerId] > 0) return false;

            //以下ターゲット確定
            SelectFix[playerId] = true;
            selectedId = SelectPlayer[playerId].PlayerId;

            if (!CheckRolePlayer.ContainsKey(playerId)) CheckRolePlayer.Add(playerId, new());
            CheckRolePlayer[playerId].Add(SelectPlayer[playerId].PlayerId);
            TergetFixTimer.Remove(playerId);

            Logger.Info($"{player.name} PlayerDecision:{SelectPlayer[playerId].name}", "player");
            player.RpcProtectedMurderPlayer();   //設定完了のパリン
            player.RpcResetAbilityCooldown();

            return true;
        }
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            CheckRolePlayer.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
        public static string GetCheckPlayerText(PlayerControl psychic, bool hud)
        {
            if (psychic == null) return "";
            var psychicId = psychic.PlayerId;

            var str = new StringBuilder();
            SelectPlayer.TryGetValue(psychicId, out var target);
            if (hud)
            {
                if (target == null)
                    str.Append(GetString("SelectPlayerTagBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTag"));
                    str.Append(target.GetRealName());
                }
            }
            else
            {
                if (target == null)
                    str.Append(GetString("SelectPlayerTagMiniBefore"));
                else
                {
                    str.Append(GetString("SelectPlayerTagMini"));
                    str.Append(target.GetRealName());
                }
            }
            return str.ToString();
        }
    }
}