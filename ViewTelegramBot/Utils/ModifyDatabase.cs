using Database;

namespace ViewTelegramBot.Utils;

public static class ModifyDatabase
{
    private static readonly Dictionary<Place, string> Tabels = new()
    {
        { Place.State, "states" },
        { Place.Params, "params" },
        { Place.User, "user" },
    };

    public static async Task<string> GetState(this IDatabase database, long userId) => (await GetFullState(database, userId)).State ?? string.Empty;

    public static async Task<bool> SetState(this IDatabase database, long userId, string state)
    {
        if (userId == -1)
            return false;

        var fullState = await GetFullState(database, userId);

        if (string.IsNullOrWhiteSpace(fullState.State))
        {
            return (await database.Create(
                Tabels[Place.State],
                new Dictionary<string, object?>()
                {
                    { "userId", userId },
                    { "nameState", state }
                }
            )).Success;
        }

        return await database.Update(
            Tabels[Place.State],
            fullState.Id,
            new Dictionary<string, object>()
            {
                { "nameState", state }
            }
        );
    }

    public static async Task<bool> ClearState(this IDatabase database, long userId)
    {
        if (userId == -1)
            return false;

        return await database.DeleteByField(Tabels[Place.State], "userId", userId);
    }

    private static async Task<(string Id, string? State)> GetFullState(IDatabase database, long userId)
    {
        if (userId == -1)
            return (string.Empty, string.Empty);

        var record = await database.GetRecord(Tabels[Place.State], "userId", userId);
        return (record.Id, record.Fields.TryGetValue("nameState", out var value) ? value.ToString() : null);
    }

    public static async Task<bool> AddParam(this IDatabase database, long userId, string nameParam, string param)
    {
        if (userId == -1)
            return false;

        var result = await database.Create(Tabels[Place.Params], new Dictionary<string, object?>()
        {
            { "userId", userId },
            { "nameParam", nameParam},
            { "param", param }
        });

        return result.Success;
    }

    public static async Task<(bool Success, Dictionary<string, string> Params)> GetParams(this IDatabase database, long userId)
    {
        if (userId == -1)
            return (false, new Dictionary<string, string>());

        var records = database.GetAllRecordsByField(Tabels[Place.Params], "userId", userId);

        var dict = new Dictionary<string, string>();

        await foreach (var item in records)
        {
            if (!item.Fields.TryGetValue("nameParam", out var value) ||
                string.IsNullOrWhiteSpace(value.ToString()) ||
                !item.Fields.TryGetValue("param", out var param) ||
                string.IsNullOrWhiteSpace(param.ToString()) ||
                dict.ContainsKey(value.ToString() ?? string.Empty))
                continue;

            dict.TryAdd(value.ToString(), param.ToString());
        }

        return dict.Count > 0 ? (true, dict) : (false, new Dictionary<string, string>());
    }

    public static async Task<bool> ClearParams(this IDatabase database, long userId)
    {
        if (userId == -1)
            return false;

        return await database.DeleteByField(Tabels[Place.Params], "userId", userId);
    }

    public static async Task<User?> GetUser(this IDatabase database, long userId)
    {
        var record = await database.GetRecord(Tabels[Place.User], "telegram_id", userId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return null;

        var user = new User
        {
            Id = record.Id.ToInt(),
            TelegramId = userId,
            UserId = record.Fields.GetInt("user_id"),
            Token = record.Fields.GetString("token"),
            Lang = record.Fields.GetString("lang")
        };

        return user;
    }

    public static async Task<bool> AddUser(this IDatabase database, long telegramId, int userId, string token, string lang)
    {
        var result = await database.Create(Tabels[Place.User], new Dictionary<string, object?>()
        {
            { "telegram_id", telegramId },
            { "user_id", userId },
            { "token", token },
            { "lang", lang }
        });

        return result.Success;
    }

    public static async Task<bool> SetLangUser(this IDatabase database, long userId, string lang)
    {
        if (userId == -1)
            return false;

        var record = await database.GetRecord(Tabels[Place.User], "telegram_id", userId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return false;

        return await database.Update(
            Tabels[Place.User],
            record.Id,
            new Dictionary<string, object>()
            {
                { "lang", lang }
            }
        );
    }

    public static async Task<bool> CreateLocalTables(this IDatabase database)
    {
        // Add "states" table
        var res = await database.CreateTable("states", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("userId", typeof(int)),
            new DbParam("nameState", typeof(string))
        );

        // Add "params" table
        res &= await database.CreateTable("params", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("userId", typeof(int)),
            new DbParam("nameParam", typeof(string)),
            new DbParam("param", typeof(string))
        );

        // Add "user" table
        res &= await database.CreateTable("user", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("telegram_id", typeof(long)) { Unique = true, CanNull = false },
            new DbParam("user_id", typeof(int)) { CanNull = false },
            new DbParam("token", typeof(string)) { CanNull = false },
            new DbParam("lang", typeof(string)) { CanNull = true, DefaultValue = "ru" }
        );

        return res;
    }
}