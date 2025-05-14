using System.Text;

namespace ViewTelegramBot.Bot.Context;

public class TextDocument : Document
{
    public Stream Stream { get; }

    public TextDocument(Stream stream, string filename) : base(filename, filename, stream.Length) => Stream = stream;

    public TextDocument(string text, string filename) : base(filename, filename, -1)
    {
        Stream = new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
        FileSize = Stream.Length;
    }
}