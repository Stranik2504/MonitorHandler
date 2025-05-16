using Database;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;

namespace ViewTelegramBot.Bot.Contexts;

public class Context
{
    private readonly IBot _bot;
    private bool _answered;

    public Content? Content { get; private set; }
    public Callback? Callback { get; }
    public Chat Chat { get; }
    public TypeEvents Type { get; }
    public long Access { get; }
    public PhrasesManager PhrasesManager { get; private set; }
    public CancellationToken CancellationToken { get; }

    public State? State { get; private set; }
    public State? CallbackState { get; private set; }

    // TODO: Remove if not needed
    // public IDatabase Database { get; init; }
    public IDatabase Local { get; init; }

    public long UserId => Callback != null && Type == TypeEvents.Callback ? Callback.User.Id  : Content != null && Type == TypeEvents.Text ? Content.User.Id : -1;

    public string Text => Callback != null && Type == TypeEvents.Callback ? Callback.Text ?? "" : Content != null && Type == TypeEvents.Text ? Content.Text : "";

    public int MessageId => Content?.Id ?? -1;

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

    public async Task ReloadPhraseManager()
    {
        var result = await Local.GetUser(UserId);
        var lang = result != null && !string.IsNullOrWhiteSpace(result.Lang) ? result.Lang : "default";

        PhrasesManager = await PhrasesLoader.LoadPhrasesManager(lang);
    }

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

    public async Task<bool> SetState(string state) => await Local.SetState(UserId, state);

    public async Task ClearState() => await Local.ClearState(UserId);

    public async Task<bool> AddParam(string nameParam, string param) => await AddParam(UserId, nameParam, param);

    public async Task<bool> AddParam(long userId, string nameParam, string param) => await Local.AddParam(userId, nameParam, param);

    public async Task<(bool Success, Dictionary<string, string> Params)> GetParams() => await GetParams(UserId);

    public async Task<(bool Success, Dictionary<string, string> Params)> GetParams(long userId) => await Local.GetParams(userId);

    public async Task ClearParams() => await ClearParams(UserId);

    public async Task ClearParams(long userId) => await Local.ClearParams(userId);

    public async Task<Content?> Send((string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(param.Text, param.Keyboard, parseMode, saveContent);

    public async Task<Content?> Send(string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(new Content(MessageId, text, Content?.User ?? User.DefaultUser, Content?.TimeSent ?? DateTime.Now, default, keyboard), parseMode, saveContent);

    public async Task<Content?> Send(string text, List<Document?>? documents, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(new Content(MessageId, text, User.DefaultUser, Content?.TimeSent ?? DateTime.Now, documents, keyboard), parseMode, saveContent);

    public async Task<Content?> Send(Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Send(Chat, content, parseMode, saveContent);

    public async Task<Content?> Send(Chat chat, Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true)
    {
        /*if (parseMode == ParseMode.MarkdownV2)
            content.SetText(content.Text.ConvertToMark2());*/

        var newContent = await _bot.Send(chat, content, parseMode, CancellationToken);

        if (saveContent)
            Content = newContent;

        return newContent;
    }

    public async Task<Content?> Edit((string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(param.Text, param.Keyboard, parseMode, saveContent);

    public async Task<Content?> Edit(int messageId, (string Text, Keyboard Keyboard) param, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(messageId, param.Text, param.Keyboard, parseMode, saveContent);

    public async Task<Content?> Edit(string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(MessageId, text, keyboard, parseMode, saveContent);

    public async Task<Content?> Edit(int messageId, string text, Keyboard? keyboard = null, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(new Content(messageId, text, User.DefaultUser, Content?.TimeSent ?? DateTime.Now, new List<Document?>(), keyboard), parseMode, saveContent);

    public async Task<Content?> Edit(Content content, ParseMode parseMode = ParseMode.Markdown, bool saveContent = true) => await Edit(Chat, content, parseMode, saveContent);

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

    public async Task Delete() => await _bot.Delete(Chat.Id, MessageId, Content?.TimeSent ?? DateTime.Now, CancellationToken);

    public async Task Delete(int messageId) => await Delete(Chat.Id, messageId);

    public async Task Delete(long chatId, int messageId) => await _bot.Delete(chatId, messageId, Content?.TimeSent ?? DateTime.Now, CancellationToken);

    public async Task Answer(string? text = null, string? url = null)
    {
        if (Callback != null && !_answered && Type == TypeEvents.Callback)
            await _bot.Answer(Callback.Id, text, url, CancellationToken);

        _answered = true;
    }

    public async Task GetFileStream(Stream destination) => await GetFileStream(Content?.Documents?.FirstOrDefault()?.FileId, destination);

    public async Task GetFileStream(string? fileId, Stream? destination)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return;

        if (destination == null)
            return;

        await _bot.GetFileStream(fileId, destination, CancellationToken);
    }

    public string GetPing(string username, long userId) => _bot.GetPing(username, userId);

    public Task<Utils.User?> GetUser() => Local.GetUser(UserId);
}