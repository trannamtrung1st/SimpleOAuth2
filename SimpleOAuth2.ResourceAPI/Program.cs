using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleOAuth2.ResourceAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Bind(nameof(AppSettings), AppSettings);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Simple API",
        Description = "A simple example ASP.NET Core Web API",
        TermsOfService = new Uri("https://example.com/terms"),
    });

    #region Bearer/Api Key
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter into field the word 'Bearer' following by space and JWT",
            Name = HeaderNames.Authorization,
            Type = SecuritySchemeType.ApiKey
        });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[0]
        }
    });
    #endregion
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // JWT tokens
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        opt.Authority = AppSettings.IdpUrl;
        opt.Audience = "resource_api";
        //opt.TokenValidationParameters.ClockSkew = TimeSpan.Zero; // demo only: since local will not have clock skew
    })
    // Reference tokens
    .AddOAuth2Introspection("Introspection", opt =>
    {
        opt.Authority = AppSettings.IdpUrl;
        opt.ClientId = "resource_api";
        opt.ClientSecret = "resource_api_secret";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           summaries[Random.Shared.Next(summaries.Length)]
       )).ToArray();
    return forecast;
})
    .RequireAuthorization()
    .WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program
{
    public static AppSettings AppSettings { get; } = new AppSettings();
}