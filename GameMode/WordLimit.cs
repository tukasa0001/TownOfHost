using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TownOfHostForE.Attributes;
using TownOfHostForE.Roles.AddOns.Common;

namespace TownOfHostForE.GameMode;

public static class WordLimit
{
    public static readonly int Id = 230000;

    private static string LimitWord = "";

    public static List<string> nowSafeWords = new ();

    public enum regulation
    {
        None = 0,
        HiraganaLimit,
        KatanakaLimit,
        EnglishLimit,
        SetWordOnly
    }


    [GameModuleInitializer]
    public static void GameInit()
    {
        nowSafeWords.Clear ();
    }
    public static void OnReceiveChat(PlayerControl player, string text)
    {
        //ロビーでは適用しない
        if (GameStates.IsLobby) return;

        //最初の会議は対象外
        if (MeetingStates.FirstMeeting) return;

        //既に死んでたら対象外
        if (!player.IsAlive()) return;

        //レギュ取得
        regulation nowRegulation = Options.GetWordLimitMode();

        if (nowRegulation == regulation.None) return;

        //コマンドの呼び出しは対象外
        if (text[0] == '/') return;

        //システム的なメッセージは対象外
        if (!CheckSafeWord(text)) return;

        //中二病特例
        if (Chu2Byo.CheckCh2Words(player,text)) return;

        //伸ばし棒は判定しないため置換する
        text = text.Replace("ー", "");

        CheckWords(nowRegulation,player,text);
    }

    private static bool CheckSafeWord(string text)
    {
        if (nowSafeWords.Contains(text))
        {
            nowSafeWords.Remove(text);
            return false;
        }

        return true;
    }

    private static void CheckWords(regulation nowRegulation,PlayerControl target,string CheckText)
    {
        switch(nowRegulation)
        {
            case regulation.HiraganaLimit:
                IsHiraganaLimit(target,CheckText);
                break;
            case regulation.KatanakaLimit:
                IsKatakanaLimit(target, CheckText);
                break;
            case regulation.EnglishLimit:
                IsEnglishLimit(target, CheckText);
                break;
            case regulation.SetWordOnly:
                IsSetWordLimit(target, CheckText);
                break;
            default: break;
        }
    }
    public static void IsFirstMeetingCheck(regulation nowRegulation)
    {
        switch(nowRegulation)
        {
            case regulation.HiraganaLimit:
                Utils.SendMessage("現在平仮名が禁止されています。発言にはご注意を。");
                break;
            case regulation.KatanakaLimit:
                Utils.SendMessage("現在カタカナが禁止されています。発言にはご注意を。");
                break;
            case regulation.EnglishLimit:
                Utils.SendMessage("現在アルファベットが禁止されています。発言にはご注意を。");
                break;
            case regulation.SetWordOnly:
                Utils.SendMessage($"発言する際は「{LimitWord}」を含めて話すよう注意してください。");
                break;
            default: break;
        }
    }
    static void IsHiraganaLimit(PlayerControl target,string str)
    {
        var results = Regex.Matches(str, @"\p{IsHiragana}+");

        if (results.Count() > 0)
        {
            //target.RpcMurderPlayer(target);
            LimitOutDeath(target);
            Utils.SendMessage($"{target.name}は平仮名を発言したため処刑されました。");
        }
    }

    static void IsKatakanaLimit(PlayerControl target, string str)
    {
        if (Regex.IsMatch(str, @"[\p{IsKatakana}]+"))
        {
            //target.RpcMurderPlayer(target);
            LimitOutDeath(target);
            Utils.SendMessage($"{target.name}はカタカナを発言したため処刑されました。");
        }
    }
    public static void IsEnglishLimit(PlayerControl pc, string str)
    {
        // 指定した文字列がアルファベットかどうかを判定します。
        if (string.IsNullOrEmpty(str)) return;

        if (Regex.IsMatch(str, "[A-Za-z]"))
        {
            //pc.RpcMurderPlayer(pc);
            LimitOutDeath(pc);
            Utils.SendMessage($"{pc.name}はアルファベットを発言したため処刑されました。");
        }
    }

    public static void SetLimitWord(string word)
    {
        LimitWord = word;
    }

    private static void IsSetWordLimit(PlayerControl target,string word)
    {
        if (!word.Contains(LimitWord))
        {
            //target.RpcMurderPlayer(target);
            LimitOutDeath(target);
            Utils.SendMessage($"{target.name}は「{LimitWord}」を発言しなかったため処刑されました。");
        }
    }

    private static void LimitOutDeath(PlayerControl target)
    {
        //ホストだけ死ぬ前のセリフが見えない不具合修正
        new LateTask(() =>
        {
            //キルではなくつる(死体発生対策)
            Utils.REIKAITENSOU(target.PlayerId,CustomDeathReason.wordLimit);
            FixedUpdatePatch.LoversSuicide(target.PlayerId, true);
        }, 0.25f, "WordLimitDeath");
    }
}
