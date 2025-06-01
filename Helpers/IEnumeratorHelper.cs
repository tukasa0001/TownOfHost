
using System;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime;

namespace TownOfHost;

public class CoroutinPatcher
{
    Dictionary<string, Action> _prefixActions = [];
    Dictionary<string, Action> _postfixActions = [];
    private readonly Il2CppSystem.Collections.IEnumerator _enumerator;
    public CoroutinPatcher(Il2CppSystem.Collections.IEnumerator enumerator)
    {
        _enumerator = enumerator;
    }
    public void AddPrefix(Type type, Action action)
    {
        var key = Il2CppType.From(type).FullName;
        Logger.Info($"AddPrefix: {key}", "CoroutinPatcher");
        _prefixActions[key] = action;
    }
    public void AddPostfix(Type type, Action action)
    {
        var key = Il2CppType.From(type).FullName;
        Logger.Info($"AddPostfix: {key}", "CoroutinPatcher");
        _postfixActions[key] = action;
    }
    public Il2CppSystem.Collections.IEnumerator EnumerateWithPatch()
    {
        return EnumerateWithPatchInternal().WrapToIl2Cpp();
    }
    public System.Collections.IEnumerator EnumerateWithPatchInternal()
    {
        Logger.Info("ExecEnumerator", "CoroutinPatcher");
        while (_enumerator.MoveNext())
        {
            var fullName = _enumerator.Current?.GetIl2CppType()?.FullName;
            if (fullName == null)
            {
                Logger.Info("Current: null", "CoroutinPatcher");
                yield return _enumerator.Current;
                continue;
            }
            Logger.Info($"Current: {fullName}", "CoroutinPatcher");

            if (_prefixActions.TryGetValue(fullName, out var prefixAction))
            {
                Logger.Info($"Exec Prefix: {fullName}", "CoroutinPatcher");
                prefixAction();
            }
            Logger.Info($"Yield Return: {fullName}", "CoroutinPatcher");
            yield return _enumerator.Current;
            if (_postfixActions.TryGetValue(fullName, out var postfixAction))
            {
                Logger.Info($"Exec Postfix: {fullName}", "CoroutinPatcher");
                postfixAction();
            }
        }
        Logger.Info("ExecEnumerator End", "CoroutinPatcher");
    }
}

