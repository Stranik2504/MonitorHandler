namespace ViewTelegramBot.Bot.Context;

public class User
{
    public long Id { get; }
    public bool IsBot { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? Username { get; }

    public static User DefaultUser => new User();

    public User(
        long id,
        string? username,
        string? firstName = null,
        string? lastName = null,
        bool isBot = false
    )
    {
        Id = id;
        IsBot = isBot;
        Username = username ?? "";
        FirstName = firstName;
        LastName = lastName;
    }

    private User() : this(0, null) {}
}