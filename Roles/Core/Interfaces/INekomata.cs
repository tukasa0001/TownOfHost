namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// 追放されたときに誰かを道連れにする役職
/// </summary>
public interface INekomata
{
    /// <summary>
    /// 道連れが発動するかのチェック
    /// </summary>
    /// <param name="deathReason">猫又の死因</param>
    /// <returns>道連れを発生させるならtrue</returns>
    public bool DoRevenge(CustomDeathReason deathReason);
    /// <summary>
    /// プレイヤーが道連れ対象の候補に含まれるかどうかのチェック
    /// </summary>
    /// <param name="player">判定するプレイヤー</param>
    /// <returns>playerを道連れ対象候補に含ませるならtrue</returns>
    public bool IsCandidate(PlayerControl player);
}
