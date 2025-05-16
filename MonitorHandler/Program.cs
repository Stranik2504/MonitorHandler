using Database;
using MonitorHandler.Controllers;
using MonitorHandler.Utils;

namespace MonitorHandler
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddSingleton<ManagerObject<Config>>(_ =>
            {
                var manager = new ManagerObject<Config>("files/config.json");
                manager.Load();

                return manager;
            });

            builder.Services.AddSingleton<IDatabase>(x =>
            {
                var config = x.GetRequiredService<ManagerObject<Config>>();

                if (config.Obj == null)
                    throw new Exception("Config is null");

                var db = new Database.MySql(
                    config.Obj.MainDbHost,
                    config.Obj.MainDbPort,
                    config.Obj.MainDbName,
                    config.Obj.MainDbUser,
                    config.Obj.MainDbPassword,
                    x.GetRequiredService<ILogger<Database.MySql>>()
                );
                db.Start();

                return db;
            });

            builder.Services.AddSingleton<ServerManager>();
            builder.Services.AddHostedService<MetricsCleanupService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SupportNonNullableReferenceTypes();
            });

            var app = builder.Build();

            var db = app.Services.GetRequiredService<IDatabase>();
            var config = app.Services.GetRequiredService<ManagerObject<Config>>();
            var log = app.Services.GetRequiredService<ILogger<Program>>();

            MigrationManager migrationManager = new(db, config.Obj?.VersionDb ?? 1);
            await migrationManager.Migrate();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // WebSockets
            app.UseWebSockets();

            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var logger = app.Services.GetRequiredService<ILogger<WebSocketController>>();
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    log.LogInformation("[WebSocket]: New connection from {Remote}", context.Connection.RemoteIpAddress);

                    var controller = new WebSocketController(logger, db, webSocket, config.Obj ?? new Config());
                    await Task.Run(controller.Run);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            await app.RunAsync();
        }
    }
}