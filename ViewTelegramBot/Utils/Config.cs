namespace ViewTelegramBot.Utils;

public class Config
{
    public string UserNoHaveUsername { get; set; } = "noname";
    public bool SendRequestToAdmin { get; set; } = false;
    public string TelegramToken { get; set; } = string.Empty;
    public string MainDbHost { get; set; } = string.Empty;
    public int MainDbPort { get; set; } = 0;
    public string MainDbName { get; set; } = string.Empty;
    public string MainDbUser { get; set; } = string.Empty;
    public string MainDbPassword { get; set; } = string.Empty;
    public string ServerHost { get; set; } = string.Empty;
}