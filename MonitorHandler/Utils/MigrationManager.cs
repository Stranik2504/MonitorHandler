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
            new DbParam("ip", typeof(string)) { CanNull = false },
            new DbParam("name", typeof(string)) { CanNull = false, DefaultValue = "server" },
            new DbParam("status", typeof(string)) { CanNull = false, DefaultValue = "offline" },
            new DbParam("token", typeof(string)) { CanNull = false }
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
            "user_server",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbParam("user_id", typeof(int)),
            new DbForeignKey("server_id", "servers", "id"),
            new DbForeignKey("user_id", "users", "id")
        );

        await _db.CreateTable(
            "metrics",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbForeignKey("server_id", "servers", "id"),
            new DbParam("cpus", typeof(string)),
            new DbParam("use_ram", typeof(ulong)),
            new DbParam("total_ram", typeof(ulong)),
            new DbParam("use_disks", typeof(string)),
            new DbParam("total_disks", typeof(string)),
            new DbParam("network_send", typeof(ulong)),
            new DbParam("network_receive", typeof(ulong)),
            new DbParam("time", typeof(DateTime))
        );

        await _db.CreateTable(
            "docker",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbForeignKey("server_id", "servers", "id"),
            new DbParam("containers", typeof(string)),
            new DbParam("images", typeof(string))
        );

        await _db.CreateTable(
            "images",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("name", typeof(string)) { CanNull = false },
            new DbParam("size", typeof(double)) { CanNull = false },
            new DbParam("hash", typeof(string)) { CanNull = false, Unique = true }
        );

        await _db.CreateTable(
            "containers",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("name", typeof(string)) { CanNull = false },
            new DbParam("image_id", typeof(int)),
            new DbForeignKey("image_id", "images", "id"),
            new DbParam("image_hash", typeof(string)) { CanNull = false },
            new DbParam("status", typeof(string)) { CanNull = false },
            new DbParam("resources", typeof(string)),
            new DbParam("hash", typeof(string)) { CanNull = false, Unique = true }
        );

        await _db.CreateTable(
            "scripts",
            true,
            new DbParam("id", typeof(int)) { PrimaryKey = true, Unique = true, AutoIncrement = true },
            new DbParam("server_id", typeof(int)),
            new DbForeignKey("server_id", "servers", "id"),
            new DbParam("filename", typeof(string)) { CanNull = false },
            new DbParam("start_time", typeof(DateTime)) { CanNull = true },
            new DbParam("offset_time", typeof(DateTime)) { CanNull = true }
        );
    }
}