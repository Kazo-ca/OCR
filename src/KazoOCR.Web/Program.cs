using KazoOCR.Web.Components;
using KazoOCR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment variable or use default
var port = Environment.GetEnvironmentVariable("KAZO_WEB_PORT") ?? "5001";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure typed HttpClient for API communication
var apiBaseUrl = Environment.GetEnvironmentVariable("KAZO_API_BASE_URL") ?? "http://api:5000";
builder.Services.AddHttpClient<IKazoApiClient, KazoApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register authentication state service
builder.Services.AddScoped<AuthStateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

namespace KazoOCR.Web
{
    /// <summary>
    /// Partial class for WebApplicationFactory integration testing.
    /// </summary>
    public partial class Program { }
}
