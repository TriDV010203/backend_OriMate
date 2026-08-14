using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrigamiPlatform.API.Middleware;
using OrigamiPlatform.API.Options;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure;
using OrigamiPlatform.Infrastructure.Persistence;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    // Dev-only: log every SQL statement EF Core issues, so N+1 query patterns are visible
    // in the console while iterating locally. Never enabled outside Development.
    if (builder.Environment.IsDevelopment())
    {
        opt.LogTo(Console.WriteLine, new[] { "Microsoft.EntityFrameworkCore.Database.Command" }, LogLevel.Information)
           .EnableSensitiveDataLogging();
    }
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection("SePay"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT. Ví dụ: eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "https://orimate-web.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await SeedData.SeedAsync(seedContext, seedHasher);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();  // ← Chỉ bật khi production
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }