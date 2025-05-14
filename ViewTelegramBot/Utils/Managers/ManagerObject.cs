namespace ViewTelegramBot.Utils.Managers;

public abstract class ManagerObject<T>(Manager<T?> manager)
{
    public T? Obj { get; set; }

    public async Task Load() => Obj = await manager.Load();

    public async Task Save() => await manager.Save(Obj);
}