using System;

namespace TownOfHost.Modules;

public static class SystemEnvironment
{
    public static void SetEnvironmentVariables()
    {
        // ユーザ環境変数に最近開かれたTOHアモアスフォルダのパスを設定
        Environment.SetEnvironmentVariable("TOWN_OF_HOST_DIR_ROOT", Environment.CurrentDirectory, EnvironmentVariableTarget.User);
        // ユーザ環境変数にログフォルダのパスを設定
        Environment.SetEnvironmentVariable("TOWN_OF_HOST_DIR_LOGS", Utils.GetLogFolder().FullName, EnvironmentVariableTarget.User);
    }
}
