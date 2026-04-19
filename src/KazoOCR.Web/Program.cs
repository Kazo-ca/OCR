using KazoOCR.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001
var port = Environment.GetEnvironmentVariable("KAZO_WEB_PORT") ?? "5001";
builder.WebHost.UseUrls($"http://*:{port}");

// Get API base URL from environment
var apiBaseUrl = Environment.GetEnvironmentVariable("KAZO_API_BASE_URL") ?? "http://api:5000";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register typed HttpClient for API communication
builder.Services.AddHttpClient("KazoOcrApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
