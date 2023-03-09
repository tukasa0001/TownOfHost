using System.IO;
using TownOfHost.Managers;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Managers;

public class PluginDataManager
{
    public const string DataDirectory = "./TOHTOR_DATA";
    public const string TemplateFile = "Template.txt";
    public const string WordListFile = "BannedWords.txt";

    private DirectoryInfo dataDirectory;
    public TemplateManager TemplateManager;
    public ChatManager ChatManager;

    public PluginDataManager()
    {
        dataDirectory = new DirectoryInfo(DataDirectory);
        if (!dataDirectory.Exists) dataDirectory.Create();
        TemplateManager = new TemplateManager(dataDirectory.GetFile(TemplateFile));
        ChatManager = new ChatManager(dataDirectory.GetFile(WordListFile));
    }
}