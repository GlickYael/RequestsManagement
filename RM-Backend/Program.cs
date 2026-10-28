using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.Infrastructure;
using RequestsManagement.Services;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigin = builder.Configuration["CorsSettings:AllowedOrigin"]
    ?? throw new InvalidOperationException("Missing required configuration: CorsSettings:AllowedOrigin");

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IRequestStatusAuditService, RequestStatusAuditService>();

builder.Services.AddScoped<ISeedService, SeedService>();

builder.Services.AddScoped<IRequestsService, RequestsService>();

builder.Services.AddMemoryCache();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// First in the pipeline so it can catch exceptions from everything after it
app.UseExceptionHandler();

app.UseCors("AllowAngular");

app.InitializeDatabase(args);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required so WebApplicationFactory<Program> (in the test project) can reference
// the Program class that is auto-generated from top-level statements.
public partial class Program { }
