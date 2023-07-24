namespace TownOfHost.Attributes;

/// <summary>
/// <see cref="AmongUsClient.CoStartGame"/>のPostfixで毎ゲーム初期化するメソッドに使う<br/>
/// staticメソッドの前に [GameModuleInitializer] と付けるとゲーム開始時に自動で呼び出される<br/>
/// [GameModuleInitializer(InitializePriority.High)] のようにすることで呼び出される順番を指定できる
/// </summary>
public sealed class GameModuleInitializerAttribute : InitializerAttribute<GameModuleInitializerAttribute> { }
