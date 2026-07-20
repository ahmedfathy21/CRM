using System.Reflection;
using System.Text;
using CRM.Common.Caching;
using CRM.Common.Data;
using CRM.Common.Models;
using CRM.Common.Constants;
using CRM.Common.Repositories;
using CRM.Common.Services;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CRM.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

        services.AddScoped<JwtService>();

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CrmAccess", policy =>
                policy.RequireRole(RoleConstants.Admin, RoleConstants.SalesRep, RoleConstants.SalesManager));

            options.AddPolicy("CrmManagerOnly", policy =>
                policy.RequireRole(RoleConstants.Admin, RoleConstants.SalesManager));
        });

        return services;
    }

    public static IServiceCollection AddCrmDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("CrmConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

        return services;
    }

    public static IServiceCollection AddCrmInfrastructure(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        services.AddAutoMapper(cfg => { }, typeof(Program));

        services.AddValidatorsFromAssembly(assembly);

        services.AddMemoryCache();

        services.AddScoped<ICacheService, InMemoryCacheService>();

        return services;
    }
}
