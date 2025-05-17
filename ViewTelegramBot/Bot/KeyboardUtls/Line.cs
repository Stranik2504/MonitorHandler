namespace ViewTelegramBot.Bot.KeyboardUtls;

/// <summary>
/// Строка (ряд) кнопок для клавиатуры Telegram-бота.
/// </summary>
public class Line
{
    /// <summary>
    /// Список кнопок в строке.
    /// </summary>
    public List<Button> Buttons { get; set; } = [];

    /// <summary>
    /// Создаёт строку с кнопками.
    /// </summary>
    /// <param name="buttons">Список кнопок</param>
    public Line(List<Button> buttons) => Buttons.AddRange(buttons);

    /// <summary>
    /// Создаёт строку с кнопками.
    /// </summary>
    /// <param name="buttons">Массив кнопок</param>
    public Line(params Button[] buttons) => Buttons.AddRange(buttons);

    /// <summary>
    /// Добавляет кнопки в строку.
    /// </summary>
    /// <param name="buttons">Массив кнопок</param>
    public void AddButtons(params Button[] buttons) => Buttons.AddRange(buttons);

    /// <summary>
    /// Добавляет одну кнопку в строку.
    /// </summary>
    /// <param name="button">Кнопка</param>
    public void AddButton(Button button) => Buttons.Add(button);
}
