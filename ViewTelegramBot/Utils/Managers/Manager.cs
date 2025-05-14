namespace ViewTelegramBot.Utils.Managers;

public abstract class Manager<T>
{
    public Action Loaded;
    public Action Saved;

    public abstract Task<T?> Load();
    public abstract Task Save(T obj);
}