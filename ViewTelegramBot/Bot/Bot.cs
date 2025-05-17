using Mysqlx.Crud;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;
using ViewTelegramBot.Commands;
using ViewTelegramBot.Utils;
using Chat = ViewTelegramBot.Bot.Contexts.Chat;
using Document = ViewTelegramBot.Bot.Contexts.Document;
using ParseMode = ViewTelegramBot.Utils.ParseMode;
using Update = Telegram.Bot.Types.Update;
using User = ViewTelegramBot.Bot.Contexts.User;

namespace ViewTelegramBot.Bot;

/// <summary>
/// Класс Telegram-бота, реализующий интерфейс IBot.
/// </summary>
public class Bot(string token) : IBot
{
    /// <summary>
    /// Событие для управления жизненным циклом бота.
    /// </summary>
    private readonly ManualResetEvent _active = new(false);

    /// <summary>
    /// Клиент Telegram Bot API.
    /// </summary>
    private readonly ITelegramBotClient _bot = new TelegramBotClient(token);

    /// <summary>
    /// Запускает цикл обработки обновлений Telegram-бота.
    /// </summary>
    public void Start()
    {
        var cts = new CancellationTokenSource();
        var receiverOpt = new ReceiverOptions
        {
            DropPendingUpdates = true
        };

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions: receiverOpt,
            cancellationToken: cts.Token
        );

