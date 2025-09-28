using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using registroAsistencia.Data;
using registroAsistencia.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "QuantumAttend.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios personalizados
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDataService, EfDataService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IQrService, QrService>();
builder.Services.AddSingleton<ICsvService, CsvService>();
builder.Services.AddSingleton<ILoggingService, ConsoleLoggingService>();

var app = builder.Build();

// Datos de prueba iniciales (comentados para evitar duplicados)
// using (var scope = app.Services.CreateScope())
// {
//     var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
//     // Los datos se crean una sola vez. Usa la API para crear más datos.
// }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Logging de requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"🌍 REQUEST: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    Console.WriteLine($"🌍 User-Agent: {context.Request.Headers.UserAgent}");
    await next();
    Console.WriteLine($"🌍 RESPONSE: {context.Response.StatusCode} {context.Response.ContentType}");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();