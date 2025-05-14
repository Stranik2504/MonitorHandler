using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Bot.KeyboardUtls;

public class Keyboard
{
    public bool Inline { get; } = true;
    public List<Line> Lines { get; } = [];

    public Keyboard(List<Line> lines, bool inline = true) => (Lines, Inline) = (lines, inline);

    public Keyboard(bool inline) => Inline = inline;

    public Keyboard(bool inline = true, params Line[] lines)
    {
        Inline = inline;
        Lines.AddRange(lines);
    }

    public Keyboard(bool inline = true, params Button[] buttons)
    {
        Inline = inline;
        AddLine(buttons);
    }

    public Keyboard(params Line[] lines) => Lines.AddRange(lines);

    public void AddLine(Line line) => Lines.Add(line);

    public void AddLine(params Button[] buttons) => buttons.ForEach(x => Lines.Add(new Line(x)));

    public void AddButton(Button button)
    {
        if (Lines.Count == 0)
            Lines.Add(new Line());

        Lines[^1].AddButton(button);
    }

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

    public override int GetHashCode() => HashCode.Combine(Inline, Lines);
}