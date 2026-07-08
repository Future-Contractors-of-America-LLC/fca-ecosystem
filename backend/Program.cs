using Serilog;
using AuricruxBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = builder.Configuration;

// Serilog setup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = config["AllowedOrigins"]?.Split(',') ?? 
        new[] { "http://localhost:3000", "http://localhost:3001", "http://localhost:5173" };

    options.AddPolicy("AllowFrontends", policyBuilder =>
    {
        policyBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add HttpClient for external service calls
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<ITtsService, TtsService>();
builder.Services.AddScoped<IPromptBuilderService, PromptBuilderService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontends");

app.UseRouting();

app.MapControllers();

// Log startup info
Log.Information("🚀 Auricrux Backend (OSS) starting up");
Log.Information("📡 LLM Provider: {LlmProvider}", config["LLM:Provider"] ?? "openai");
Log.Information("🧠 Model: {Model}", config["LLM:Model"] ?? "gpt-4");
Log.Information("🔊 TTS Provider: {TtsProvider}", config["TTS:Provider"] ?? "coqui");
Log.Information("📍 Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();
