using AmongUs.GameOptions;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// シュレディンガーの猫をキルして仲間に引き入れる事ができる役職のインタフェイス
/// </summary>
public interface ISchrodingerCatOwner
{
    /// <summary>
    /// シュレディンガーの猫を切った際の変化先役職
    /// </summary>
    public SchrodingerCat.TeamType SchrodingerCatChangeTo { get; }
    /// <summary>
    /// この役職に切られたシュレディンガーの猫へのオプション変更<br/>
    /// デフォルトではなにもしない
    /// </summary>
    public void ApplySchrodingerCatOptions(IGameOptions option) { }

    /// <summary>
    /// シュレディンガーの猫をキルした際に追加で実行するアクション
    /// </summary>
    public void OnSchrodingerCatKill(SchrodingerCat schrodingerCat) { }
}
