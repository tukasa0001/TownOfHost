using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using Hazel;
using TownOfHost.API;
using TownOfHost.Chat.Patches;
using TownOfHost.GUI;
using TownOfHost.Options;
using VentLib.Localization;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost;

public static class Utils
{
    public static string GetNameWithRole(this GameData.PlayerInfo player)
    {
        return GetPlayerById(player.PlayerId)?.GetNameWithRole() ?? "";
    }

    public static string GetRoleName(CustomRole role)
    {
        // return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        return role.RoleName;
    }

    public static Color GetRoleColor(CustomRole role)
    {
        return role.RoleColor;
    }

    public static Color ConvertHexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    public static bool HasTasks(GameData.PlayerInfo p) => p.GetCustomRole().HasTasks();

    // GM.Ref<GM>()
    public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
    {
        SendMessage(Localizer.Get("StaticOptions.ActiveSettingsHelp") + ":", PlayerId);

        if (StaticOptions.SyncButtonMode)
        {
            SendMessage(Localizer.Get("StaticOptions.SyncButton.Info"), PlayerId);
        }

        if (StaticOptions.SabotageTimeControl)
        {
            SendMessage(Localizer.Get("StaticOptions.SabotageTimeControl.Info"), PlayerId);
        }

        if (StaticOptions.RandomMapsMode)
        {
            SendMessage(Localizer.Get("StaticOptions.RandomMap.Info"), PlayerId);
        }

        if (StaticOptions.EnableGM)
        {
            SendMessage(CustomRoleManager.Special.GM.RoleName + Localizer.Get("StaticOptions.EnableGMInfo"), PlayerId);
        }

        foreach (var role in CustomRoleManager.AllRoles)
        {
            if (role is Fox or Troll) continue;
            if (role.IsEnable() && !role.IsVanilla())
                SendMessage(role.RoleName + Localizer.Get($"StaticOptions.{role.EnglishRoleName}.Description"), PlayerId);
        }

        if (StaticOptions.NoGameEnd) SendMessage(Localizer.Get("StaticOptions.NoGameEndInfo"), PlayerId);
    }

    /*public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
    {
        var mapId = TOHPlugin.NormalOptions.MapId;
        var text = "";
        if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
        {
            text = GetString("Roles") + ":";
            if (Fox.Ref<Fox>().IsEnable())
                text += String.Format("\n{0}:{1}", GetRoleName(Fox.Ref<Fox>()), Fox.Ref<Fox>().Count);
            if (Troll.Ref<Troll>().IsEnable())
                text += String.Format("\n{0}:{1}", GetRoleName(Troll.Ref<Troll>()), Troll.Ref<Troll>().Count);
            SendMessage(text, PlayerId);
            text = GetString("Settings") + ":";
            text += GetString("HideAndSeek");
        }
        else
        {
            text = GetString("Settings") + ":";
            foreach (var role in OldOptions.CustomRoleCounts)
            {
                if (!role.Key.GetReduxRole().IsEnable()) continue;
                text += $"\n【{GetRoleName(role.Key.GetReduxRole())}×{role.Key.GetReduxRole().Count}】\n";
                ShowChildrenSettings(OldOptions.CustomRoleSpawnChances[role.Key], ref text);
                text = text.RemoveHtmlTags();
            }

            foreach (var opt in OptionItem.AllOptions.Where(x =>
                         x.GetBool() && x.Parent == null && x.Id >= 80000 &&
                         !x.IsHiddenOn(OldOptions.CurrentGameMode)))
            {
                if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                    text += $"\n【{opt.GetName(true)}: {{opt.GetString()}}】\n";
                else
                    text += $"\n【{opt.GetName(true)}】\n";
                ShowChildrenSettings(opt, ref text);
                text = text.RemoveHtmlTags();
            }
        }

        SendMessage(text, PlayerId);
    }*/

    /*public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
    {
        var text = GetString("Roles") + ":";
        text += string.Format("\n{0}:{1}", GetRoleName(GM.Ref<GM>()),
            StaticOptions.EnableGM.ToString().RemoveHtmlTags());
        foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
        {
            if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
            if (role.GetReduxRole().IsEnable())
                text += string.Format("\n{0}:{1}x{2}", GetRoleName(role.GetReduxRole()),
                    $"{role.GetReduxRole().Chance * 100}%", role.GetReduxRole().Count);
        }

        SendMessage(text, PlayerId);
    }*/

