using Database;
using MonitorHandler.Models;

namespace MonitorHandler.Utils;

public class ServerManager
{
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

    public async Task<List<Server>> GetAllServers()
    {
        var servers = new List<Server>();

        foreach (var VARIABLE in _db.GetAllRecordsByField("servers", ""))
        {
            
        }
    }
}