using AmongUs.GameOptions;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// インポスターのインタフェイス<br/>
/// <see cref="IKiller"/>を継承
/// </summary>
public interface IImpostor : IKiller, ISchrodingerCatOwner
{
    /// インポスターは基本サボタージュボタンを使える
    bool IKiller.CanUseSabotageButton() => true;
    /// <summary>
    /// ラストインポスターになれるかどうか デフォルトtrue
    /// </summary>
    public bool CanBeLastImpostor => true;
    /// <summary>
    /// シュレディンガーの猫を切った際の変化先役職<br/>
    /// デフォルト<see cref="SchrodingerCat.TeamType.TeamImpostor"/>
    /// </summary>
    SchrodingerCat.TeamType ISchrodingerCatOwner.SchrodingerCatChangeTo => SchrodingerCat.TeamType.Mad;

    /// <summary>
    /// この役職に切られたシュレディンガーの猫へのオプション変更<br/>
    /// デフォルト<see cref="SchrodingerCat.ApplyMadCatOptions"/>
    /// </summary>
    void ISchrodingerCatOwner.ApplySchrodingerCatOptions(IGameOptions option)
    {
        SchrodingerCat.ApplyMadCatOptions(option);
    }
}
