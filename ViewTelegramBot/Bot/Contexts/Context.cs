using Database;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;

namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст выполнения команды или события Telegram-бота.
/// </summary>
public class Context
{
    /// <summary>
    /// Экземпляр бота.
    /// </summary>
    private readonly IBot _bot;

    /// <summary>
    /// Флаг, был ли отправлен ответ на callback.
    /// </summary>
    private bool _answered;

    /// <summary>
    /// Контент текстового сообщения пользователя.
    /// </summary>
    public Content? Content { get; private set; }

    /// <summary>
    /// Callback-запрос пользователя.
    /// </summary>
    public Callback? Callback { get; }

    /// <summary>
    /// Контекст чата.
    /// </summary>
    public Chat Chat { get; }

    /// <summary>
    /// Тип события (текст, callback и т.д.).
    /// </summary>
    public TypeEvents Type { get; }

    /// <summary>
    /// Уровень доступа пользователя.
    /// </summary>
    public long Access { get; }

    /// <summary>
    /// Менеджер фраз для локализации.
    /// </summary>
    public PhrasesManager PhrasesManager { get; private set; }

    /// <summary>
    /// Токен отмены для асинхронных операций.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Состояние пользователя (FSM).
    /// </summary>
    public State? State { get; private set; }

    /// <summary>
    /// Состояние callback (FSM).
    /// </summary>
    public State? CallbackState { get; private set; }

    // TODO: Remove if not needed
    // public IDatabase Database { get; init; }

    /// <summary>
    /// Локальная база данных.
    /// </summary>
    public IDatabase Local { get; init; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public long UserId => Callback != null && Type == TypeEvents.Callback ? Callback.User.Id  : Content != null && Type == TypeEvents.Text ? Content.User.Id : -1;

    /// <summary>
    /// Текст сообщения или callback.
    /// </summary>
    public string Text => Callback != null && Type == TypeEvents.Callback ? Callback.Text ?? "" : Content != null && Type == TypeEvents.Text ? Content.Text : "";

    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    public int MessageId => Content?.Id ?? -1;

    /// <summary>
    /// Конструктор для текстового события.
    /// </summary>
    /// <param name="content">Контент</param>
    /// <param name="chat">Чат</param>
    /// <param name="access">Доступ</param>
    /// <param name="type">Тип события</param>
    /// <param name="bot">Бот</param>
    /// <param name="localDb">Локальная БД</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Context(
        Content content,
        Chat chat,
        long access,
        TypeEvents type,
        IBot bot,
        IDatabase localDb,
        // IDatabase mainDb,
        CancellationToken cancellationToken = default
    ) :
    this(
        null,
        content,
        chat,
        access,
        type,
        bot,
        localDb,
        // mainDb,
        cancellationToken
    ) {}

