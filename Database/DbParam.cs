namespace Database;

// Row to create table
public class DbParam(string name, Type typeField)
{
    private readonly object? _defaultValue;

    public string Name { get; set; } = name;
    public Type TypeField { get; private set; } = typeField;
    public bool PrimaryKey { get; set; } = false;
    public bool Unique { get; set; } = false;
    public bool? AutoIncrement { get; set; } = false;
    public bool? CanNull { get; set; } = null;
    public bool HaveDefaultValue { get; private set; } = false;

    public object? DefaultValue
    {
        get => _defaultValue;
        init
        {
            HaveDefaultValue = true;
            _defaultValue = value;
        }
    }

    public void ClearDefaultValue() => HaveDefaultValue = false;
}