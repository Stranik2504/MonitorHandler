namespace ViewTelegramBot.Utils.Managers;

public class ManagerObjectFile<T>(string filename) : ManagerObject<T>(new ManagerFile<T?>(filename));