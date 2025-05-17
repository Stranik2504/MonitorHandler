namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Документ, прикреплённый к сообщению.
/// </summary>
public class Document
{
    /// <summary>
    /// Имя файла.
    /// </summary>
    public string Filename { get; protected set; }

    /// <summary>
    /// Идентификатор файла в Telegram.
    /// </summary>
    public string FileId { get; protected set; }

    /// <summary>
    /// Размер файла.
    /// </summary>
    public long? FileSize { get; protected set; }

    /// <summary>
    /// Создаёт новый документ.
    /// </summary>
    /// <param name="filename">Имя файла</param>
    /// <param name="fileId">Идентификатор файла</param>
    /// <param name="fileSize">Размер файла</param>
    public Document(string filename, string fileId, long? fileSize) => (Filename, FileId, FileSize) = (filename, fileId, fileSize);
}
