using Database;
using MonitorHandler.Utils;

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
        config.Obj.MainDbName,
        config.Obj.MainDbUser,
        config.Obj.MainDbPassword,
        x.GetRequiredService<ILogger<Database.MySql>>()
    );
    db.Start();

    return db;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();