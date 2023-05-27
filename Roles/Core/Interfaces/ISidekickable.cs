namespace TownOfHost.Roles.Core.Interfaces;

public interface ISidekickable
{
    /// <summary>
    /// サイドキック対象が変化する役職です．SKMadmateなら何もする必要はありません．<br/>
    /// ジャッカル陣営などにサイドキック役職ができた場合に継承先で変更してください．<br/>
    /// get-onlyプロパティなので注意 ( ｢=｣で書くと別物になります )
    /// </summary>
    public CustomRoles SidekickTargetRole => CustomRoles.SKMadmate;
    /// <summary>
    /// サイドキックを作れるかどうか．ほとんどの場合各役職のサイドキックオプションをラップするbool型メンバの値をそのまま返すことになります
    /// </summary>
    /// <returns>作れるならtrue</returns>
    public bool CanMakeSidekick() => true;
}
