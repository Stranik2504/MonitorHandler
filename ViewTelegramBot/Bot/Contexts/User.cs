namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст пользователя Telegram.
/// </summary>
public class User
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Является ли пользователь ботом.
    /// </summary>
    public bool IsBot { get; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string? FirstName { get; }

    /// <summary>
    /// Фамилия пользователя.
    /// </summary>
    public string? LastName { get; }

    /// <summary>
    /// Username пользователя.
    /// </summary>
    public string? Username { get; }

    /// <summary>
    /// Пользователь по умолчанию.
    /// </summary>
    public static User DefaultUser => new User();

    /// <summary>
    /// Создаёт новый контекст пользователя.
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="username">Username</param>
    /// <param name="firstName">Имя</param>
    /// <param name="lastName">Фамилия</param>
    /// <param name="isBot">Является ли ботом</param>
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

    /// <summary>
    /// Приватный конструктор для пользователя по умолчанию.
    /// </summary>
    private User() : this(0, null) {}
}
