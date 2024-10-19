using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public bool SingleTrigger(PlayerControl killer, PlayerControl target);
    /// <summary>
    /// ダブル時のアクションの記述
    /// </summary>
    /// <param name="killer"></param>
    /// <param name="target"></param>
    /// <returns>true:キルする false:キルしない</returns>
    public bool DoubleTrigger(PlayerControl killer, PlayerControl target);
}
