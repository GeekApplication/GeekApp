using GeekApp.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Mongo and AuthService
builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// Register TMDB services with Polly policies
builder.Services.AddHttpClient("TmdbService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Tmdb:BaseUrl"]!);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Register TmdbService and CachedTmdbService
builder.Services.AddScoped<TmdbService>();
builder.Services.AddScoped<ITmdbService>(provider =>
    new CachedTmdbService(
        provider.GetRequiredService<TmdbService>(),
        provider.GetRequiredService<IMemoryCache>(),
        provider.GetRequiredService<ILogger<CachedTmdbService>>()));

//builder.Services.AddSingleton<IGenreService, GenreService>();

// Add Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", builder =>
    {
        builder.WithOrigins("https://localhost:7004")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowBlazorFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}