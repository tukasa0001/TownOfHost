using System.Collections.Generic;
namespace TownOfHost
{
    public static class Camouflage
    {
        public static Dictionary<byte, (int, string, string, string, string)> PlayerSkins = new();
        public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false)
        {
            if (!(AmongUsClient.Instance.AmHost && Options.CommsCamouflage.GetBool())) return;
            if (target == null) return;
            var id = target.PlayerId;

            int colorId = 15; //グレー
            string hatId = "";
            string skinId = "";
            string visorId = "";
            string petId = "";
            if (Utils.IsActive(SystemTypes.Comms))
            {
                if (Main.PlayerStates[id].IsDead) return;
            }
            if (!Utils.IsActive(SystemTypes.Comms) || ForceRevert)
            {
                var GetValue = Main.CheckShapeshift.TryGetValue(id, out var shapeshifting);
                if (!GetValue && Main.CheckShapeshift.ContainsKey(id)) return;

                var outfit = target.CurrentOutfit;
                var value = PlayerSkins[id];

                if (
                    outfit.ColorId == value.Item1 &&
                    outfit.HatId == value.Item2 &&
                    outfit.SkinId == value.Item3 &&
                    outfit.VisorId == value.Item4 &&
                    outfit.PetId == value.Item5
                    ) return; //姿が変わっていないなら処理しない

                colorId = shapeshifting ? outfit.ColorId : value.Item1;
                hatId = shapeshifting ? outfit.HatId : value.Item2;
                skinId = shapeshifting ? outfit.SkinId : value.Item3;
                visorId = shapeshifting ? outfit.VisorId : value.Item4;
                petId = shapeshifting ? outfit.PetId : value.Item5;
            }

            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

            target.SetColor(colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(colorId)
                .EndRpc();

            target.SetHat(hatId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(hatId)
                .EndRpc();

            target.SetSkin(skinId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(skinId)
                .EndRpc();

            target.SetVisor(visorId, colorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(visorId)
                .EndRpc();

            target.SetPet(petId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(petId)
                .EndRpc();

            sender.SendMessage();
        }
    }
}