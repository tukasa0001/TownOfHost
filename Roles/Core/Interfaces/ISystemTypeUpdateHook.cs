namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// サボタージュに対する関与へのパッチ
/// </summary>
public interface ISystemTypeUpdateHook
{
    /// <summary>
    /// Skeld,MIRAHQ, Polus, Fungleのリアクター
    /// </summary>
    public bool UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount) => true;
    /// <summary>
    /// Airshipのリアクター
    /// </summary>
    public bool UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount) => true;
    /// <summary>
    /// Skeld, MIRAHQのO2
    /// </summary>
    public bool UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount) => true;
    /// <summary>
    /// Skeld, Polus, Airshipのコミュサボ
    /// </summary>
    public bool UpdateHudOverrideSystem(HudOverrideSystemType hudOverrideSystem, byte amount) => true;
    /// <summary>
    /// MIRAHQ, Fungleのコミュサボ
    /// </summary>
    public bool UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount) => true;
    /// <summary>
    /// Skeld, MIRAHQ, Polus, Airshipの停電と配電盤
    /// </summary>
    public bool UpdateSwitchSystem(SwitchSystem switchSystem, byte amount) => true;
    /// <summary>
    /// Polus, Airship, Fungleのドア開け
    /// </summary>
    public bool UpdateDoorsSystem(DoorsSystemType doorsSystem, byte amount) => true;
}

