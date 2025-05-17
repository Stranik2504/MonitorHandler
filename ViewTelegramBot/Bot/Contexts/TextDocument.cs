using System.Text;

namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Документ с текстовым содержимым.
/// </summary>
public class TextDocument : Document
{
    /// <summary>
    /// Поток с содержимым документа.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Создаёт новый текстовый документ на основе потока.
    /// </summary>
    /// <param name="stream">Поток</param>
    /// <param name="filename">Имя файла</param>
    public TextDocument(Stream stream, string filename) : base(filename, filename, stream.Length) => Stream = stream;

    /// <summary>
    /// Создаёт новый текстовый документ на основе строки.
    /// </summary>
    /// <param name="text">Текст</param>
    /// <param name="filename">Имя файла</param>
    public TextDocument(string text, string filename) : base(filename, filename, -1)
    {
        Stream = new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
        FileSize = Stream.Length;
    }
}
