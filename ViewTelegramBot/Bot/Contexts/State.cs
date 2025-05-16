namespace ViewTelegramBot.Bot.Contexts;

public class State
{
    public string NameCommand { get; set; } = "";
    public string? NameMethod { get; set; }
    public string? Params { get; set; }
}