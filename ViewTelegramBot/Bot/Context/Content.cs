using ViewTelegramBot.Bot.KeyboardUtls;

namespace ViewTelegramBot.Bot.Context;

public class Content
{
    public int Id { get; }
    public string Text { get; private set; }
    public User User { get; }
    public DateTime TimeSent { get; }
    public List<Document?>? Documents { get; }
    public Keyboard? Keyboard { get; set; }
    public Content? ReplyToContent { get; private set; }

    public Content(int id, string? text, User user, DateTime timeSent = default, List<Document?>? documents = default, Keyboard? keyboard = default, Content? replyToContent = default)
    {
        if (timeSent == default)
            timeSent = DateTime.Now;

        Id = id;
        Text = text ?? "";
        User = user;
        TimeSent = timeSent;
        Documents = documents;
        Keyboard = keyboard;
        ReplyToContent = replyToContent;
    }

    public void SetText(string text) => Text = text;

    public void SetReplyToMessage(Content content) => ReplyToContent = content;
}