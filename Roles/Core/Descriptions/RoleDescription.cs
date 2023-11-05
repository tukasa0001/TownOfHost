using System.Text;

namespace TownOfHost.Roles.Core.Descriptions;

public abstract class RoleDescription
{
    public RoleDescription(SimpleRoleInfo roleInfo)
    {
        RoleInfo = roleInfo;
    }

    public SimpleRoleInfo RoleInfo { get; }
    /// <summary>イントロなどで表示される短い文</summary>
    public abstract string Blurb { get; }
    /// <summary>
    /// ヘルプコマンドで使用される長い説明文<br/>
    /// AmongUs2023.7.12時点で，Impostor, Crewmateに関してはバニラ側でロング説明文が未実装のため「タスクを行う」と表示される
    /// </summary>
    public abstract string Description { get; }
    public string FullFormatHelp
    {
        get
        {
            var builder = new StringBuilder(256);
            // 役職名と説明文
            builder.AppendFormat("<size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", FirstHeaderSize, Translator.GetRoleString(RoleInfo.RoleName.ToString()).Color(RoleInfo.RoleColor.ToReadableColor()));
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Description);
            // 陣営
            builder.AppendFormat("<size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", SecondHeaderSize, Translator.GetString("Team"));
            //   マッドメイトはインポスター陣営
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Translator.GetString($"CustomRoleTypes.{roleTeam}"));
            // バニラ役職判定
            builder.AppendFormat("<size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", SecondHeaderSize, Translator.GetString("Basis"));
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Translator.GetString(RoleInfo.BaseRoleType.Invoke().ToString()));
            return builder.ToString();
        }
    }

    public const string FirstHeaderSize = "130%";
    public const string SecondHeaderSize = "100%";
    public const string BodySize = "70%";
    public const string BlankLineSize = "30%";
}
