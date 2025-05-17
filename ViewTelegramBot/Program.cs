using Database;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ViewTelegramBot.Bot.Phrase;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Utils.Managers;
using Additions = ViewTelegramBot.Utils.Additions;

namespace ViewTelegramBot;

/// <summary>
/// Главный класс точки входа Telegram-бота.
/// </summary>
public class Program
{
    /// <summary>
    /// Путь к файлу ошибок.
    /// </summary>
    public const string ErrorFile = @"files/Error.log";

    /// <summary>
    /// Путь к файлу конфигурации.
    /// </summary>
    private const string ConfigFile = @"files/config.json";

    /// <summary>
    /// Менеджер конфигурации.
    /// </summary>
    public static readonly ManagerObject<Config?> ConfigManager = new ManagerObjectFile<Config?>(ConfigFile);

    /// <summary>
    /// Конфигурация приложения.
    /// </summary>
    public static Config? Config => ConfigManager.Obj;

    /// <summary>
    /// Локальная база данных.
    /// </summary>
    public static IDatabase? Local { get; private set; }

    /// <summary>
    /// Экземпляр Telegram-бота.
    /// </summary>
    private static Bot.Bot? _bot;

    /// <summary>
    /// Главная асинхронная точка входа приложения.
    /// </summary>
    /// <param name="args">Аргументы командной строки</param>
    public static async Task Main(string[] args)
    {
        await Load();
        await Init();

        _bot?.Start();
        Unload();
    }

    /// <summary>
    /// Инициализация приложения и зависимостей.
    /// </summary>
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

    /// <summary>
    /// Загрузка конфигурации и языковых фраз.
    /// </summary>
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

    /// <summary>
    /// Завершение работы приложения и освобождение ресурсов.
    /// </summary>
    private static void Unload()
    {
        Local?.End();
    }
}
