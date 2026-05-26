using LocalScope.Api.Endpoints;
using LocalScope.Api.Middleware; // Подключили пространство имен для Middleware
using LocalScope.Domain.Abstractions;
using LocalScope.Infrastructure.OpenMeteo;
using LocalScope.Infrastructure.Yandex;
using LocalScope.Infrastructure.TwoGis;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку OpenAPI (Swagger)
builder.Services.AddOpenApi();

// Настройка CORS (Разрешаем фронтенду на Blazor/MAUI слать запросы к API)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// --- РЕГИСТРАЦИЯ СЕРВИСОВ ИЗ СЛОЯ INFRASTRUCTURE (Dependency Injection) ---
// HttpClient регистрируем отдельно для каждого провайдера, чтобы внутри них 
// можно было гибко настраивать BaseAddress, таймауты и заголовки.
builder.Services.AddHttpClient<IGeocodingProvider, YandexGeocoderClient>(client => {
    client.BaseAddress = new Uri("https://geocode-maps.yandex.ru/1.x/");
});
builder.Services.AddHttpClient<IPlacesProvider, TwoGisSearchClient>(client => {
    client.BaseAddress = new Uri("https://catalog.api.2gis.com/");
});
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoClient>(client => {
    client.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
});
// -------------------------------------------------------------------------
builder.WebHost.UseUrls("http://192.168.31.203:5108/");

var app = builder.Build();

// Включаем твое глобальное Middleware для централизованной обработки ошибок.
// Важно: оно должно стоять самым первым, чтобы ловить ошибки из всех слоев ниже!
app.UseErrorHandling();

// Включаем CORS middleware
app.UseCors();
app.UseDefaultFiles();    // ← Добавь
app.UseStaticFiles();     // ← Добавь
// Настройка Swagger для среды разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- ПОДКЛЮЧАЕМ МАРШРУТЫ ИЗ ПАПКИ ENDPOINTS ---
app.MapGeocodeEndpoints();
app.MapPlacesEndpoints();
app.MapSessionEndpoints();
app.MapWeatherEndpoints();
app.MapFavoritesEndpoints();
// ----------------------------------------------

app.Run();