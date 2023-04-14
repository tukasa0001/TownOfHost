namespace TownOfHost.Roles.Core.Interfaces;

public interface IAdditionalWinner
{
    public AdditionalWinners WinnerType { get; }
    public bool CheckWin();
}
