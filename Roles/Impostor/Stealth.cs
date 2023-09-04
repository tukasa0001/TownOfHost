using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class Stealth : RoleBase, IImpostor
{
    public Stealth(PlayerControl player) : base(RoleInfo, player)
    {
        excludeImpostors = optionExcludeImpostors.GetBool();
        darkenDuration = darkenTimer = optionDarkenDuration.GetFloat();
        darkenedPlayers = null;
    }
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.Create(
        typeof(Stealth),
        player => new Stealth(player),
        CustomRoles.Stealth,
        () => RoleTypes.Impostor,
        CustomRoleTypes.Impostor,
        3200,
        SetupOptionItems,
        "st",
        introSound: () => GetIntroSound(RoleTypes.Shapeshifter));
    private static LogHandler logger = Logger.Handler(nameof(Stealth));

    #region カスタムオプション
    private static BooleanOptionItem optionExcludeImpostors;
    private static FloatOptionItem optionDarkenDuration;
    private enum OptionName { StealthExcludeImpostors, StealthDarkenDuration, }
    private static void SetupOptionItems()
    {
        optionExcludeImpostors = BooleanOptionItem.Create(RoleInfo, 10, OptionName.StealthExcludeImpostors, true, false);
        optionDarkenDuration = FloatOptionItem.Create(RoleInfo, 20, OptionName.StealthDarkenDuration, new(0.5f, 5f, 0.5f), 1f, false);
        optionDarkenDuration.SetValueFormat(OptionFormat.Seconds);
    }
    #endregion

    private bool excludeImpostors;
    private float darkenDuration;
    /// <summary>暗転解除までのタイマー</summary>
    private float darkenTimer;
    /// <summary>今暗転させているプレイヤー 暗転効果が発生してないときはnull</summary>
    private PlayerControl[] darkenedPlayers;
    /// <summary>暗くしている部屋</summary>
    private SystemTypes? darkenedRoom = null;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // キルできない，もしくは普通のキルじゃないならreturn
        if (!info.CanKill || !info.DoKill || info.IsSuicide || info.IsAccident || info.IsFakeSuicide)
        {
            return;
        }
        var playersToDarken = FindPlayersInSameRoom(info.AttemptTarget);
        if (playersToDarken == null)
        {
            logger.Info("部屋の当たり判定を取得できないため暗転を行いません");
            return;
        }
        if (excludeImpostors)
        {
            playersToDarken = playersToDarken.Where(player => !player.Is(CustomRoles.Impostor));
        }
        DarkenPlayers(playersToDarken);
    }
    /// <summary>自分と同じ部屋にいるプレイヤー全員を取得する</summary>
    private IEnumerable<PlayerControl> FindPlayersInSameRoom(PlayerControl killedPlayer)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        return Main.AllAlivePlayerControls.Where(player => player != Player && player.Collider.IsTouching(roomArea));
    }
    /// <summary>渡されたプレイヤーを<see cref="darkenDuration"/>秒分視界ゼロにする</summary>
    private void DarkenPlayers(IEnumerable<PlayerControl> playersToDarken)
    {
        darkenedPlayers = playersToDarken.ToArray();
        foreach (var player in playersToDarken)
        {
            PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }
        // 誰かを暗転させているとき
        if (darkenedPlayers != null)
        {
            // タイマーを減らす
            darkenTimer -= Time.fixedDeltaTime;
            // タイマーが0になったらみんなの視界を戻してタイマーと暗転プレイヤーをリセットする
            if (darkenTimer <= 0)
            {
                ResetDarkenState();
            }
        }
    }
    public override void OnStartMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
    }
    private void RpcDarken(SystemTypes? roomType)
    {
        logger.Info($"暗転させている部屋を{roomType?.ToString() ?? "null"}に設定");
        darkenedRoom = roomType;
        using var sender = CreateSender(CustomRPC.StealthDarken);
        sender.Writer.Write((byte?)roomType ?? byte.MaxValue);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.StealthDarken)
        {
            var roomId = reader.ReadByte();
            darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
        }
    }
    /// <summary>発生している暗転効果を解除</summary>
    private void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (var player in darkenedPlayers)
            {
                PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = darkenDuration;
        RpcDarken(null);
        Utils.NotifyRoles(SpecifySeer: Player);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        // 会議中，自分のSuffixじゃない，どこも暗転させてなければ何も出さない
        if (isForMeeting || seer != Player || seen != Player || !darkenedRoom.HasValue)
        {
            return base.GetSuffix(seer, seen, isForMeeting);
        }
        return string.Format(Translator.GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
    }
}
