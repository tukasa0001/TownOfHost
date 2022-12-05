using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Publisher
    {
        private static readonly int Id = 7000;
        public static List<byte> playerIdList = new();
        public static OptionItem SendAllPlayer;
        public static Dictionary<byte, byte> Killer = new();
        public static List<byte> Target = new();
        public static Dictionary<byte, byte> SendTarget = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Publisher);
            SendAllPlayer = BooleanOptionItem.Create(Id + 10, "PublisherSendAllPlayer", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Publisher]);
        }
        public static void Init()
        {
            playerIdList = new();
            Killer = new();
            Target = new();
            SendTarget = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static PlayerControl GetPublisherKiller(byte targetId)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return null;
            Killer.TryGetValue(targetId, out var killerId);
            var killer = Utils.GetPlayerById(killerId);
            return killer;
        }
        public static void PublisherUseAbility(PlayerControl reporter, PlayerControl target)
        {
            if (target == null) return;

            new LateTask(() =>
            {
                if (!(target.Is(CustomRoles.Publisher) && target.Data.IsDead)) return;
                if (reporter == target) return;
                if (!target.Data.IsDead) return;
                var killer = GetPublisherKiller(target.PlayerId);

                //動作
                string publishermessage = string.Format(GetString("PublisherKiller"), killer.GetRealName(true));
                if (SendAllPlayer.GetBool())
                {
                    Utils.SendMessage($"{publishermessage}");
                }
                else
                {
                    var rand = new System.Random();
                    List<PlayerControl> targetPlayers = new();
                    //切断者と死亡者を除外してプレイヤーリストに
                    foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    {
                        if (!p.Data.Disconnected && !p.Data.IsDead && !Main.SpeedBoostTarget.ContainsValue(p.PlayerId)) targetPlayers.Add(p);
                    }
                    //ターゲットが0なら送信先のプレイヤーをnullに
                    if (targetPlayers.Count >= 1)
                    {
                        PlayerControl sendtarget = targetPlayers[rand.Next(0, targetPlayers.Count)];
                        Logger.Info("インポスター表示先:" + sendtarget.cosmetics.nameText.text, "Publisher");
                        SendTarget.Add(target.PlayerId, sendtarget.PlayerId);
                        Utils.SendMessage($"{publishermessage}", PlayerControl.AllPlayerControls[SendTarget[target.PlayerId]].PlayerId);
                    }
                    else
                    {
                        SendTarget.Add(target.PlayerId, 255);
                        Logger.SendInGame("Error.PublisherNullException");
                        Logger.Warn("メッセージ送信先がnullです。", "Publisher");
                    }

                }
            }, 3f, "UsePublisherAbility");
        }
    }
}