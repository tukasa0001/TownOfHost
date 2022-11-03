using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Thief
    {
        public static readonly int Id = 51000;
        ///<summary>現在シーフのプレイヤーに加え過去にシーフだったプレイヤーも入っているので注意</summary>
        public static List<byte> playerIdList = new();

        public static CustomOption ThiefCooldown;
        public static CustomOption ThiefHasImpostorVision;
        public static CustomOption ThiefCanVent;
        public static CustomOption ThiefChangeTargetTeam;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Thief);
            ThiefCooldown = CustomOption.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "ThiefCooldown", 30f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Thief]);
            ThiefHasImpostorVision = CustomOption.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "ThiefHasImpostorVision", false, Options.CustomRoleSpawnChances[CustomRoles.Thief]);
            ThiefCanVent = CustomOption.Create(Id + 12, TabGroup.NeutralRoles, Color.white, "ThiefCanVent", true, Options.CustomRoleSpawnChances[CustomRoles.Thief]);
            ThiefChangeTargetTeam = CustomOption.Create(Id + 13, TabGroup.NeutralRoles, Color.white, "ThiefChangeTargetTeam", true, Options.CustomRoleSpawnChances[CustomRoles.Thief]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            var player = Utils.GetPlayerById(playerId);
            playerIdList.Add(playerId);
            if (!Main.ResetCamPlayerList.Contains(playerId))
            {
                Main.ResetCamPlayerList.Add(playerId);
            }
            // ホストがシーフになったとき，ホスト視点では自分がシェイプシフター判定になっている
            if (playerId == 0 && PlayerControl.LocalPlayer.PlayerId == 0)
            {
                player.Data.Role.TeamType = RoleTeamTypes.Crewmate;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    pc.Data.Role.CanBeKilled = true;
                }
            }
        }
        public static bool IsEnable() => playerIdList.Count > 0;

        public static void GetAbilityButtonText(HudManager __instance) =>
            __instance.KillButton.OverrideText(Translator.GetString("ThiefStealButtonText"));
        ///<summary>戻り値はスチールロールの成否です 成功した場合はtrueになります</summary>
        public static bool TrySteal(PlayerControl thief, PlayerControl target)
        {
            var targetRole = target.GetCustomRole();
            var succeeded = targetRole.IsImpostor() || (targetRole is
                CustomRoles.Sheriff or
                CustomRoles.Egoist);
            // 相手がキル役職でなければ自爆
            if (!succeeded)
            {
                Logger.Info($"{target.GetNameWithRole()}はスチールロールできない役職でした", "Thief");
                PlayerState.SetDeathReason(thief.PlayerId, PlayerState.DeathReason.Misfire);
                thief.RpcMurderPlayer(thief);
            }
            // 以下スチールロール処理
            else
            {
                Logger.Info($"{thief.Data.PlayerName}のロールを{targetRole}に変更", "Thief");
                thief.RpcSetCustomRole(targetRole);
                RPC.SetCustomRole(thief.PlayerId, targetRole);
                if (ThiefChangeTargetTeam.GetBool())
                {
                    Logger.Info($"スチールロールされたプレイヤー{target.GetNameWithRole()}のロールをオポチュニストに変更します", "Thief");
                    target.RpcSetCustomRole(CustomRoles.Opportunist);
                }
                Utils.NotifyRoles();
                thief.CustomSyncSettings();
                thief.RpcResetAbilityCooldown();
                if (targetRole.IsImpostor() || targetRole == CustomRoles.Egoist)
                    thief.Data.Role.TeamType = RoleTeamTypes.Impostor;
                // ホストはこれをしないとサボボタンなどが出てこないor押せない
                if (thief.PlayerId == 0)
                {
                    DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
                    if (targetRole.IsImpostor() || targetRole == CustomRoles.Egoist)
                    {
                        thief.Data.Role.CanVent = true;
                    }
                }
            }
            return succeeded;
        }
        ///<summary>純粋なインポスターと元シーフのインポスターは互いにキルできてしまうため，その判定をします．
        ///キル者が元シーフでターゲットがインポスターの場合とキル者がインポスターでターゲットが元シーフの場合にfalseを返します</summary>
        public static bool CanKill(PlayerControl killer, PlayerControl target) =>  // キル者は確定でインポスター(=シーフではない)
            !(killer == target ||
            (playerIdList.Contains(killer.PlayerId) && target.GetCustomRole().IsImpostor()) ||
            (playerIdList.Contains(target.PlayerId) && !target.Is(CustomRoles.Thief)));
        public static void ApplyGameOptions(GameOptionsData opt, byte playerId)
        {
            var pc = Utils.GetPlayerById(playerId);
            opt.RoleOptions.ShapeshifterDuration = 1f;
            opt.RoleOptions.ShapeshifterCooldown = 255f;
            opt.SetVision(pc, ThiefHasImpostorVision.GetBool());
            opt.KillCooldown = ThiefCooldown.GetFloat();
        }
        public static void RestoreKillButtonText(PlayerControl killer)
        {
            if (killer != PlayerControl.LocalPlayer || killer.Is(CustomRoles.Thief)) return;
            DestroyableSingleton<HudManager>.Instance.KillButton.OverrideText(Translator.GetString("KillButtonText"));
        }
        /// <summary>本来シェイプシフター置き換えではないロールをスチールロールした元シーフの変身クールを255秒にします</summary>
        public static void ApplyLongShapeshiftCooldown(GameOptionsData opt, PlayerControl player)
        {
            if (player.GetCustomRole().GetVanillaRole() != RoleTypes.Shapeshifter)
                opt.RoleOptions.ShapeshifterCooldown = 255f;
        }
    }
}
