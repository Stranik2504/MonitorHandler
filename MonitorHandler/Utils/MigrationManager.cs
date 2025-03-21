using Database;

namespace MonitorHandler.Utils;

public class MigrationManager(IDatabase db, int version)
{
    private readonly IDatabase _db = db;
    private readonly int _version = version;

    public async Task Migrate()
    {
        if (_version <= 1)
        {
            await MigrateV1();
        }
    }

    private async Task MigrateV1()
    {
        await _db.CreateTable(
            "servers",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("ip", typeof(string)) { PrimaryKey = true, Unique = true },
            new DbParam("name", typeof(string)) { CanNull = false, DefaultValue = "server" },
            new DbParam("status", typeof(string)) { CanNull = false, DefaultValue = "offline" }
        );

        await _db.CreateTable(
            "users",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("username", typeof(string)) { CanNull = false },
            new DbParam("token", typeof(string)) { CanNull = true },
            new DbParam("login", typeof(string)) { CanNull = true },
            new DbParam("password", typeof(string)) { CanNull = true }
        );

        await _db.CreateTable(
            "user_servers",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbParam("user_id", typeof(int))
        );

        await _db.CreateTable(
            "metrics",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbParam("cpu", typeof(string)),
            new DbParam("ram", typeof(string)),
            new DbParam("disk", typeof(string)),
            new DbParam("network", typeof(string)),
            new DbParam("time", typeof(DateTime))
        );

        await _db.CreateTable(
            "docker"
        );
    }
}