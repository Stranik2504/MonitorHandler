using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Bot;

public interface IBot
{
    Task<Content?> Send(Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = default);
    Task<Content?> Edit(Content? old, Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = default);
    Task Delete(long chatId, int messageId, DateTime timeSent, CancellationToken? cancellationToken = null);
    Task Answer(string id, string? text = null, string? url = null, CancellationToken? cancellationToken = null);
    Task GetFileStream(string fileId, Stream destination, CancellationToken? cancellationToken = null);
    string GetPing(string username, long id);
}