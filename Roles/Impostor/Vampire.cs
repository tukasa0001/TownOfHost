using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class Vampire
    {
        class BittenInfo
        {
            public byte VampireId;
            public float KillTimer;

            public BittenInfo(byte vampierId, float killTimer)
            {
                VampireId = vampierId;
                KillTimer = killTimer;
            }
        }
        static readonly int Id = 1300;
        static List<byte> PlayerIdList = new();

        static OptionItem OptionKillDelay;

        static float KillDelay;

        static Dictionary<byte, BittenInfo> BittenPlayers = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vampire);
            OptionKillDelay = FloatOptionItem.Create(Id + 10, "VampireKillDelay", new(1f, 1000f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vampire])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            IsEnable = false;
            PlayerIdList.Clear();
            BittenPlayers.Clear();

            KillDelay = OptionKillDelay.GetFloat();
        }

        public static void Add(byte playerId)
        {
            IsEnable = true;
            PlayerIdList.Add(playerId);
        }

        public static bool IsEnable = false;
        public static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (!IsThisRole(killer.PlayerId)) return true;
            if (target.Is(CustomRoles.Bait)) return true;

            killer.SetKillCooldown();

            //誰かに噛まれていなければ登録
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                BittenPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
            }
            return false;
        }

        public static void OnFixedUpdate(PlayerControl vampire)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            var vampireID = vampire.PlayerId;
            if (!IsThisRole(vampire.PlayerId)) return;

            List<byte> targetList = new(BittenPlayers.Where(b => b.Value.VampireId == vampireID).Select(b => b.Key));

            foreach (var targetId in targetList)
            {
                var bittenVampire = BittenPlayers[targetId];
                if (bittenVampire.KillTimer >= KillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    KillBitten(vampire, target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    bittenVampire.KillTimer += Time.fixedDeltaTime;
                    BittenPlayers[targetId] = bittenVampire;
                }
            }
        }
        public static void KillBitten(PlayerControl vampire, PlayerControl target, bool isButton = false)
        {
            if (target.IsAlive())
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bite;
                target.SetRealKiller(vampire);
                target.RpcMurderPlayer(target);
                Logger.Info($"Vampireに噛まれている{target.name}を自爆させました。", "Vampire");
                if (!isButton && vampire.IsAlive())
                {
                    RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                    if (target.Is(CustomRoles.Trapper))
                        vampire.TrapperKilled(target);
                }
            }
            else
            {
                Logger.Info("Vampireに噛まれている" + target.name + "はすでに死んでいました。", "Vampire");
            }
        }

        public static void OnStartMeeting()
        {
            foreach (var targetId in BittenPlayers.Keys)
            {
                var target = Utils.GetPlayerById(targetId);
                var vampire = Utils.GetPlayerById(BittenPlayers[targetId].VampireId);
                KillBitten(vampire, target);
            }
            BittenPlayers.Clear();
        }
        public static void SetKillButtonText()
        {
            HudManager.Instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
        }
    }
}