    /// <summary>
    /// Конструктор для любого события.
    /// </summary>
    /// <param name="callback">Callback</param>
    /// <param name="content">Контент</param>
    /// <param name="chat">Чат</param>
    /// <param name="access">Доступ</param>
    /// <param name="type">Тип события</param>
    /// <param name="bot">Бот</param>
    /// <param name="localDb">Локальная БД</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Context(
        Callback? callback,
        Content content,
        Chat chat,
        long access,
        TypeEvents type,
        IBot bot,
        IDatabase localDb,
        // IDatabase mainDb,
        CancellationToken cancellationToken = default
    )
    {
        Callback = callback;
        Content = content;
        Chat = chat;
        Access = access;
        Type = type;
        _bot = bot;
        Local = localDb;
        //Database = mainDb;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Загружает параметры состояния и менеджер фраз.
    /// </summary>
    public async Task LoadParams()
    {
        var state = await GetTextState();

        if (state != null)
            State = state;

        state = GetCallbackState();

        if (state != null)
            CallbackState = state;

        // Load PhrasesManager
        var result = await Local.GetUser(UserId);
        var lang = result != null && !string.IsNullOrWhiteSpace(result.Lang) ? result.Lang : "default";

        PhrasesManager = await PhrasesLoader.LoadPhrasesManager(lang);
    }

    /// <summary>
    /// Перезагружает менеджер фраз для текущего пользователя.
    /// </summary>
    public async Task ReloadPhraseManager()
    {
        var result = await Local.GetUser(UserId);
        var lang = result != null && !string.IsNullOrWhiteSpace(result.Lang) ? result.Lang : "default";

        PhrasesManager = await PhrasesLoader.LoadPhrasesManager(lang);
    }

    /// <summary>
    /// Получает состояние пользователя из текстового сообщения.
    /// </summary>
    /// <returns>Состояние или null</returns>
    private async Task<State?> GetTextState()
    {
        var state = await Local.GetState(UserId);

        if (string.IsNullOrWhiteSpace(state))
            return null;

        if (state.Split(":").Length < 2)
            return null;

        var result = new State
        {
            NameCommand = state.Split(":")[0],
            NameMethod = state.Split(":")[1]
        };

        result.Params = state.Replace($"{result.NameCommand}:{result.NameMethod}" + (state.Split(":").Length >= 3 ? ":" : ""), "");

        return result;
    }

    /// <summary>
    /// Получает состояние пользователя из callback.
    /// </summary>
    /// <returns>Состояние или null</returns>
    private State? GetCallbackState()
    {
        if (Callback?.Text == null || Type != TypeEvents.Callback)
            return null;

        if (Callback.Text.Split(":").Length < 2)
            return null;

        var result = new State
        {
            NameCommand = Callback.Text.Split(":")[0],
            NameMethod = Callback.Text.Split(":")[1]
        };

        result.Params = Callback.Text.Replace($"{result.NameCommand}:{result.NameMethod}" + (Callback.Text.Split(":").Length >= 3 ? ":" : ""), "");

        return result;
    }

    /// <summary>
    /// Устанавливает состояние пользователя.
    /// </summary>
    /// <param name="state">Строковое состояние</param>
    /// <returns>Успех операции</returns>
    public async Task<bool> SetState(string state) => await Local.SetState(UserId, state);

    /// <summary>
    /// Очищает состояние пользователя.
    /// </summary>
    public async Task ClearState() => await Local.ClearState(UserId);

    /// <summary>
    /// Добавляет параметр пользователю.
    /// </summary>
    /// <param name="nameParam">Имя параметра</param>
    /// <param name="param">Значение</param>
    /// <returns>Успех операции</returns>
    public async Task<bool> AddParam(string nameParam, string param) => await AddParam(UserId, nameParam, param);

    /// <summary>
    /// Добавляет параметр пользователю по id.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="nameParam">Имя параметра</param>
    /// <param name="param">Значение</param>
    /// <returns>Успех операции</returns>
    public async Task<bool> AddParam(long userId, string nameParam, string param) => await Local.AddParam(userId, nameParam, param);

    /// <summary>
    /// Получает параметры пользователя.
    /// </summary>
    /// <returns>Словарь параметров</returns>
    public async Task<(bool Success, Dictionary<string, string> Params)> GetParams() => await GetParams(UserId);

    /// <summary>
    /// Получает параметры пользователя по id.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Словарь параметров</returns>
    public async Task<(bool Success, Dictionary<string, string> Params)> GetParams(long userId) => await Local.GetParams(userId);

    /// <summary>
    /// Очищает параметры пользователя.
    /// </summary>
    public async Task ClearParams() => await ClearParams(UserId);

    /// <summary>
    /// Очищает параметры пользователя по id.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    public async Task ClearParams(long userId) => await Local.ClearParams(userId);

    /// <summary>
    /// Отправляет сообщение с текстом и клавиатурой.
    /// </summary>
    /// <param name="param">Пара (текст, клавиатура)</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send((string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(param.Text, param.Keyboard, parseMode, saveContent);

    /// <summary>
    /// Отправляет сообщение с текстом и клавиатурой.
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="keyboard">Клавиатура</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send(string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(new Content(MessageId, text, Content?.User ?? User.DefaultUser, Content?.TimeSent ?? DateTime.Now, default, keyboard), parseMode, saveContent);

    /// <summary>
    /// Отправляет сообщение с текстом, документами и клавиатурой.
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="documents">Документы</param>
    /// <param name="keyboard">Клавиатура</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send(string text, List<Document?>? documents, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(new Content(MessageId, text, User.DefaultUser, Content?.TimeSent ?? DateTime.Now, documents, keyboard), parseMode, saveContent);

    /// <summary>
    /// Отправляет контент в чат.
    /// </summary>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send(Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(Chat, content, parseMode, saveContent);

    /// <summary>
    /// Отправляет контент в указанный чат.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отправленный контент</returns>
    public async Task<Content?> Send(Chat chat, Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true)
    {
        /*if (parseMode == ParseMode.MarkdownV2)
            content.SetText(content.Text.ConvertToMark2());*/

        var newContent = await _bot.Send(chat, content, parseMode, CancellationToken);

        if (saveContent)
            Content = newContent;

        return newContent;
    }

    /// <summary>
    /// Редактирует сообщение с текстом и клавиатурой.
    /// </summary>
    /// <param name="param">Пара (текст, клавиатура)</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit((string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(param.Text, param.Keyboard, parseMode, saveContent);

    /// <summary>
    /// Редактирует сообщение по id с текстом и клавиатурой.
    /// </summary>
    /// <param name="messageId">ID сообщения</param>
    /// <param name="param">Пара (текст, клавиатура)</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(int messageId, (string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(messageId, param.Text, param.Keyboard, parseMode, saveContent);

    /// <summary>
    /// Редактирует сообщение с текстом и клавиатурой.
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="keyboard">Клавиатура</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(MessageId, text, keyboard, parseMode, saveContent);

    /// <summary>
    /// Редактирует сообщение по id с текстом и клавиатурой.
    /// </summary>
    /// <param name="messageId">ID сообщения</param>
    /// <param name="text">Текст</param>
    /// <param name="keyboard">Клавиатура</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(int messageId, string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(new Content(messageId, text, User.DefaultUser, Content?.TimeSent ?? DateTime.Now, new List<Document?>(), keyboard), parseMode, saveContent);

    /// <summary>
    /// Редактирует сообщение с контентом.
    /// </summary>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(Chat, content, parseMode, saveContent);

    /// <summary>
    /// Редактирует сообщение в указанном чате.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="saveContent">Сохранять ли контент</param>
    /// <returns>Отредактированный контент</returns>
    public async Task<Content?> Edit(Chat chat, Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true)
    {
        Content? newContent;

        /*if (parseMode == ParseMode.MarkdownV2)
            content.SetText(content.Text.ConvertToMark2());*/

        if (Content == null || Content.Id == -1)
            newContent = await Send(content, parseMode);
        else
            newContent = await _bot.Edit(Content, chat, content, parseMode, CancellationToken);

        if (saveContent)
            Content = newContent;

        return newContent;
    }

    /// <summary>
    /// Удаляет текущее сообщение.
    /// </summary>
    public async Task Delete() => await _bot.Delete(Chat.Id, MessageId, Content?.TimeSent ?? DateTime.Now, CancellationToken);

    /// <summary>
    /// Удаляет сообщение по id.
    /// </summary>
    /// <param name="messageId">ID сообщения</param>
    public async Task Delete(int messageId) => await Delete(Chat.Id, messageId);

    /// <summary>
    /// Удаляет сообщение по chatId и messageId.
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageId">ID сообщения</param>
    public async Task Delete(long chatId, int messageId) => await _bot.Delete(chatId, messageId, Content?.TimeSent ?? DateTime.Now, CancellationToken);

    /// <summary>
    /// Отвечает на callback-запрос.
    /// </summary>
    /// <param name="text">Текст ответа</param>
    /// <param name="url">URL для перехода</param>
    public async Task Answer(string? text = null, string? url = null)
    {
        if (Callback != null && !_answered && Type == TypeEvents.Callback)
            await _bot.Answer(Callback.Id, text, url, CancellationToken);

        _answered = true;
    }

    /// <summary>
    /// Получает поток файла по id документа.
    /// </summary>
    /// <param name="destination">Поток назначения</param>
    public async Task GetFileStream(Stream destination) => await GetFileStream(Content?.Documents?.FirstOrDefault()?.FileId, destination);

    /// <summary>
    /// Получает поток файла по fileId.
    /// </summary>
    /// <param name="fileId">ID файла</param>
    /// <param name="destination">Поток назначения</param>
    public async Task GetFileStream(string? fileId, Stream? destination)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return;

        if (destination == null)
            return;

        await _bot.GetFileStream(fileId, destination, CancellationToken);
    }

    /// <summary>
    /// Получает строку для упоминания пользователя.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Строка для пинга</returns>
    public string GetPing(string username, long userId) => _bot.GetPing(username, userId);

    /// <summary>
    /// Получает пользователя из локальной базы данных.
    /// </summary>
    /// <returns>Пользователь</returns>
    public Task<Utils.User?> GetUser() => Local.GetUser(UserId);
}
