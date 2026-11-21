using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicStore.Api.Data;
using MusicStore.Api.Services;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Npgsql;

if (args.Contains("--healthcheck", StringComparer.Ordinal))
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        using var response = await client.GetAsync("http://127.0.0.1:8080/api/health");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        Environment.ExitCode = 1;
    }
    catch (TaskCanceledException)
    {
        Environment.ExitCode = 1;
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Подключение к PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Database connection string is missing");
var dbPasswordFile = builder.Configuration["DbPasswordFile"];
if (!string.IsNullOrWhiteSpace(dbPasswordFile))
{
    var password = File.ReadAllText(dbPasswordFile).TrimEnd('\r', '\n');
    if (password.Length == 0)
        throw new InvalidOperationException("Database password file is empty");
    connectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Password = password
    }.ConnectionString;
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Регистрируем наши кастомные сервисы
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();

// 4. Настройка JWT аутентификации
var jwtKeyFile = builder.Configuration["JwtKeyFile"];
var jwtKey = string.IsNullOrWhiteSpace(jwtKeyFile)
    ? builder.Configuration["Jwt:Key"]
    : File.ReadAllText(jwtKeyFile).TrimEnd('\r', '\n');
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT Key is missing");
builder.Configuration["Jwt:Key"] = jwtKey;
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("JWT Key must contain at least 32 bytes");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

    // Схема создаётся отдельно с учётной записью для миграций.
    // При запуске приложения используются только права runtime-пользователя.
    await DbSeeder.SeedAsync(
        db,
        hasher,
        builder.Configuration["BootstrapAdmin:Username"],
        builder.Configuration["BootstrapAdmin:Password"]);
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Простая обработка ошибок (требование лабы)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Internal Server Error\"}");
    });
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "images")),
    RequestPath = "/images"
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/version", () => Results.Ok(new
{
    version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev"
}));

app.Run();
