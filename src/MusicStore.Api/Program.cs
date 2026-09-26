using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MusicStore.Api.Data;
using MusicStore.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Подключение к БД (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// 3. Регистрируем наши кастомные сервисы
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();

// 4. Настройка JWT аутентификации
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing");
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

// 5. Настройка конвейера обработки запросов (Middleware)
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

// ВАЖНО: Порядок middleware! Сначала аутентификация, потом авторизация.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();