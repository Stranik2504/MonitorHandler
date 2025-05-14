namespace ViewTelegramBot.Bot.KeyboardUtls;

public class Button
{
    public string Text { get; set; }
    public string? Payload { get; set; }

    public Button(string text, string payload) => (Text, Payload) = (text, payload);

    public Button(string text) => Text = text;
}