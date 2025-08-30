using registroAsistencia.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicios personalizados
builder.Services.AddSingleton<IDataService, InMemoryDataService>();
builder.Services.AddSingleton<IQrService, QrService>();
builder.Services.AddSingleton<ICsvService, CsvService>();
builder.Services.AddSingleton<ILoggingService, ConsoleLoggingService>();

var app = builder.Build();

// Datos de prueba iniciales
using (var scope = app.Services.CreateScope())
{
    var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
    var qrService = scope.ServiceProvider.GetRequiredService<IQrService>();
    
    // Crear alumnos de ejemplo
    var alumno1 = await dataService.CreateAlumnoAsync(new registroAsistencia.Models.AlumnoCreateDto("EST001", "Juan Pérez"));
    var alumno2 = await dataService.CreateAlumnoAsync(new registroAsistencia.Models.AlumnoCreateDto("EST002", "María García"));
    var alumno3 = await dataService.CreateAlumnoAsync(new registroAsistencia.Models.AlumnoCreateDto("EST003", "Carlos López"));
    
    // Agregar QR a los alumnos
    alumno1.QrAlumnoBase64 = qrService.GenerateBase64Qr($"alumno:{alumno1.Id}");
    alumno2.QrAlumnoBase64 = qrService.GenerateBase64Qr($"alumno:{alumno2.Id}");
    alumno3.QrAlumnoBase64 = qrService.GenerateBase64Qr($"alumno:{alumno3.Id}");
    
    // Crear una clase de ejemplo
    await dataService.CreateClaseAsync(new registroAsistencia.Models.ClaseCreateDto("Matemáticas I"));
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

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