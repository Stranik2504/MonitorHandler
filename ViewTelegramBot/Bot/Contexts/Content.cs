using ViewTelegramBot.Bot.KeyboardUtls;

namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст текстового сообщения пользователя.
/// </summary>
public class Content
{
    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// Пользователь, отправивший сообщение.
    /// </summary>
    public User User { get; }

    /// <summary>
    /// Время отправки сообщения.
    /// </summary>
    public DateTime TimeSent { get; }

    /// <summary>
    /// Список документов, прикреплённых к сообщению.
    /// </summary>
    public List<Document?>? Documents { get; }

    /// <summary>
    /// Клавиатура, прикреплённая к сообщению.
    /// </summary>
    public Keyboard? Keyboard { get; set; }

    /// <summary>
    /// Сообщение, на которое был отправлен ответ.
    /// </summary>
    public Content? ReplyToContent { get; private set; }

    /// <summary>
    /// Создаёт новый контекст сообщения.
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="text">Текст сообщения</param>
    /// <param name="user">Пользователь</param>
    /// <param name="timeSent">Время отправки</param>
    /// <param name="documents">Документы</param>
    /// <param name="keyboard">Клавиатура</param>
    /// <param name="replyToContent">Сообщение-ответ</param>
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

    /// <summary>
    /// Устанавливает текст сообщения.
    /// </summary>
    /// <param name="text">Текст</param>
    public void SetText(string text) => Text = text;

    /// <summary>
    /// Устанавливает сообщение, на которое был отправлен ответ.
    /// </summary>
    /// <param name="content">Сообщение-ответ</param>
    public void SetReplyToMessage(Content content) => ReplyToContent = content;
}
