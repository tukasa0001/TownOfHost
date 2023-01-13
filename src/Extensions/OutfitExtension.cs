namespace TownOfHost.Extensions;

public static class OutfitExtension
{
    public static GameData.PlayerOutfit Clone(this GameData.PlayerOutfit outfit)
    {
        GameData.PlayerOutfit copied = new()
        {
            PlayerName = outfit.PlayerName,
            ColorId = outfit.ColorId,
            dontCensorName = false,
            HatId = outfit.HatId,
            PetId = outfit.PetId,
            SkinId = outfit.SkinId,
            VisorId = outfit.VisorId,
            NamePlateId = outfit.NamePlateId
        };
        return copied;
    }
}