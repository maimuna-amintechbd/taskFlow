using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services (dependency injection registrations) ---

// Controllers handle HTTP only. Enums travel as their names ("Todo", "High"),
// and numeric values such as 2 are rejected instead of silently accepted.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));

// The OpenAPI document generator reads these "HTTP JSON options", not the MVC ones
// above. Registering the same converter here makes the document describe status and
// priority as their names ("Todo", "InProgress", "Done") instead of integers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));

// Unexpected failures become a standard 500 ProblemDetails JSON body.
builder.Services.AddProblemDetails();

// Built-in health checks: GET /api/health answers 200 with the text "Healthy".
builder.Services.AddHealthChecks();

// Generates the OpenAPI document at /openapi/v1.json.
builder.Services.AddOpenApi();

// One TaskFlowDbContext per request, backed by SQLite. The connection string is read
// when the context is first created (not now), so tests can swap it with UseSetting.
builder.Services.AddDbContext<TaskFlowDbContext>((serviceProvider, options) =>
    options.UseSqlite(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("TaskFlow")));

// The app's clock. Services ask it for "now" instead of calling DateTime.Now,
// so tests can replace it with a fixed time. Singleton: one shared instance.
builder.Services.AddSingleton(TimeProvider.System);

// Business rules and data access. Scoped = one instance per request.
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

// Create the database file (if missing) and apply any pending migrations at startup,
// so there is no manual SQL step. A scope is needed because the DbContext is scoped.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    await db.Database.MigrateAsync();
}

// --- HTTP pipeline (middleware runs in this order for every request) ---

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Swagger UI page at /swagger, reading the document generated above.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TaskFlow API v1"));
}

// No HTTPS redirection and no CORS: the app is HTTP-only on localhost, and the
// browser reaches the API through the Vite proxy (same origin).

app.MapHealthChecks("/api/health");
app.MapControllers();

app.Run();
