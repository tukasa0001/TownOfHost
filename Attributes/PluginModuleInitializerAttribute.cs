namespace TownOfHost.Attributes;

/// <summary>
/// <see cref="Main.Load"/>で起動時に初期化するメソッドに使う<br/>
/// staticメソッドの前に [PluginModuleInitializer] と付けると起動時に自動で呼び出される<br/>
/// [PluginModuleInitializer(InitializePriority.High)] のようにすることで呼び出される順番を指定できる
/// </summary>
public sealed class PluginModuleInitializerAttribute : InitializerAttribute<PluginModuleInitializerAttribute> { }
