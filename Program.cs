using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using SEAL.NET.Data;
using SEAL.NET.Middleware;
using SEAL.NET.Models.Entities;
using SEAL.NET.Repositories.Implementations;
using SEAL.NET.Repositories.Interfaces;
using SEAL.NET.Services.Implementations;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
const string SecurityStampClaimType = "seal_security_stamp";

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key must be configured.");
}

var insecureJwtKeys = new HashSet<string>(StringComparer.Ordinal)
{
    "LOCAL_DEVELOPMENT_SECRET_KEY_FOR_SEAL_NET_2026",
    "set-a-long-random-secret-in-user-secrets-or-environment"
};

if (jwtKey.Length < 32 || insecureJwtKeys.Contains(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key must be a non-default secret with at least 32 characters.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();


builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRankingService, RankingService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };
    options.Events = new JwtBearerEvents
    {

        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("seal_token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },

        OnTokenValidated = async context =>

        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                context.Fail("Invalid token subject.");
                return;
            }

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(parsedUserId.ToString());

            if (user == null || !user.IsApproved)
            {
                context.Fail("User is no longer approved.");
                return;
            }

            var tokenSecurityStamp = context.Principal?.FindFirstValue(SecurityStampClaimType);
            if (string.IsNullOrEmpty(tokenSecurityStamp) || tokenSecurityStamp != user.SecurityStamp)
            {
                context.Fail("User session is no longer current.");
                return;
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var currentRoleSet = currentRoles.ToHashSet(StringComparer.Ordinal);
            var tokenRoles = context.Principal?
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList() ?? new List<string>();

            if (tokenRoles.Any(role => !currentRoleSet.Contains(role)))
            {
                context.Fail("User role claims are no longer current.");
            }
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://localhost:3000" }
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Name = "seal_token",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "Authentication uses the HttpOnly seal_token cookie set by POST /api/auth/login. Browser clients do not read or store the token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<SealCsrfHeaderOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    var isApiRequest = context.Request.Path.StartsWithSegments("/api");
    var isUnsafeMethod = HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method);
    var isAuthBootstrapEndpoint =
        context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase);

    if (isApiRequest && isUnsafeMethod && !isAuthBootstrapEndpoint && context.Request.Headers["X-SEAL-CSRF"] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { message = "Missing CSRF protection header." });
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (ApplicationDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("HealthCheck");
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "Healthy", database = "Healthy" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed.");
        return Results.Json(
            new { status = "Unhealthy", database = "Unhealthy" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapControllers();

app.Run();

public sealed class SealCsrfHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (method == null || !HttpMethods.IsPost(method) &&
            !HttpMethods.IsPut(method) &&
            !HttpMethods.IsPatch(method) &&
            !HttpMethods.IsDelete(method))
        {
            return;
        }

        var relativePath = context.ApiDescription.RelativePath;
        if (relativePath != null &&
            (relativePath.StartsWith("api/auth/login", StringComparison.OrdinalIgnoreCase) ||
             relativePath.StartsWith("api/auth/register", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-SEAL-CSRF",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Required for unsafe API methods. Set to 1.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new Microsoft.OpenApi.Any.OpenApiString("1")
            }
        });
    }
}
