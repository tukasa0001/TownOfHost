namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// キルボタンのシングルクリック、ダブルクリックで機能を変えられるようにするためのインターフェース
/// </summary>
public interface IDoubleTrigger
{
    /// <summary>
    /// シングル時のアクションの記述
    /// </summary>
    /// <param name="killer"></param>
    /// <param name="target"></param>
    /// <returns>true:キルする false:キルしない</returns>
    public bool SingleAction(PlayerControl killer, PlayerControl target);
    /// <summary>
    /// ダブル時のアクションの記述
    /// </summary>
    /// <param name="killer"></param>
    /// <param name="target"></param>
    /// <returns>true:キルする false:キルしない</returns>
    public bool DoubleAction(PlayerControl killer, PlayerControl target);
}
