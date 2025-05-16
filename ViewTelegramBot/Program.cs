using Database;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ViewTelegramBot.Bot.Phrase;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Utils.Managers;
using Additions = ViewTelegramBot.Utils.Additions;

namespace ViewTelegramBot;

public class Program
{
    public const string ErrorFile = @"files/Error.log";
    private const string ConfigFile = @"files/config.json";

    public static readonly ManagerObject<Config?> ConfigManager = new ManagerObjectFile<Config?>(ConfigFile);
    public static Config? Config => ConfigManager.Obj;

    public static IDatabase? Local { get; private set; }

    private static Bot.Bot? _bot;


    public static async Task Main(string[] args)
    {
        await Load();
        await Init();

        _bot?.Start();
        Unload();
    }

    private static async Task Init()
    {
        if (Config is null)
            return;

        Local = new Database.MySql(
            Config.MainDbHost,
            Config.MainDbPort,
            Config.MainDbName,
            Config.MainDbUser,
            Config.MainDbPassword,
            new SerilogLoggerFactory(Log.Logger).CreateLogger<Database.MySql>()
        );

        _bot = new Bot.Bot(Config.TelegramToken);

        Local.Start();

        if (await Local.CreateLocalTables())
            throw new Exception("Local tables didn't create");
    }

    private static async Task Load()
    {
        if (!Directory.Exists("files"))
            Directory.CreateDirectory("files");

        Additions.CreateLogger();
        var logger = Additions.GetLogger();

        if (logger != null)
            Log.Logger = logger;

        await PhrasesLoader.Load();
        await ConfigManager.Load();
    }

    private static void Unload()
    {
        Local?.End();
    }
}