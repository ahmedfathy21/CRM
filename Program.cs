var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("localappsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();