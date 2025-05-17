namespace ViewTelegramBot.Utils.Managers;

/// <summary>
/// Абстрактный менеджер для загрузки и сохранения объектов типа T.
/// </summary>
/// <typeparam name="T">Тип управляемого объекта</typeparam>
public abstract class Manager<T>
{
    /// <summary>
    /// Событие, вызываемое после загрузки объекта.
    /// </summary>
    public Action Loaded;

    /// <summary>
    /// Событие, вызываемое после сохранения объекта.
    /// </summary>
    public Action Saved;

    /// <summary>
    /// Загружает объект из источника.
    /// </summary>
    /// <returns>Загруженный объект или null</returns>
    public abstract Task<T?> Load();

    /// <summary>
    /// Сохраняет объект в источник.
    /// </summary>
    /// <param name="obj">Объект для сохранения</param>
    public abstract Task Save(T obj);
}
