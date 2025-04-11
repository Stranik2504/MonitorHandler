namespace Database;

public class DbForeignKey(string name, string table, string nameField) : DbBase(name)
{
    public string Table { get; set; }
    public string NameField { get; set; }
}