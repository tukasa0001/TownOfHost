using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Managers;

public class ChatManager
{
    private readonly FileInfo filterFile;
    internal List<String> BannedWords;

    public ChatManager(FileInfo filterFile)
    {
        this.filterFile = filterFile;
        BannedWords = this.filterFile.ReadAll(true).Split("\n").Where(s => s.Length > 2).ToList();
    }

    public bool HasBannedWord(string message)
    {
        return BannedWords.Any(pattern => Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase));
    }

    public void Reload()
    {
        BannedWords = this.filterFile.ReadAll(true).Split("\n").Where(s => s.Length > 2).ToList();
    }

    public void AddWord(string word)
    {
        BannedWords.Add(word);
        File.WriteAllText(this.filterFile.FullName, String.Join("\n", BannedWords));
    }
}