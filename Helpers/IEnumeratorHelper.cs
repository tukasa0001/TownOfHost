
using System;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace TownOfHost;

public static class IEnumeratorWaitor
{
    /// <summary>指定回数実行した後にActionを実行するIEnumerator</summary
    /// Il2CppSystem.Collections.IEnumeratorを戻り値にするとyeild returnができないので間接実行
    public static Il2CppSystem.Collections.IEnumerator WaitCount(this Il2CppSystem.Collections.IEnumerator enumerator, int execCount, Action action)
    {
        return enumerator.WaitCountBase(execCount, action).WrapToIl2Cpp();
    }
    private static System.Collections.IEnumerator WaitCountBase(this Il2CppSystem.Collections.IEnumerator enumerator, int execCount, Action action)
    {
        int count = 0;
        while (enumerator.MoveNext())
        {
            if (count == execCount)
            {
                action();
            }
            count++;
            yield return enumerator.Current;
        }
    }
}
