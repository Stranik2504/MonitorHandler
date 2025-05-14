namespace ViewTelegramBot.Bot.KeyboardUtls;

public class Line
{
    public List<Button> Buttons { get; set; } = [];

    public Line(List<Button> buttons) => Buttons.AddRange(buttons);

    public Line(params Button[] buttons) => Buttons.AddRange(buttons);

    public void AddButtons(params Button[] buttons) => Buttons.AddRange(buttons);

    public void AddButton(Button button) => Buttons.Add(button);
}