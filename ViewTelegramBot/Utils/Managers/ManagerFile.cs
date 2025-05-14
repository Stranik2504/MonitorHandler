using Newtonsoft.Json;

namespace ViewTelegramBot.Utils.Managers;

public class ManagerFile<T> : Manager<T>
{
    private readonly AutoResetEvent _waitHandle;
    protected readonly string _filename;

    public ManagerFile(string filename) => (_filename, _waitHandle) = (filename, new AutoResetEvent(true));

    public override async Task<T?> Load()
    {
        try
        {
            _waitHandle.WaitOne();

            if (!File.Exists(_filename))
                await File.WriteAllTextAsync(_filename, "{}");

            var obj = JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(_filename));
            _waitHandle.Set();

            Loaded?.Invoke();

            return obj;
        }
        catch
        {
            // ignored
        }

        return default;
    }

    public override async Task Save(T obj)
    {
        try
        {
            _waitHandle.WaitOne();

            await File.WriteAllTextAsync(_filename, JsonConvert.SerializeObject(obj));

            _waitHandle.Set();

            Saved?.Invoke();
        }
        catch
        {
            // ignored
        }
    }
}