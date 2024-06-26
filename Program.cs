using BizAssistWebApp.Controllers.Services;
using BizAssistWebApp.Data;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
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
    return new CommunicationTokenService(configValues.CommunicationServicesConnectionString);
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

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
});

// Add QuickPulseTelemetryModule for Live Metrics
builder.Services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
builder.Services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>();

// Add IHttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register WebSocketServer as a singleton
// Register WebSocketServer as a singleton
builder.Services.AddSingleton(sp =>
{
    ConfigurationValues configValues = sp.GetRequiredService<ConfigurationValues>();
    SpeechToTextService speechToTextService = sp.GetRequiredService<SpeechToTextService>();
    TextToSpeechService textToSpeechService = sp.GetRequiredService<TextToSpeechService>();
    AssistantManager assistantManager = sp.GetRequiredService<AssistantManager>();
    ILogger<WebSocketServer> logger = sp.GetRequiredService<ILogger<WebSocketServer>>();
    IHttpContextAccessor httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new WebSocketServer(configValues, logger, speechToTextService, textToSpeechService, assistantManager, httpContextAccessor);
});




// Add Application Insights


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
    ILogger<WebApplication> logger = context.RequestServices.GetRequiredService<ILogger<WebApplication>>();

    if (context.Request.Path == "/media-streaming")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            string uri = $"wss://{context.Request.Host}/media-streaming";
            WebSocketServer webSocketServer = context.RequestServices.GetRequiredService<WebSocketServer>();
            webSocketServer.Uri = uri ;
            webSocketServer.WebSocket = await context.WebSockets.AcceptWebSocketAsync();
            await webSocketServer.InitWebSocketAsync();
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
