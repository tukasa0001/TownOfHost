namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// インポスターのインタフェイス<br/>
/// <see cref="IKiller"/>を継承
/// </summary>
public interface IImpostor : IKiller
{
    /// <summary>
    /// ラストインポスターになれるかどうか デフォルトtrue
    /// </summary>
    public bool CanBeLastImpostor => true;
}
