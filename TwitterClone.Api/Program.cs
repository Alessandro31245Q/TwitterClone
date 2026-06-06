using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TwitterClone.Api.Middleware;
using TwitterClone.Application.Interfaces;
using TwitterClone.Domain.Entities;
using TwitterClone.Infrastructure.Data;
using TwitterClone.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Database — Entity Framework Core with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
b => b.MigrationsAssembly("TwitterClone.Infrastructure")));


// Identity — ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;

        // Lockout policy
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();


// Authentication with JWT Bearer tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

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
            ValidateLifetime  = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// APPLICATION SERVICES — Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();

// CONTROLLERS
builder.Services.AddControllers();

// SWAGGER / OPENAPI — with JWT suppor
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "TwitterClone API",
        Version     = "v1",
        Description = "REST API for TwitterClone — Authentication module"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: eyJhbGciOiJI..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS (permissive for development — restrict in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Build
var app = builder.Build();

// Auto-Apply Migrations (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TwitterClone API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("DevPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
