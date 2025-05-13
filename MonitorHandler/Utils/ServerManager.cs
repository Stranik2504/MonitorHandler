using System.Security.Cryptography;
using Database;
using MonitorHandler.Controllers;
using MonitorHandler.Models;
using Newtonsoft.Json;

namespace MonitorHandler.Utils;

public class ServerManager
{
    private const string ScriptFolder = "scripts";

    private readonly IDatabase _db;

    public ServerManager(IDatabase db)
    {
        _db = db;
        
        _db.Start();
    }

    ~ServerManager()
    {
        _db.End();
    }

    public async Task<List<Server>> GetAllServers(int userId, string token)
    {
        await ValidateUser(userId, token);

        var servers = new List<Server>();

        await foreach (var item in _db.GetAllRecordsByField("user_server", "user_id", userId))
        {
            if (string.IsNullOrWhiteSpace(item.Id)) continue;

            var server = await _db.GetRecordById("servers", item.Id);

            if (string.IsNullOrWhiteSpace(server.Id)) continue;

            servers.Add(new Server()
            {
                Id = server.Fields.GetInt("id"),
                Name = server.Fields.GetString("name"),
                Ip = server.Fields.GetString("ip"),
                Status = server.Fields.GetString("status")
            });
        }

        return servers;
    }

    public async Task<bool> CreateServer(int userId, string userToken, string name, string ip)
    {
        await ValidateUser(userId, userToken);

        var token = GenToken();

        var result = await _db.Create("servers", new Dictionary<string, object>()
        {
            { "ip", ip },
            { "name", name },
            { "status", "offline" },
            { "token", token },
        });

        if (!result.Success)
            throw new Exception("Failed to create server");

        result = await _db.Create("user_server", new Dictionary<string, object>()
        {
            { "server_id", result.Id },
            { "user_id", userId },
        });

        return result.Success;
    }

    public async Task<bool> AddServer(int userId, string userToken, string ip, string token)
    {
        await ValidateUser(userId, userToken);

        var record = await _db.GetRecord("servers", "token", token);

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid token or ip");

        if (record.Fields.GetString("ip") != ip)
            throw new Exception("Invalid token or ip");

        var result = await _db.Create("user_server", new Dictionary<string, object>()
        {
            { "server_id", record.Id },
            { "user_id", userId },
        });

        return result.Success;
    }

    public async Task<bool> UpdateServer(int userId, string userToken, int serverId, Server server)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecordById("servers", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        var result = await _db.Update("servers", record.Id, new Dictionary<string, object>()
        {
            { "name", string.IsNullOrWhiteSpace(server.Name) ? record.Fields.GetString("name") : server.Name },
            { "ip", string.IsNullOrWhiteSpace(server.Ip) ? record.Fields.GetString("ip") : server.Ip },
            { "status", string.IsNullOrWhiteSpace(server.Status) ? record.Fields.GetString("status") : server.Status },
        });

        return result;
    }

    public async Task<bool> DeleteServer(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var result = await _db.Delete("servers", serverId.ToString());

        return result;
    }

    public async Task<string> GetToken(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecordById("servers", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        return record.Fields.GetString("token");
    }

    public async Task<Metric> GetLastMetric(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecord("metrics",
            new SearchField(serverId, "server_id", con: Connection.AND),
            new SearchField(null, "time", con: Connection.AND, match: Match.Max)
        );

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        var metric = new Metric()
        {
            Cpus = JsonConvert.DeserializeObject<List<double>>(record.Fields.GetString("cpus")) ?? [],
            UseRam = record.Fields.GetInt("use_ram"),
            TotalRam = record.Fields.GetInt("total_ram"),
            UseDisks = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("use_disks")) ?? [],
            TotalDisks = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("total_disks")) ?? [],
            NetworkSend = record.Fields.GetInt("network_send"),
            NetworkReceive = record.Fields.GetInt("network_receive"),
            Time = record.Fields.GetDateTime("time")
        };

