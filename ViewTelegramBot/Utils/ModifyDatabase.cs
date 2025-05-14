using Database;

namespace ViewTelegramBot.Utils;

// TODO: Fix it
public static class ModifyDatabase
{
    private static readonly Dictionary<Place, string> Tabels = new()
    {
        { Place.Admin, "admins" },
        { Place.State, "states" },
        { Place.Params, "params" },
        { Place.SaveParams, "save_params" },
        { Place.Paymods, "paymodes" },
        { Place.TestMode, "test_mode" }
    };

    public static async Task<long> GetAccess(this IDatabase database, long userId, string? tableName = null)
    {
        if (userId == -1)
            return -1;

        if (string.IsNullOrWhiteSpace(tableName))
            tableName = Tabels[Place.Admin];

        var record = await database.GetRecord(tableName, "userId", userId, false);
        return record.Fields.TryGetValue("access", out var value) ? value.ToLong() : (long) Access.None;
    }

    public static async Task<string> GetState(this IDatabase database, long userId) => (await GetFullState(database, userId)).State;

    public static async Task<bool> SetState(this IDatabase database, long userId, string state)
    {
        if (userId == -1)
            return false;

        var fullState = await GetFullState(database, userId);

        if (string.IsNullOrWhiteSpace(fullState.State))
        {
            var result = await database.Create(
                Tabels[Place.State],
                new Dictionary<string, object>()
                {
                    { "userId", userId },
                    { "nameState", state }
                }
            );

            return result.Success;
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
            return ("", "");

        var record = await database.GetRecord(Tabels[Place.State], "userId", userId);
        return (record.Id, record.Fields.TryGetValue("nameState", out var value) ? value.ToString() : null);
    }

    public static async Task<bool> AddParam(this IDatabase database, long userId, string nameParam, string param)
    {
        if (userId == -1)
            return false;

        var result = await database.Create(Tabels[Place.Params], new Dictionary<string, object>()
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

    public static async Task AddUser(this IDatabase database, long userId, long access, string username, string name)
    {
        if (userId == -1)
            return;

        await database.Create(Tabels[Place.Admin], new Dictionary<string, object>()
        {
            { "userId", userId },
            { "access", access },
            { "username", username },
            { "name", name }
        });
    }

    public static async Task<List<long>> GetUserIdsByAccess(this IDatabase database, long access)
    {
        var lst = new List<long>();

        await foreach (var item in database.GetAllRecordsByField(Tabels[Place.Admin], "access", access))
        {
            if (item.Fields.TryGetValue("userId", out var value))
                lst.Add(value.ToLong());
        }

        return lst;
    }

    // TODO: Update system of contracts
    /*public static async Task<bool> AddContract(this IDatabase database, string? contract, string? nameParam, object? param)
    {
        if (param == null || string.IsNullOrWhiteSpace(contract) || string.IsNullOrWhiteSpace(nameParam))
            return false;

        var result = await database.Create(Tabels[Place.SaveParams], new Dictionary<string, object>()
        {
            { "contract", contract },
            { nameParam, param }
        });

        return result.Success;
    }

    public static async Task<bool> AddLink(this IDatabase database, string contract, string link)
        => await database.AddItemContract(contract, "link", link);

    public static async Task<bool> AddIsSentLink(this IDatabase database, string contract, bool field)
        => await database.AddItemContract(contract, "isSentLink", Convert.ToInt32(field));

    public static async Task<bool> AddIsSentContract(this IDatabase database, string contract, bool field)
        => await database.AddItemContract(contract, "isSentContract", Convert.ToInt32(field));

    public static async Task<bool> AddIsSentRequest(this IDatabase database, string contract, bool field)
        => await database.AddItemContract(contract, "isSentRequest", Convert.ToInt32(field));

    public static async Task<bool> AddContractPath(this IDatabase database, string contract, string pathContract)
        => await database.AddItemContract(contract, "pathContract", pathContract);

    public static async
        Task<(List<(string Link, string Date, int Price)> Links, string pathContract, bool isSentLink, bool isSentContract, bool isSentRequest)>
        GetContract(this IDatabase database, string contract)
    {
        if (string.IsNullOrWhiteSpace(contract))
            return new ValueTuple<List<(string Link, string Date, int Price)>, string, bool, bool, bool>();

        var record = await database.GetContractsField(contract);

        if (string.IsNullOrWhiteSpace(record.Id))
            return (new List<(string Link, string Date, int Price)>(), "", false, false, false);

        var links = JsonConvert.DeserializeObject<List<(string Link, string Date, int Price)>>(string.IsNullOrWhiteSpace(record.Fields.GetString("link")) ? "[]" : record.Fields.GetString("link"))  ?? new List<(string Link, string Date, int Price)>();
        var pathContract = record.Fields.GetString("pathContract");
        var isSentLink = Convert.ToBoolean(record.Fields.GetString("isSentLink").To(x => int.TryParse(x, out var res) ? res : 0));
        var isSentContract = Convert.ToBoolean(record.Fields.GetString("isSentContract").To(x => int.TryParse(x, out var res) ? res : 0));
        var isSentRequest = Convert.ToBoolean(record.Fields.GetString("isSentRequest").To(x => int.TryParse(x, out var res) ? res : 0));

        return (links, pathContract, isSentLink, isSentContract, isSentRequest);
    }

    public static async
        IAsyncEnumerable<(List<(string Link, string Date, int Price)> Links, string contract, string pathContract, bool isSentLink, bool isSentContract, bool isSentRequest)>
        GetAllContracts(this IDatabase database)
    {
        await foreach (var record in database.GetAllRecords(_tabels[Place.SaveParams]))
        {
            var links = JsonConvert.DeserializeObject<List<(string Link, string Date, int Price)>>(string.IsNullOrWhiteSpace(record.Fields.GetString("link")) ? "[]" : record.Fields.GetString("link"))  ?? new List<(string Link, string Date, int Price)>();
            var contract = record.Fields.GetString("contract");
            var pathContract = record.Fields.GetString("pathContract");
            var isSentLink = Convert.ToBoolean(record.Fields.GetString("isSentLink").To(x => int.TryParse(x, out var res) ? res : 0));
            var isSentContract = Convert.ToBoolean(record.Fields.GetString("isSentContract").To(x => int.TryParse(x, out var res) ? res : 0));
            var isSentRequest = Convert.ToBoolean(record.Fields.GetString("isSentRequest").To(x => int.TryParse(x, out var res) ? res : 0));

            yield return (links, contract, pathContract, isSentLink, isSentContract, isSentRequest);
        }
    }

    public static async Task<bool> DeleteContract(this IDatabase database, string contract)
    {
        if (string.IsNullOrWhiteSpace(contract))
            return false;

        return await database.DeleteByField(_tabels[Place.SaveParams], "contract", contract);
    }

    private static async Task<(Dictionary<string, object> Fields, string Id)> GetContractsField(this IDatabase database,
        string contract)
        => await database.GetRecord(_tabels[Place.SaveParams], "contract", contract);

    private static async Task<bool> AddItemContract(this IDatabase database, string contract, string nameField,
        object field)
    {
        if (string.IsNullOrWhiteSpace(contract))
            return false;

        var result = await database.GetRecord(_tabels[Place.SaveParams], "contract", contract);

        if (string.IsNullOrWhiteSpace(result.Id))
            return await database.AddContract(contract, nameField, field);

        return await database.UpdateByField(_tabels[Place.SaveParams], "contract", contract, new() { { nameField, field } });
    }*/

    // TODO: Remove paymodes
    /*public static async Task<List<PayMode>> GetPayMods(this IDatabase database)
    {
        var records = database.GetAllRecords(_tabels[Place.Paymods]);
        var lst = new List<PayMode>();

        await foreach (var item in records)
        {
            lst.Add(
                new PayMode
                {
                    Id = item.Fields.Get("id").To(Convert.ToInt64),
                    Priority = item.Fields.Get("priority").To(Convert.ToUInt32),
                    CountDays = item.Fields.Get("countDays").To(Convert.ToUInt32),
                    Name = item.Fields.GetString("name"),
                    StartTourDate = item.Fields.GetString("startTourDate"),
                    EndTourDate = item.Fields.GetString("endTourDate"),
                    StartPayDate = item.Fields.GetString("startPayDate"),
                    EndPayDate = item.Fields.GetString("endPayDate"),
                    TextMail = item.Fields.GetString("textMail"),
                    Login = item.Fields.GetString("login"),
                    Password = item.Fields.GetString("password")
                }
            );
        }

        return lst;
    }

    public static async Task<bool> DeletePayMode(this IDatabase database, long id) => await database.Delete(_tabels[Place.Paymods], id.ToString());

    public static async Task<bool> AddPayMode(this IDatabase database, PayMode payMode)
    {
        if (payMode == default)
            return false;

        var result = await database.Create(_tabels[Place.Paymods], new Dictionary<string, object>()
        {
            { "priority", payMode.Priority },
            { "countDays", payMode.CountDays },
            { "name", payMode.Name },
            { "startTourDate", payMode.StartTourDate },
            { "endTourDate", payMode.EndTourDate },
            { "startPayDate", payMode.StartPayDate },
            { "endPayDate", payMode.EndPayDate },
            { "textMail", payMode.TextMail },
            { "login", payMode.Login },
            { "password", payMode.Password }
        });

        return result.Success;
    }

    public static async Task<bool> UpdatePayMode(this IDatabase database, PayMode payMode)
    {
        if (payMode == default)
            return false;

        var result = await database.Update(_tabels[Place.Paymods], payMode.Id.ToString(), new Dictionary<string, object>()
        {
            { "priority", payMode.Priority },
            { "countDays", payMode.CountDays },
            { "name", payMode.Name },
            { "startTourDate", payMode.StartTourDate },
            { "endTourDate", payMode.EndTourDate },
            { "startPayDate", payMode.StartPayDate },
            { "endPayDate", payMode.EndPayDate },
            { "textMail", payMode.TextMail },
            { "login", payMode.Login },
            { "password", payMode.Password }
        });

        return result;
    }*/

    public static async Task<(bool Success, bool DebugMode, string Email)> GetTestInfo(this IDatabase database, long userId)
    {
        var record = await database.GetRecord(Tabels[Place.TestMode], "userId", userId, isString: false);

        if (string.IsNullOrWhiteSpace(record.Id))
            return (false, false, "");

        return (true, record.Fields["debugMode"].ToBool(), record.Fields["debugEmail"].ToString() ?? "");
    }

    public static async Task<bool> UpdateDebugMode(this IDatabase database, long userId, bool debugMode)
    {
        var record = await database.GetRecord(Tabels[Place.TestMode], "userId", userId, isString: false);

        if (string.IsNullOrWhiteSpace(record.Id))
        {
            var result = await database.Create(
                Tabels[Place.TestMode],
                new Dictionary<string, object>()
                {
                    { "userId", userId },
                    { "debugMode", Convert.ToInt32(debugMode) },
                    { "debugEmail", Program.DebugEmail }
                }
            );

            return result.Success;
        }

        return await database.UpdateByField(
            Tabels[Place.TestMode],
            "userId",
            userId,
            new Dictionary<string, object>()
            {
                { "debugMode", Convert.ToInt32(debugMode) }
            }
        );
    }

    // TODO: Update
    /*public static async Task<bool> CreateLocalTables(this IDatabase database)
    {
        // Add "paymodes" table
        var res = await database.CreateTable("paymodes", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("priority", typeof(int)) { DefaultValue = 0 },
            new DbParam("countDays", typeof(int)) { DefaultValue = 4 },
            new DbParam("name", typeof(string)),
            new DbParam("startTourDate", typeof(string)),
            new DbParam("endTourDate", typeof(string)),
            new DbParam("startPayDate", typeof(string)),
            new DbParam("endPayDate", typeof(string)),
            new DbParam("textMail", typeof(string)),
            new DbParam("login", typeof(string)),
            new DbParam("password", typeof(string))
        );

        // Add "admins" table
        res |= await database.CreateTable("admins", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("userId", typeof(int)),
            new DbParam("access", typeof(int)),
            new DbParam("username", typeof(string)),
            new DbParam("name", typeof(string))
        );

        // Add "states" table
        res |= await database.CreateTable("states", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("userId", typeof(int)),
            new DbParam("nameState", typeof(string))
        );

        // Add "params" table
        res |= await database.CreateTable("params", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("userId", typeof(int)),
            new DbParam("nameParam", typeof(string)),
            new DbParam("param", typeof(string))
        );

        // Add "save_params" table
        res |= await database.CreateTable("save_params", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("contract", typeof(string)),
            new DbParam("pathContract", typeof(string)),
            new DbParam("link", typeof(string)),
            new DbParam("isSentLink", typeof(bool)) { DefaultValue = false },
            new DbParam("isSentContract", typeof(bool)) { DefaultValue = false },
            new DbParam("isSentRequest", typeof(bool)) { DefaultValue = false }
        );

        // Add "tester" table
        res |= await database.CreateTable("test_mode", true,
            new DbParam("id", typeof(int)) { PrimaryKey = true },
            new DbParam("userId", typeof(int)),
            new DbParam("debugMode", typeof(bool)) { DefaultValue = false },
            new DbParam("debugEmail", typeof(string)) { CanNull = true }
        );

        return res;
    }*/
}