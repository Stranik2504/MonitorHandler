using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Bot.KeyboardUtls;

/// <summary>
/// Клавиатура для Telegram-бота.
/// </summary>
public class Keyboard
{
    /// <summary>
    /// Признак inline-клавиатуры.
    /// </summary>
    public bool Inline { get; } = true;

    /// <summary>
    /// Список строк клавиатуры.
    /// </summary>
    public List<Line> Lines { get; } = [];

    /// <summary>
    /// Создаёт клавиатуру с заданными строками и типом.
    /// </summary>
    /// <param name="lines">Список строк</param>
    /// <param name="inline">Inline-клавиатура</param>
    public Keyboard(List<Line> lines, bool inline = true) => (Lines, Inline) = (lines, inline);

    /// <summary>
    /// Создаёт клавиатуру с типом (без строк).
    /// </summary>
    /// <param name="inline">Inline-клавиатура</param>
    public Keyboard(bool inline) => Inline = inline;

    /// <summary>
    /// Создаёт клавиатуру с типом и строками.
    /// </summary>
    /// <param name="inline">Inline-клавиатура</param>
    /// <param name="lines">Строки</param>
    public Keyboard(bool inline = true, params Line[] lines)
    {
        Inline = inline;
        Lines.AddRange(lines);
    }

    /// <summary>
    /// Создаёт клавиатуру с типом и кнопками (одна строка).
    /// </summary>
    /// <param name="inline">Inline-клавиатура</param>
    /// <param name="buttons">Кнопки</param>
    public Keyboard(bool inline = true, params Button[] buttons)
    {
        Inline = inline;
        AddLine(buttons);
    }

    /// <summary>
    /// Создаёт клавиатуру с заданными строками.
    /// </summary>
    /// <param name="lines">Строки</param>
    public Keyboard(params Line[] lines) => Lines.AddRange(lines);

    /// <summary>
    /// Добавляет строку в клавиатуру.
    /// </summary>
    /// <param name="line">Строка</param>
    public void AddLine(Line line) => Lines.Add(line);

    /// <summary>
    /// Добавляет строку из кнопок в клавиатуру.
    /// </summary>
    /// <param name="buttons">Кнопки</param>
    public void AddLine(params Button[] buttons) => buttons.ForEach(x => Lines.Add(new Line(x)));

    /// <summary>
    /// Добавляет кнопку в последнюю строку клавиатуры (или создаёт новую строку).
    /// </summary>
    /// <param name="button">Кнопка</param>
    public void AddButton(Button button)
    {
        if (Lines.Count == 0)
            Lines.Add(new Line());

        Lines[^1].AddButton(button);
    }

    /// <summary>
    /// Проверяет равенство клавиатур.
    /// </summary>
    /// <param name="obj">Объект для сравнения</param>
    /// <returns>True, если клавиатуры равны</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Keyboard keyboard)
            return false;

        if (Lines.Count != keyboard.Lines.Count || Inline != keyboard.Inline)
            return false;

        for (var i = 0; i < Lines.Count; i++)
        {
            if (Lines[i].Buttons.Count != keyboard.Lines[i].Buttons.Count)
                return false;

            for (var j = 0; j < Lines[i].Buttons.Count; j++)
            {
                var btn1 = Lines[i].Buttons[j];
                var btn2 = keyboard.Lines[i].Buttons[j];

                if (btn1.Payload != btn2.Payload || btn1.Text != btn2.Text)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Возвращает хэш-код клавиатуры.
    /// </summary>
    /// <returns>Хэш-код</returns>
    public override int GetHashCode() => HashCode.Combine(Inline, Lines);
}
