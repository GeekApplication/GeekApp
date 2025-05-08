using Microsoft.AspNetCore.Components.Authorization;
using GeekApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddLogging(config =>
{
    config.AddDebug();
    config.AddConsole();
});

// Authentication and authorization
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("NotAuthenticated", policy =>
        policy.RequireAssertion(context => context.User.Identity == null || !context.User.Identity.IsAuthenticated));
});

// Backend services
builder.Services.AddScoped<GeekApp.Server.Services.ITmdbService, GeekApp.Server.Services.TmdbService>();
builder.Services.AddScoped<GeekApp.Server.Services.CachedTmdbService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IListService, ListService>();
builder.Services.AddScoped<JwtAuthMessageHandler>();

// HTTP client for backend API
builder.Services.AddHttpClient("AuthorizedAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7282");
}).AddHttpMessageHandler<JwtAuthMessageHandler>();

// Plain HttpClient for login/register
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();