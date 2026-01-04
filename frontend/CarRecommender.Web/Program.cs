using CarRecommender.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure HttpClient voor CarApiClient
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] 
    ?? throw new InvalidOperationException(
        $"ApiSettings:BaseUrl is niet geconfigureerd. Environment: {builder.Environment.EnvironmentName}. " +
        $"Beschikbare configuratie keys: {string.Join(", ", builder.Configuration.AsEnumerable().Select(kvp => kvp.Key))}");

// Zorg dat BaseAddress eindigt met een slash voor correct gebruik met relatieve URLs
if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl = apiBaseUrl + "/";
}

// LOG: Toon welke API URL wordt gebruikt
Console.WriteLine($"[FRONTEND] API Base URL: {apiBaseUrl}");
Console.WriteLine($"[FRONTEND] Environment: {builder.Environment.EnvironmentName}");

builder.Services.AddHttpClient<CarApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    // Verhoog timeout voor ML evaluatie die lang kan duren (30-60 seconden)
    client.Timeout = TimeSpan.FromSeconds(120);
    // LOG: Bevestig configuratie
    Console.WriteLine($"[FRONTEND] HttpClient geconfigureerd met BaseAddress: {client.BaseAddress}");
    Console.WriteLine($"[FRONTEND] HttpClient Timeout: {client.Timeout.TotalSeconds} seconden");
    Console.WriteLine($"[FRONTEND] Test URL zou zijn: {client.BaseAddress}api/health");
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
