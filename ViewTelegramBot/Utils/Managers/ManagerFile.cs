using Newtonsoft.Json;

namespace ViewTelegramBot.Utils.Managers;

/// <summary>
/// Менеджер для загрузки и сохранения объекта типа T в файл с поддержкой потокобезопасности.
/// </summary>
/// <typeparam name="T">Тип управляемого объекта</typeparam>
public class ManagerFile<T> : Manager<T>
{
    /// <summary>
    /// Событие для синхронизации доступа к файлу.
    /// </summary>
    private readonly AutoResetEvent _waitHandle;

    /// <summary>
    /// Имя файла для хранения объекта.
    /// </summary>
    protected readonly string _filename;

    /// <summary>
    /// Создаёт менеджер для работы с файлом.
    /// </summary>
    /// <param name="filename">Имя файла</param>
    public ManagerFile(string filename) => (_filename, _waitHandle) = (filename, new AutoResetEvent(true));

    /// <summary>
    /// Загружает объект из файла.
    /// </summary>
    /// <returns>Загруженный объект или null</returns>
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

    /// <summary>
    /// Сохраняет объект в файл.
    /// </summary>
    /// <param name="obj">Объект для сохранения</param>
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
