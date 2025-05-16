namespace ViewTelegramBot.Bot.Contexts;

public class Document
{
    public string Filename { get; protected set; }
    public string FileId { get; protected set; }
    public long? FileSize { get; protected set; }

    public Document(string filename, string fileId, long? fileSize) => (Filename, FileId, FileSize) = (filename, fileId, fileSize);
}