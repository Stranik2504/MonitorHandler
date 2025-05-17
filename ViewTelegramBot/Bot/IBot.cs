using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Bot;

/// <summary>
/// Интерфейс Telegram-бота для отправки сообщений, редактирования, удаления и работы с файлами.
/// </summary>
public interface IBot
{
    /// <summary>
    /// Отправляет сообщение или документ в чат.
    /// </summary>
    /// <param name="chat">Чат</param>
    /// <param name="content">Контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Отправленный контент</returns>
    Task<Content?> Send(Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Редактирует сообщение в чате.
    /// </summary>
    /// <param name="old">Старый контент</param>
    /// <param name="chat">Чат</param>
    /// <param name="content">Новый контент</param>
    /// <param name="parseMode">Режим парсинга</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Отредактированный контент</returns>
    Task<Content?> Edit(Content? old, Chat? chat, Content? content, ParseMode parseMode = ParseMode.Markdown, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Удаляет сообщение из чата.
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageId">ID сообщения</param>
    /// <param name="timeSent">Время отправки сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task Delete(long chatId, int messageId, DateTime timeSent, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Отвечает на callback-запрос.
    /// </summary>
    /// <param name="id">ID callback-запроса</param>
    /// <param name="text">Текст ответа</param>
    /// <param name="url">URL для перехода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task Answer(string id, string? text = null, string? url = null, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Получает поток файла по fileId.
    /// </summary>
    /// <param name="fileId">ID файла</param>
    /// <param name="destination">Поток назначения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task GetFileStream(string fileId, Stream destination, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Получает строку для упоминания пользователя.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="id">ID пользователя</param>
    /// <returns>Строка для пинга</returns>
    string GetPing(string username, long id);
}
