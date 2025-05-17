namespace ViewTelegramBot.Utils.Managers;

/// <summary>
/// Абстрактный класс-обёртка для управления объектом типа T через менеджер.
/// </summary>
/// <typeparam name="T">Тип управляемого объекта</typeparam>
public abstract class ManagerObject<T>(Manager<T?> manager)
{
    /// <summary>
    /// Управляемый объект.
    /// </summary>
    public T? Obj { get; set; }

    /// <summary>
    /// Загружает объект через менеджер.
    /// </summary>
    public async Task Load() => Obj = await manager.Load();

    /// <summary>
    /// Сохраняет объект через менеджер.
    /// </summary>
    public async Task Save() => await manager.Save(Obj);
}
