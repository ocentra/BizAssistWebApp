using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.DependencyCollector;
using Azure.Communication.CallAutomation;
using BizAssistWebApp.Controllers.Services;
using BizAssistWebApp.Data;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Load configuration
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
string connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton(new ConfigurationValues(configuration, builder.Environment));

builder.Services.AddSingleton(sp =>
{
    ConfigurationValues configValues = sp.GetRequiredService<ConfigurationValues>();
    return new CallAutomationClient(configValues.CommunicationServicesConnectionString);
});

// Register the Speech services
builder.Services.AddSingleton(sp =>
{
    ConfigurationValues configValues = sp.GetRequiredService<ConfigurationValues>();
    return new SpeechToTextService(configValues.SpeechKey, configValues.SpeechRegion);
});
builder.Services.AddSingleton(sp =>
{
    ConfigurationValues configValues = sp.GetRequiredService<ConfigurationValues>();
    return new TextToSpeechService(configValues.SpeechKey, configValues.SpeechRegion);
});

// Register the AssistantManager
builder.Services.AddSingleton(sp =>
{
    ConfigurationValues configValues = sp.GetRequiredService<ConfigurationValues>();
    return new AssistantManager(configValues.AssistantIds);
});

// Register CallHandler
builder.Services.AddScoped<CallHandler>();

builder.Services.AddSingleton<IWebSocketServerFactory, WebSocketServerFactory>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
});

// Add QuickPulseTelemetryModule for Live Metrics
builder.Services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
builder.Services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<WebApplication>>();

    if (context.Request.Path == "/media-streaming")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocketServerFactory = context.RequestServices.GetRequiredService<IWebSocketServerFactory>();
            var webSocketServer = webSocketServerFactory.Create(context);
            webSocketServer.WebSocket = await context.WebSockets.AcceptWebSocketAsync();
            await webSocketServer.ProcessWebSocketAsync();
            logger.LogInformation("WebSocket request processed at /media-streaming");
        }
        else
        {
            context.Response.StatusCode = 400;
            logger.LogWarning("Non-WebSocket request received at /media-streaming");
        }
    }
    else
    {
        logger.LogInformation($"HTTP request received at {context.Request.Path}");
        await next();
    }
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
