namespace MonitorHandler.Models;

/// <summary>
/// Модель пользователя системы.
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Токен пользователя для авторизации.
    /// </summary>
    public string Token { get; set; }
}
