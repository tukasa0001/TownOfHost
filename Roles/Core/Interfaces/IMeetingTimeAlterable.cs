namespace TownOfHost.Roles.Core.Interfaces;

public interface IMeetingTimeAlterable
{
    /// <summary>
    /// 死亡時に変更した会議時間を元に戻すかどうか<br/>
    /// get-onlyプロパティなので注意 ( ｢=｣で書くと別物になります )
    /// </summary>
    public bool RevertOnDie { get; }
    /// <summary>
    /// 会議時間の増減を計算するメソッド
    /// </summary>
    /// <returns>増減させる時間</returns>
    public int CalculateMeetingTimeDelta();
}
