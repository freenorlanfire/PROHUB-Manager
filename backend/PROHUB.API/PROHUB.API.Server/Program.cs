using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Endpoints;
using PROHUB.API.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (OpenTelemetry — safe standalone; OTLP solo si hay endpoint) ──
builder.AddServiceDefaults();

// ── Database ──────────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Prohub")
    ?? throw new InvalidOperationException(
        "Connection string 'Prohub' not found. " +
        "Define ConnectionStrings__Prohub en env var o appsettings.Development.json.");

builder.Services.AddDbContext<ProhubDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// ── JWT ───────────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Jwt settings not found. Define Jwt__Key / Jwt__Issuer / Jwt__Audience.");

if (jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PROHUB Manager API", Version = "v1" });

    var bearer = new OpenApiSecurityScheme
    {
        In          = ParameterLocation.Header,
        Description = "Introduce: Bearer {tu JWT}",
        Name        = "Authorization",
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    };
    c.AddSecurityDefinition("Bearer", bearer);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000", "http://localhost:5534"];

builder.Services.AddCors(opt => opt.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader()));

builder.Services.AddProblemDetails();

// ─────────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────────

app.UseExceptionHandler();
app.UseCors("Frontend");

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Request-Id"] = ctx.TraceIdentifier;
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ── Swagger ───────────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PROHUB Manager API v1");
    c.RoutePrefix = "swagger";
});

// ── Auto-migrate DB ───────────────────────────────────────────────────────────────
// Aplica pendientes al arrancar (dev + prod). Idempotente y seguro.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ProhubDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("[DB] Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[DB] Error al aplicar migraciones. La app seguirá, pero puede fallar.");
    }
}

// ── Health — SIEMPRE público, sin auth ───────────────────────────────────────────
// El AppHost lo usa para saber si el server está listo.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .ExcludeFromDescription();

app.MapGet("/api/health", () => Results.Ok(ApiResponse<object>.Success(new
{
    status      = "healthy",
    version     = "1.0.0",
    timestamp   = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}))).WithTags("Health").ExcludeFromDescription();

// ── API endpoints ─────────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapCompanyEndpoints();
app.MapProjectEndpoints();
app.MapStatusEndpoints();
app.MapContextDocEndpoints();
app.MapIntegrationEndpoints();
app.MapTagEndpoints();
app.MapLinkEndpoints();

// ── Aspire probes (/alive) ────────────────────────────────────────────────────────
app.MapDefaultEndpoints();

// ── Serve React SPA (wwwroot en producción) ───────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// ── Seed inicial (dev + prod) — solo crea el admin si no existe ningún usuario ────
{
    var seedLogger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        await DataSeeder.SeedAsync(app.Services, seedLogger);
    }
    catch (Exception ex)
    {
        seedLogger.LogWarning(
            "[Seed] No se pudo conectar a la BD. El servidor arranca igual. " +
            "Reinicia para crear el usuario admin. Error: {Msg}",
            ex.Message);
    }
}

app.Run();
