using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskApp.API.Middleware;
using TaskApp.Application.Interfaces;
using TaskApp.Application.Services;
using TaskApp.Domain.Interfaces.Repositories;
using TaskApp.Infrastructure.Auth;
using TaskApp.Infrastructure.Persistence;
using TaskApp.Infrastructure.Persistence.Repositories;
using TaskApp.Infrastructure.Seeder;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// MongoDB
string mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
string mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "taskapp";
MongoDbContext mongoDbContext = new MongoDbContext(mongoConnectionString, mongoDatabaseName);
builder.Services.AddSingleton(mongoDbContext);

// Repositories
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// JWT config
string jwtSecret = builder.Configuration["Jwt:Secret"] ?? "supersecretkey_that_is_long_enough_32chars!";
string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskApp";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TaskAppUsers";
int jwtExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out int expiry) ? expiry : 60;

// Infrastructure services
builder.Services.AddSingleton<IJwtTokenGenerator>(
    new JwtTokenGenerator(jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Application services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDemoSeedService, DemoSeedService>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddAppRateLimiting();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Seed database
using (IServiceScope scope = app.Services.CreateScope())
{
    DatabaseSeeder seeder = new DatabaseSeeder(mongoDbContext);
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