        return metric;
    }

    public async Task<bool> SetMetric(string token, int serverId, Metric metric)
    {
        var record = await _db.GetRecordById("servers", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        if (record.Fields.GetString("token") != token)
            throw new Exception("Invalid token");

        var result = await _db.Create("metrics", new Dictionary<string, object?>()
        {
            { "server_id", serverId },
            { "cpu", metric.Cpu },
            { "ram", metric.Ram },
            { "disk", metric.Disk },
            { "network", metric.Network },
            { "time", DateTime.UtcNow }
        });

        return result.Success;
    }

    public async Task<List<DockerContainer>> GetAllContainers(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecord("docker", "server_id", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        var containers = new List<DockerContainer>();

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("containers")))
            return containers;

        var containersList = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("containers"));

        if (containersList == null)
            return containers;

        foreach (var containerId in containersList)
        {
            var container = await _db.GetRecordById("containers", containerId.ToString());

            if (string.IsNullOrWhiteSpace(container.Id))
                continue;

            containers.Add(new DockerContainer()
            {
                Id = container.Fields.GetInt("id"),
                Name = container.Fields.GetString("name"),
                ImageId = container.Fields.GetInt("image_id"),
                Status = container.Fields.GetString("status"),
                Resources = container.Fields.GetString("resources"),
                Hash = container.Fields.GetString("hash")
            });
        }

        return containers;
    }

    public async Task<List<DockerImage>> GetAllImages(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecord("docker", "server_id", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        var images = new List<DockerImage>();

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("images")))
            return images;

        var imagesList = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images"));

        if (imagesList == null)
            return images;

        foreach (var imageId in imagesList)
        {
            var image = await _db.GetRecordById("images", imageId.ToString());

            if (string.IsNullOrWhiteSpace(image.Id))
                continue;

            images.Add(new DockerImage()
            {
                Id = image.Fields.GetInt("id"),
                Name = image.Fields.GetString("name"),
                Size = image.Fields.GetDouble("size"),
                Hash = image.Fields.GetString("hash")
            });
        }

        return images;
    }

    public async Task<bool> StartContainer(int userId, string userToken, int serverId, int containerId)
    {
        if (!await ValidateContainer(userId, userToken, serverId, containerId))
            return false;

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageStartContainer(containerId);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    public async Task<bool> StopContainer(int userId, string userToken, int serverId, int containerId)
    {
        if (!await ValidateContainer(userId, userToken, serverId, containerId))
            return false;

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageStopContainer(containerId);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    public async Task<bool> RemoveContainer(int userId, string userToken, int serverId, int containerId)
    {
        if (!await ValidateContainer(userId, userToken, serverId, containerId))
            return false;

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageRemoveContainer(containerId);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    public async Task<bool> RemoveImage(int userId, string userToken, int serverId, int imageId)
    {
        if (!await ValidateImage(userId, userToken, serverId, imageId))
            return false;

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageRemoveImage(imageId);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    public async Task<List<Script>> GetAllScripts(int userId, string userToken, int serverId)
    {
        await Validate(userId, userToken, serverId);

        var scripts = new List<Script>();

        await foreach (var script in _db.GetAllRecordsByField("scripts", "server_id", serverId.ToString()))
        {
            if (string.IsNullOrWhiteSpace(script.Id)) continue;
            if (string.IsNullOrWhiteSpace(script.Fields.GetString("filename"))) continue;

            scripts.Add(new Script()
            {
                Id = script.Fields.GetInt("id"),
                Text = script.Fields.GetString("filename"),
                StartTime = script.Fields.GetDateTime("start_time"),
                OffsetTime = script.Fields.GetDateTime("offset_time")
            });
        }

        await foreach (var script in _db.GetAllRecordsByField("scripts", "server_id", -1))
        {
            if (string.IsNullOrWhiteSpace(script.Id)) continue;
            if (string.IsNullOrWhiteSpace(script.Fields.GetString("filename"))) continue;

            scripts.Add(new Script()
            {
                Id = script.Fields.GetInt("id"),
                Text = script.Fields.GetString("filename"),
                StartTime = script.Fields.GetDateTime("start_time"),
                OffsetTime = script.Fields.GetDateTime("offset_time")
            });
        }

        return scripts;
    }

    public async Task<Script> GetScript(int userId, string userToken, int serverId, int scriptId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecordById("scripts", scriptId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid script");

        if (record.Fields.GetInt("server_id") != serverId && record.Fields.GetInt("server_id") != -1)
            throw new Exception("Invalid server");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("filename")))
            throw new Exception("Invalid script");

        var script = new Script()
        {
            Id = record.Fields.GetInt("id"),
            Text = record.Fields.GetString("filename"),
            StartTime = record.Fields.GetDateTime("start_time"),
            OffsetTime = record.Fields.GetDateTime("offset_time")
        };

        if (File.Exists(Path.Combine(ScriptFolder, script.Text)))
            script.Text = await File.ReadAllTextAsync(Path.Combine(ScriptFolder, script.Text));

        return script;
    }

    public async Task<bool> RunScript(int userId, string userToken, int serverId, int scriptId)
    {
        if (!await ValidateScript(userId, userToken, serverId, scriptId))
            return false;

        var record = await _db.GetRecordById("scripts", scriptId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid script");

        var filename = record.Fields.GetString("filename");

        if (string.IsNullOrWhiteSpace(filename))
            throw new Exception("Invalid script");

        if (!File.Exists(ScriptFolder + filename))
            return false;

        var script = await File.ReadAllTextAsync(Path.Combine(ScriptFolder, filename));

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageRunScript(script);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    public async Task<bool> CreateScript(int userId, string userToken, int serverId, Script script)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecordById("servers", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        if (!Directory.Exists(ScriptFolder))
            Directory.CreateDirectory(ScriptFolder);

        var filename = string.Empty;

        while (File.Exists(Path.Combine(ScriptFolder, filename)) || string.IsNullOrWhiteSpace(filename))
            filename = Guid.NewGuid().ToString() + ".sh";

        await File.WriteAllTextAsync(Path.Combine(ScriptFolder, filename), script.Text);
        script.Text = Path.Combine(ScriptFolder, filename);

        var paramScript = new Dictionary<string, object>()
        {
            { "server_id", serverId },
            { "filename", script.Text }
        };

        if (script.StartTime != null)
            paramScript.Add("start_time", script.StartTime);

        if (script.OffsetTime != null)
            paramScript.Add("offset_time", script.OffsetTime);

        var result = await _db.Create("scripts", paramScript);

        return result.Success;
    }

    public async Task<bool> DeleteScript(int userId, string userToken, int serverId, int scriptId)
    {
        if (!await ValidateScript(userId, userToken, serverId, scriptId))
            return false;

        var record = await _db.GetRecordById("scripts", scriptId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid script");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("filename")))
            throw new Exception("Invalid script");

        if (File.Exists(ScriptFolder + record.Fields.GetString("filename")))
            File.Delete(ScriptFolder + record.Fields.GetString("filename"));

        var result = await _db.Delete("scripts", scriptId.ToString());
        return result;
    }

    public async Task<bool> RunCommand(int userId, string userToken, int serverId, string command)
    {
        await Validate(userId, userToken, serverId);

        var controller = WebSocketController.GetController(serverId);

        if (controller == null)
            return false;

        controller.AddMessageRunCommand(command);
        return bool.TryParse(await controller.WaitResult(), out var result) && result;
    }

    // TODO: Remove
    public async Task AddTestValues()
    {
        await _db.Create("users", new Dictionary<string, object>()
        {
            { "username", "admin" },
            { "token", "admin" },
        });

        await _db.Create("users", new Dictionary<string, object>()
        {
            { "username", "test" },
            { "token", "test" },
        });

        await _db.Create("servers", new Dictionary<string, object>()
        {
            { "ip", "127.0.0.1" },
            { "name", "hahaha" },
            { "status", "online" },
            { "token", "admin" },
        });

        await _db.Create("servers", new Dictionary<string, object>()
        {
            { "ip", "127.0.0.1" },
            { "name", "2" },
            { "status", "online" },
            { "token", "adminss" },
        });

        await _db.Create("servers", new Dictionary<string, object>()
        {
            { "ip", "192.0.0.1" },
            { "name", "3" },
            { "status", "offline" },
            { "token", "admin12323" },
        });

        await _db.Create("user_server", new Dictionary<string, object>()
        {
            { "server_id", 1 },
            { "user_id", 1 },
        });

        await _db.Create("user_server", new Dictionary<string, object>()
        {
            { "server_id", 2 },
            { "user_id", 1 },
        });

        await _db.Create("user_server", new Dictionary<string, object>()
        {
            { "server_id", 3 },
            { "user_id", 2 },
        });
    }

    private string GenToken()
    {
        const int tokenLength = 64;
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

        var tokenChars = new char[tokenLength];
        var randomBytes = new byte[tokenLength];


        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        for (var i = 0; i < tokenLength; i++)
            tokenChars[i] = chars[randomBytes[i] % chars.Length];

        return new string(tokenChars);
    }

    private async Task ValidateUser(int userId, string userToken)
    {
        var record = await _db.GetRecordById("users", userId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid token or userId");

        if (record.Fields.GetString("token") != userToken)
            throw new Exception("Invalid token or userId");
    }

    private async Task Validate(int userId, string userToken, int serverId)
    {
        await ValidateUser(userId, userToken);

        var result = await _db.GetRecord(
            "user_server",
            new SearchField(userId, "user_id", con: Connection.AND),
            new SearchField(serverId, "server_id")
        );

        if (string.IsNullOrWhiteSpace(result.Id))
            throw new Exception("User doesn't have this server");
    }

    private async Task<bool> ValidateContainer(int userId, string userToken, int serverId, int containerId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecord("docker", "server_id", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("containers")))
            return false;

        var containersList = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("containers"));

        if (containersList == null)
            return false;

        if (!containersList.Contains(containerId))
            return false;

        return true;
    }

    private async Task<bool> ValidateImage(int userId, string userToken, int serverId, int imageId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecord("docker", "server_id", serverId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("images")))
            return false;

        var imagesList = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images"));

        if (imagesList == null)
            return false;

        if (!imagesList.Contains(imageId))
            return false;

        return true;
    }

    private async Task<bool> ValidateScript(int userId, string userToken, int serverId, int scriptId)
    {
        await Validate(userId, userToken, serverId);

        var record = await _db.GetRecordById("scripts", scriptId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid script");

        if (record.Fields.GetInt("server_id") != serverId && record.Fields.GetInt("server_id") != -1)
            throw new Exception("Invalid server");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("filename")))
            return false;

        return true;
    }
}