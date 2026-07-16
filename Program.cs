using CRM.Common.Extensions;
using CRM.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("localappsettings.json", optional: true, reloadOnChange: true);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddJwt(builder.Configuration)
    .AddAuthorizationPolicies()
    .AddCrmInfrastructure();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();