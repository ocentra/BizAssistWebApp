using Azure.Communication.CallAutomation;
using BizAssistWebApp.Controllers.Services;
using BizAssistWebApp.Data;
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
    var configValues = sp.GetRequiredService<ConfigurationValues>();
    return new CallAutomationClient(configValues.CommunicationServicesConnectionString);
});

// Register the Speech services
builder.Services.AddSingleton(sp =>
{
    var configValues = sp.GetRequiredService<ConfigurationValues>();
    return new SpeechToTextService(configValues.SpeechKey, configValues.SpeechRegion);
});
builder.Services.AddSingleton(sp =>
{
    var configValues = sp.GetRequiredService<ConfigurationValues>();
    return new TextToSpeechService(configValues.SpeechKey, configValues.SpeechRegion);
});

// Register the AssistantManager
builder.Services.AddSingleton(sp =>
{
    var configValues = sp.GetRequiredService<ConfigurationValues>();
    return new AssistantManager(configValues.AssistantIds);
});

// Register CallHandler
builder.Services.AddScoped<CallHandler>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
