using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- Services (dependency injection registrations) ---

// Controllers handle HTTP only. Enums travel as their names ("Todo", "High"),
// and numeric values such as 2 are rejected instead of silently accepted.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));

// Unexpected failures become a standard 500 ProblemDetails JSON body.
builder.Services.AddProblemDetails();

// Built-in health checks: GET /api/health answers 200 with the text "Healthy".
builder.Services.AddHealthChecks();

// Generates the OpenAPI document at /openapi/v1.json.
builder.Services.AddOpenApi();

var app = builder.Build();

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
