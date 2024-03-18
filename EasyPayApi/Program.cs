using EasyPayApi.Services;
using Microsoft.ApplicationInsights; 

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddApplicationInsights(
        configureTelemetryConfiguration: (config) =>
            config.ConnectionString = builder.Configuration.GetSection("ApplicationInsights")["ConnectionString"],
            configureApplicationInsightsLoggerOptions: (options) => { });

builder.Services.AddApplicationInsightsTelemetry(builder.Configuration.GetSection("ApplicationInsights")["ConnectionString"]);

// Enable CORS
builder.Services.AddCors(setup => {
    setup.AddPolicy("default", (options) => {
        options
            .WithOrigins(
                "http://localhost:8080",
                "https://localhost:7088",
                "https://easypaytest-80d7a65f94b6.herokuapp.com",
                "https://easypayapitest.azurewebsites.net"
            )
            .AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<AzureBlobStorageService>();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("default");

app.MapControllers();

app.Run();