    public static void Teleport(CustomNetworkTransform nt, Vector2 location)
    {
        if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
        MessageWriter writer =
            AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
        NetHelpers.WriteVector2(location, writer);
        writer.Write(nt.lastSequenceId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    /*public static void ShowLastResult(byte PlayerId = byte.MaxValue)
    {
        if (AmongUsClient.Instance.IsGameStarted)
        {
            SendMessage(GetString("CantUse.lastroles"), PlayerId);
            return;
        }

        var text = GetString("LastResult") + ":";
        List<byte> cloneRoles = new(TOHPlugin.PlayerStates.Keys);
        text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
        /*foreach (var id in TOHPlugin.winnerList)
        {
            text += $"\n★ " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
            cloneRoles.Remove(id);
        }#1#

        foreach (var id in cloneRoles)
        {
            text += $"\n　 " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
        }

        SendMessage(text, PlayerId);
        SendMessage(EndGamePatch.KillLog, PlayerId);
    }
    */


    public static string GetSubRolesText(byte id, bool disableColor = false)
    {
        return GetPlayerById(id).GetDynamicName().GetComponentValue(UI.Subrole);
    }

    /*public static void ShowHelp()
    {
        SendMessage(
            GetString("CommandList")
            + $"\n/winner - {GetString("Command.winner")}"
            + $"\n/lastresult - {GetString("Command.lastresult")}"
            + $"\n/rename - {GetString("Command.rename")}"
            + $"\n/now - {GetString("Command.now")}"
            + $"\n/h now - {GetString("Command.h_now")}"
            + $"\n/h roles {GetString("Command.h_roles")}"
            + $"\n/h addons {GetString("Command.h_addons")}"
            + $"\n/h modes {GetString("Command.h_modes")}"
            + $"\n/dump - {GetString("Command.dump")}"
        );

    }*/

    public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "")
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (title == "") title = "<color=#aaaaff>" + Localizer.Get("Announcements.SystemMessage") + "</color>";
        ChatUpdatePatch.MessagesToSend.Add((text.RemoveHtmlTags(), sendTo, title));
    }

    public static PlayerControl? GetPlayerById(int playerId) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(pc => pc.PlayerId == playerId);

    public static string GetVoteName(byte num)
    {
        string name = "invalid";
        var player = GetPlayerById(num);
        if (num < 15 && player != null) name = player?.GetNameWithRole();
        if (num == 253) name = "Skip";
        if (num == 254) name = "None";
        if (num == 255) name = "Dead";
        return name;
    }

    public static string PadRightV2(this object text, int num)
    {
        int bc = 0;
        var t = text.ToString();
        foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
        return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
    }

    public static void DumpLog()
    {
        string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
        string filename =
            $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{TOHPlugin.PluginVersion}{(TOHPlugin.DevVersion ? TOHPlugin.DevVersionStr : "")}-{t}.log";
        FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
        file.CopyTo(@filename);
        System.Diagnostics.Process.Start(
            @$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
        if (PlayerControl.LocalPlayer != null)
            HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer,
                "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
    }

    /*public static string SummaryTexts(byte id, bool disableColor = true)
    {
        var RolePos = TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English ? 47 : 37;
        string summary =
            $"{ColorString(TOHPlugin.PlayerColors[id], TOHPlugin.AllPlayerNames[id])}<pos=22%> {GetProgressText(id)}</pos><pos=29%> {GetVitalText(id)}</pos><pos={RolePos}%> {GetDisplayRoleName(id)}{GetSubRolesText(id)}</pos>";
        return disableColor ? summary.RemoveHtmlTags() : summary;
    }*/

    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");

    public static void FlashColor(Color color, float duration = 1f)
    {
        var hud = DestroyableSingleton<HudManager>.Instance;
        if (hud.FullScreen == null) return;
        var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
        if (obj == null)
        {
            obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
            obj.name = "FlashColor_FullScreen";
        }

        hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
        {
            obj.SetActive(t != 1f);
            obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b,
                Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
        })));
    }

    public static Sprite LoadSprite(string path, float pixelsPerUnit = 100f)
    {
        Sprite sprite = null;
        try
        {
            var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray());
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f),
                pixelsPerUnit);
        }
        catch (Exception e)
        {
            VentLogger.Error($"Error Loading Asset: \"{path}\"", "LoadImage");
            VentLogger.Exception(e, "LoadImage");
        }

        return sprite;
    }

    public static AudioClip LoadAudioClip(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
    {
        // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity� to export)
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteAudio = new byte[stream.Length];
            _ = stream.Read(byteAudio, 0, (int)stream.Length);
            float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
            int offset;
            for (int i = 0; i < samples.Length; i++)
            {
                offset = i * 4;
                samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
            }

            int channels = 2;
            int sampleRate = 48000;
            AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }
        catch
        {
            System.Console.WriteLine("Error loading AudioClip from resources: " + path);
        }

        return null;

        /* Usage example:
        AudioClip exampleClip = Helpers.loadAudioClipFromResources("TownOfHost.assets.exampleClip.raw");
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        */
    }

    public static string ColorString(Color32 color, string str) =>
        $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";

    public static string GetOnOffColored(bool value) =>
        value ? Color.cyan.Colorize("ON") : Color.red.Colorize("OFF");
}