        _active.WaitOne();
        cts.Cancel();
    }

    /// <summary>
    /// Останавливает цикл обработки обновлений Telegram-бота.
    /// </summary>
    public void Stop()
    {
        _active.Reset();
    }

    /// <summary>
    /// Обрабатывает входящее обновление Telegram.
    /// </summary>
    /// <param name="bot">Клиент Telegram Bot API</param>
    /// <param name="update">Обновление</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await OnMessage(update.Message, cancellationToken);
                break;
            case UpdateType.CallbackQuery:
                await OnCallbackQuery(update.CallbackQuery, cancellationToken);
                break;
            default:
                await Send(
                    new Chat(update.Message?.Chat.Id ?? -1),
                    new Content(0, (await PhrasesLoader.LoadPhrasesManager("default"))["in_process"], User.DefaultUser, DateTime.Now)
                );
                break;
        }

    }

    /// <summary>
    /// Обрабатывает входящее текстовое сообщение.
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task OnMessage(Message? message, CancellationToken? cancellationToken = null)
    {
        if (message == null)
            return;

        var ctx = new Context(
            new Content(
                message.MessageId,
                message.Text,
                new User(
                    message.From?.Id ?? -1,
                    message.From?.Username,
                    message.From?.FirstName,
                    message.From?.LastName,
                    message.From?.IsBot ?? false
                ),
                message.Date,
                message.Document?.Convert().ToList()
            ),
            new Chat(message.Chat.Id),
            0,
            TypeEvents.Text,
            this,
            Program.Local,
            cancellationToken: cancellationToken ?? CancellationToken.None
        );

        if (ctx.Content != null && message.ReplyMarkup != null)
            ctx.Content.Keyboard = message.ReplyMarkup.ReGenerate();

        await Command.ExecuteAsync(ctx);
    }

    /// <summary>
    /// Обрабатывает входящий callback-запрос.
    /// </summary>
    /// <param name="callbackQuery">Callback-запрос</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task OnCallbackQuery(CallbackQuery? callbackQuery, CancellationToken? cancellationToken = null)
    {
        if (callbackQuery == null)
            return;

        var ctx = new Context(
            new Callback(
                callbackQuery.Id,
                callbackQuery.Data,
                new User(
                    callbackQuery.From.Id,
                    callbackQuery.From.Username,
                    callbackQuery.From.FirstName,
                    callbackQuery.From.LastName,
                    callbackQuery.From.IsBot
                )
            ),
            new Content(
                callbackQuery.Message?.MessageId ?? -1,
                callbackQuery.Message?.Text,
                new User(
                    callbackQuery.Message?.From?.Id ?? -1,
                    callbackQuery.Message?.From?.Username,
                    callbackQuery.Message?.From?.FirstName,
                    callbackQuery.Message?.From?.LastName,
                    callbackQuery.Message?.From?.IsBot ?? false
                ),
                callbackQuery.Message?.Date ?? DateTime.Now,
                callbackQuery.Message?.Document?.Convert().ToList()
            ),
            new Chat(callbackQuery.Message?.Chat.Id ?? -1),
            0,
            TypeEvents.Callback,
            this,
            Program.Local,
            cancellationToken: cancellationToken ?? CancellationToken.None
        );

        if (ctx.Content != null && callbackQuery.Message?.ReplyMarkup != null)
            ctx.Content.Keyboard = callbackQuery.Message.ReplyMarkup.ReGenerate();

        await Command.ExecuteAsync(ctx);
    }

    /// <summary>
    /// Обрабатывает ошибку при опросе Telegram API.
    /// </summary>
    /// <param name="bot">Клиент Telegram Bot API</param>
    /// <param name="exception">Исключение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    private static Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        exception.Log();
        Environment.Exit(1);
        return null;
    }

    /// <summary>
    /// Отправляет сообщение или документ в чат.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send(Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = null)
    {
        if (content == null || chat == null || chat.Id == -1)
            return null;

        if (content.Documents == null || content.Documents.Count == 0)
        {
            var mess = await _bot.SendMessage(
                chatId: chat.Id,
                text: content.Text,
                replyMarkup: content.Keyboard?.Generate(),
                parseMode: ConvertParseMode(parseMode) ?? Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken ?? CancellationToken.None
            );

            return Convert(mess);
        }

        Content? result = null;

        foreach (var document in content.Documents.OfType<Document>())
        {
            var stream = document is TextDocument doc ? doc.Stream : File.Open(document.FileId, FileMode.Open);

            var mess = await _bot.SendDocument(
                chatId: chat.Id,
                document: new InputFileStream(stream, document.Filename),
                caption: content.Text,
                cancellationToken: cancellationToken ?? CancellationToken.None,
                replyMarkup: content.Keyboard?.Generate()
            );

            await stream.DisposeAsync();
            result = Convert(mess);
        }

        return result;
    }

    /// <summary>
    /// Редактирует сообщение в чате.
    /// </summary>
    /// <param name="old">Старый контент</param>
    /// <param name="chat">Чат</param>
    /// <param name="content">Новый контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(Content? old, Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = null)
    {
        if (content == null)
            return null;

        if (chat == null || chat.Id == -1)
            return null;

        Content? result = null;
        Keyboard? keyboard = null;

        if (old?.Text != content.Text && !string.IsNullOrWhiteSpace(content.Text))
        {
            var mess = await _bot.EditMessageText(
                chatId: chat.Id,
                messageId: content.Id,
                text: content.Text,
                parseMode: ConvertParseMode(parseMode) ?? Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken ?? CancellationToken.None
            );

            result = Convert(mess);
        }

        if (result != null)
            keyboard = result.Keyboard;
        else if (old != null)
            keyboard = old.Keyboard;

        if ((keyboard == null && content.Keyboard != null) ||
            (keyboard != null && content.Keyboard == null) ||
            (keyboard != null && content.Keyboard != null && !keyboard.Equals(content.Keyboard)))
        {
            var mess = await _bot.EditMessageReplyMarkup(
                chatId: chat.Id,
                messageId: result?.Id ?? content.Id,
                replyMarkup: content.Keyboard?.Generate(),
                cancellationToken: cancellationToken ?? CancellationToken.None
            );

            result = Convert(mess);
        }

        return result;
    }

    /// <summary>
    /// Удаляет сообщение из чата.
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageId">ID сообщения</param>
    /// <param name="timeSent">Время отправки сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task Delete(long chatId, int messageId, DateTime timeSent, CancellationToken? cancellationToken = null)
    {
        if ((DateTime.Now - timeSent).TotalDays >= 2)
            return;

        if (chatId == -1 || messageId == -1)
            return;

        try
        {
            await _bot.DeleteMessage(
                chatId: chatId,
                messageId: messageId,
                cancellationToken: cancellationToken ?? CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    /// <summary>
    /// Отвечает на callback-запрос.
    /// </summary>
    /// <param name="id">ID callback-запроса</param>
    /// <param name="text">Текст ответа</param>
    /// <param name="url">URL для перехода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task Answer(string id, string? text = null, string? url = null, CancellationToken? cancellationToken = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        await _bot.AnswerCallbackQuery(
            callbackQueryId: id,
            text: text,
            url: url,
            cancellationToken: cancellationToken ?? CancellationToken.None
        );
    }

    /// <summary>
    /// Получает поток файла по fileId.
    /// </summary>
    /// <param name="fileId">ID файла</param>
    /// <param name="destination">Поток назначения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task GetFileStream(string fileId, Stream destination, CancellationToken? cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId))
             return;

        await _bot.GetInfoAndDownloadFile(
            fileId,
            destination,
            cancellationToken ?? CancellationToken.None
        );
    }

    /// <summary>
    /// Получает строку для упоминания пользователя.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="id">ID пользователя</param>
    /// <returns>Строка для пинга</returns>
    public string GetPing(string username, long id) => $"[@{(string.IsNullOrWhiteSpace(username) ? Program.Config?.UserNoHaveUsername : username)}](tg://user?id={id})";

    /// <summary>
    /// Преобразует объект Message в Content.
    /// </summary>
    /// <param name="message">Сообщение Telegram</param>
    /// <returns>Контент</returns>
    private static Content? Convert(Message? message)
    {
        if (message == null)
            return null;

        var result = new Content(
            message.MessageId,
            message.Text,
            new User(
                message.From?.Id ?? -1,
                message.From?.Username,
                message.From?.FirstName,
                message.From?.LastName,
                message.From?.IsBot ?? false
            ),
            message.Date,
            message.Document?.Convert().ToList(),
            message.ReplyMarkup?.ReGenerate()
        );

        if (message.ReplyToMessage != null)
        {
            result.SetReplyToMessage(new Content(
                message.ReplyToMessage.MessageId,
                message.ReplyToMessage.Text,
                new User(
                    message.ReplyToMessage.From?.Id ?? -1,
                    message.ReplyToMessage.From?.Username,
                    message.ReplyToMessage.From?.FirstName,
                    message.ReplyToMessage.From?.LastName,
                    message.ReplyToMessage.From?.IsBot ?? false
                ),
                message.ReplyToMessage.Date,
                message.ReplyToMessage.Document?.Convert().ToList(),
                message.ReplyMarkup?.ReGenerate()
            ));
        }

        return result;
    }

    /// <summary>
    /// Преобразует режим парсинга в Telegram-формат.
    /// </summary>
    /// <param name="parseMode">Режим парсинга</param>
    /// <returns>Режим Telegram</returns>
    private static Telegram.Bot.Types.Enums.ParseMode? ConvertParseMode(ParseMode parseMode) => parseMode switch
    {
        ParseMode.Markdown => Telegram.Bot.Types.Enums.ParseMode.Markdown,
        ParseMode.MarkdownV2 => Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
        ParseMode.Html => Telegram.Bot.Types.Enums.ParseMode.Html,
        _ => default
    };
}
