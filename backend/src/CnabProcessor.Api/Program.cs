// ========================================
// File: CnabProcessor.Web/Program.cs
// Purpose: Application entry point with all configurations
// ========================================

using CnabProcessor.Api.Filters;
using CnabProcessor.Api.Services;
using CnabProcessor.Domain.Interfaces;
using CnabProcessor.Domain.Services;
using CnabProcessor.Infrastructure.Data;
using CnabProcessor.Infrastructure.Interfaces;
using CnabProcessor.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cnab-processor-.txt", rollingInterval: Serilog.RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CNAB Processor application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllersWithViews(options =>
    {
        // Add global exception filter
        options.Filters.Add<GlobalExceptionFilter>();
    });

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CNAB Processor API",
            Version = "v1",
            Description = "API for processing Brazilian CNAB (Centro Nacional de Automação Bancária) financial transaction files. " +
                          "This system parses fixed-width text files containing financial transactions and stores them in a SQL Server database.",
            Contact = new OpenApiContact
            {
                Name = "Edson Mata",
                Email = "edsonmata@hotmail.com",
                Url = new Uri("https://www.linkedin.com/in/edsonsilvaa")
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer eyJhbGc...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments for better API documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Database Configuration with Retry Logic
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<CnabDbContext>(options =>
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            // Enable retry on failure for better resilience
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // Command timeout for long-running queries
            sqlServerOptions.CommandTimeout(60);
        }));

    // Dependency Injection - Services
    builder.Services.AddScoped<ICnabParser, CnabParserService>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddScoped<JwtTokenService>();

    // JWT Authentication Configuration (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

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
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });
    }

    builder.Services.AddAuthorization();

    // CORS (if needed for API consumption)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
    });

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        // Apply database migrations automatically
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            var retryCount = 0;
            const int maxRetries = 30;
            var success = false;

            while (retryCount < maxRetries && !success)
            {
                try
                {
                    var context = services.GetRequiredService<CnabDbContext>();

                    logger.LogInformation("Attempting to connect to database... Attempt {RetryCount}/{MaxRetries}",
                        retryCount + 1, maxRetries);

                    // Test connection
                    await context.Database.CanConnectAsync();
                    logger.LogInformation("Database connection established successfully");

                    // Apply pending migrations
                    logger.LogInformation("Applying database migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");

                    success = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    logger.LogWarning(ex,
                        "Failed to connect to database. Attempt {RetryCount}/{MaxRetries}. Error: {ErrorMessage}",
                        retryCount, maxRetries, ex.Message);

                    if (retryCount >= maxRetries)
                    {
                        logger.LogError(ex,
                            "Failed to connect to database after {MaxRetries} attempts. Application will start but database may be unavailable.",
                            maxRetries);
                        break;
                    }

                    logger.LogInformation("Waiting 5 seconds before retry...");
                    await Task.Delay(5000);
                }
            }
        }
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CNAB Processor API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "CNAB Processor API Documentation";
        });
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map MVC routes
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Log application startup info
    var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
    Log.Information("=========================================");
    Log.Information("🚀 CNAB Processor - Application Started");
    Log.Information("=========================================");
    Log.Information("📍 URLs: {Urls}", urls);
    Log.Information("🗄️  Database: SQL Server");
    Log.Information("📊 Swagger: {SwaggerUrl}", $"{urls}/swagger");
    Log.Information("❤️  Health: {HealthUrl}", $"{urls}/health");
    Log.Information("🌍 Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("=========================================");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ========================================
// Expose Program class for integration tests
// ========================================
public partial class Program { }