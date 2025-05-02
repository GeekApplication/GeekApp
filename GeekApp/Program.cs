using Blazored.LocalStorage;
using GeekApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage; // Add this


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor(); // Add this line
builder.Services.AddProtectedBrowserStorage();  // Add this line
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<GeekApp.Server.Services.ITmdbService, GeekApp.Server.Services.TmdbService>(); // Add this
builder.Services.AddScoped<GeekApp.Server.Services.CachedTmdbService>(); // If using caching
builder.Services.AddMemoryCache(); // Required for CachedTmdbService
builder.Services.AddScoped<BackendService>();
builder.Services.AddHttpClient("Backend", client => 
{
    client.BaseAddress = new Uri("https://localhost:7282"); // Your backend URL
});

builder.Services.AddLogging();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();

builder.Services.AddHttpClient("AuthorizedAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7282"); // Your backend
});

// Add this to your services
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NotAuthenticated", policy =>
    {
        policy.RequireAssertion(context =>
            context.User.Identity == null || !context.User.Identity.IsAuthenticated);
    });
});

builder.Services.AddLogging(config =>
{
    config.AddDebug();
    config.AddConsole();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host"); // ⬅ Important line

app.Run();
