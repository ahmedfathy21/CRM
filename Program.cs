using CRM.Common.Extensions;
using CRM.Common.Middleware;
using CRM.Features.CRM.Common.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("localappsettings.json", optional: true, reloadOnChange: true);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddCrmDatabase(builder.Configuration)
    .AddJwt(builder.Configuration)
    .AddAuthorizationPolicies()
    .AddCrmInfrastructure();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var crmDb = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    if (crmDb.Database.IsRelational())
        await crmDb.Database.MigrateAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();