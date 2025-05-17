namespace ViewTelegramBot.Utils.Managers;

/// <summary>
/// Класс-обёртка для управления объектом типа T, хранящимся в файле.
/// </summary>
/// <typeparam name="T">Тип управляемого объекта</typeparam>
public class ManagerObjectFile<T>(string filename) : ManagerObject<T>(new ManagerFile<T?>(filename));
