namespace ViewTelegramBot.Bot.Context;

public class Callback
{
    public string Id { get; }
    public string? Text { get; private set; }
    public User User { get; set; }

    public Callback(string id, string? text, User user) => (Id, Text, User) = (id, text ?? "", user);

    public void SetText(string text) => Text = text;
}