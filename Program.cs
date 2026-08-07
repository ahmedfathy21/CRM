using CRM.Common.Data;
using CRM.Common.Extensions;
using CRM.Common.Middleware;
using CRM.Features.CRM.Common.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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

    var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (appDb.Database.IsRelational())
        await appDb.Database.MigrateAsync();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("CRM API").WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();