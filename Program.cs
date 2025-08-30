using System.Collections.Concurrent;
using QRCoder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======================== "BD" en memoria ========================
var alumnos = new ConcurrentDictionary<int, Alumno>();
var clases  = new ConcurrentDictionary<int, Clase>();
var asist   = new ConcurrentBag<Asistencia>();
var tokens  = new ConcurrentDictionary<string, QrClaseToken>(); // nonce -> token
int alumnoSeq = 0, claseSeq = 0;

// ======================== Helpers QR =============================
// Devuelve PNG en Base64 (para JSON)
static string PngBase64(string text)
{
    using var gen = new QRCodeGenerator();
    using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
    var png = new PngByteQRCode(data);
    byte[] bytes = png.GetGraphic(20);
    return Convert.ToBase64String(bytes);
}

// Devuelve bytes PNG (para endpoint image/png)
static byte[] PngBytes(string text)
{
    using var gen = new QRCodeGenerator();
    using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
    var png = new PngByteQRCode(data);
    return png.GetGraphic(20);
}

// ======================== Endpoints mínimos ======================
// Crear alumno (genera QR permanente del alumno)
app.MapPost("/api/alumnos", (AlumnoCreateDto dto) =>
{
    var id = Interlocked.Increment(ref alumnoSeq);
    var a = new Alumno {
        Id = id,
        Codigo = dto.Codigo,
        Nombre = dto.Nombre,
        QrAlumnoBase64 = PngBase64($"alumno:{id}") // QR permanente del alumno
    };
    alumnos[id] = a;
    return Results.Ok(a);
});

// Abrir clase
app.MapPost("/api/clases", (ClaseCreateDto dto) =>
{
    var id = Interlocked.Increment(ref claseSeq);
    var c = new Clase { Id = id, Asignatura = dto.Asignatura, InicioUtc = DateTime.UtcNow };
    clases[id] = c;
    return Results.Ok(c);
});

// Cerrar clase
app.MapPost("/api/clases/{id:int}/cerrar", (int id) =>
{
    if (!clases.TryGetValue(id, out var c)) return Results.NotFound();
    if (c.FinUtc is not null) return Results.BadRequest("La clase ya está cerrada.");
    c.FinUtc = DateTime.UtcNow;
    return Results.Ok(c);
});

// =================== QR de clase (JSON con base64) ===================
// (Usa payload en URL para abrir /scan con claseId y nonce)
app.MapGet("/api/clases/{id:int}/qr", (HttpRequest req, int id) =>
{
    if (!clases.TryGetValue(id, out var c)) return Results.NotFound();
    if (!c.Activa) return Results.BadRequest("Clase no activa.");

    var nonce = Guid.NewGuid().ToString("N");
    var tk = new QrClaseToken { ClaseId = id, Nonce = nonce, ExpiraUtc = DateTime.UtcNow.AddSeconds(90) };
    tokens[nonce] = tk;

    // Detecta host actual para armar URL de /scan (ajusta si despliegas detrás de proxy)
    var host = $"{req.Scheme}://{req.Host}";
    var payloadUrl = $"{host}/scan?claseId={id}&nonce={nonce}";
    var base64 = PngBase64(payloadUrl);

    return Results.Ok(new { base64Png = base64, expiraUtc = tk.ExpiraUtc, url = payloadUrl });
});

// =================== QR de clase (PNG directo) =======================
app.MapGet("/api/clases/{id:int}/qr.png", (HttpRequest req, int id) =>
{
    if (!clases.TryGetValue(id, out var c)) return Results.NotFound();
    if (!c.Activa) return Results.BadRequest("Clase no activa.");

    var nonce = Guid.NewGuid().ToString("N");
    var tk = new QrClaseToken { ClaseId = id, Nonce = nonce, ExpiraUtc = DateTime.UtcNow.AddSeconds(90) };
    tokens[nonce] = tk;

    var host = $"{req.Scheme}://{req.Host}";
    var payloadUrl = $"{host}/scan?claseId={id}&nonce={nonce}";
    var bytes = PngBytes(payloadUrl);

    return Results.File(bytes, "image/png", $"qr-clase-{id}.png");
});

// =================== Asistencias ===============================
// Flujo A: Docente escanea QR del alumno (usa alumnoId)
app.MapPost("/api/asistencias/profesor-scan", (ProfesorScanDto dto) =>
{
    if (!clases.TryGetValue(dto.ClaseId, out var c)) return Results.NotFound("Clase no existe");
    if (!c.Activa) return Results.BadRequest("Clase no activa");
    if (!alumnos.ContainsKey(dto.AlumnoId)) return Results.NotFound("Alumno no existe");

    if (asist.Any(a => a.AlumnoId == dto.AlumnoId && a.ClaseId == dto.ClaseId))
        return Results.Ok(new { mensaje = "Asistencia ya registrada" });

    asist.Add(new Asistencia {
        Id = asist.Count + 1,
        AlumnoId = dto.AlumnoId,
        ClaseId = dto.ClaseId,
        MarcadaUtc = DateTime.UtcNow,
        Metodo = "PROFESOR_ESCANEA"
    });
    return Results.Ok(new { mensaje = "Asistencia registrada (profesor escanea)" });
});

// Flujo B: Alumno escanea QR de clase (nonce rotativo)
app.MapPost("/api/asistencias/alumno-scan", (AlumnoScanDto dto) =>
{
    if (!clases.TryGetValue(dto.ClaseId, out var c)) return Results.NotFound("Clase no existe");
    if (!c.Activa) return Results.BadRequest("Clase no activa");
    if (!alumnos.ContainsKey(dto.AlumnoId)) return Results.NotFound("Alumno no existe");

    if (!tokens.TryGetValue(dto.Nonce, out var tk)) return Results.BadRequest("Nonce inválido");
    if (tk.ClaseId != dto.ClaseId) return Results.BadRequest("Nonce no corresponde a la clase");
    if (DateTime.UtcNow > tk.ExpiraUtc) return Results.BadRequest("Nonce expirado");

    // consumir nonce (one-time)
    tokens.TryRemove(dto.Nonce, out _);

    if (asist.Any(a => a.AlumnoId == dto.AlumnoId && a.ClaseId == dto.ClaseId))
        return Results.Ok(new { mensaje = "Asistencia ya registrada" });

    asist.Add(new Asistencia {
        Id = asist.Count + 1,
        AlumnoId = dto.AlumnoId,
        ClaseId = dto.ClaseId,
        MarcadaUtc = DateTime.UtcNow,
        Metodo = "ALUMNO_ESCANEA"
    });
    return Results.Ok(new { mensaje = "Asistencia registrada (alumno escanea)" });
});

// Listar asistencias de una clase (debug)
app.MapGet("/api/clases/{id:int}/asistencias", (int id) =>
{
    var lista = asist.Where(a => a.ClaseId == id).ToList();
    return Results.Ok(lista);
});

// =================== Vista mínima (QR rotativo) ======================
app.MapGet("/", () => Results.Content(@"
<!doctype html>
<html lang='es'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>QR Clase (demo)</title>
<style>
  body { font-family: system-ui; max-width: 780px; margin: auto; padding: 24px; }
  img { width: 320px; height: 320px; border: 1px solid #ddd; border-radius: 12px; }
  .row { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
  input { padding: 8px 10px; width: 100px; }
  button { padding: 8px 12px; cursor: pointer; }
</style>
</head>
<body>
  <h1>QR de Clase (rotativo)</h1>
  <div class='row'>
    <label>Clase ID: <input id='claseId' value='1'></label>
    <button id='btn'>Refrescar QR</button>
    <span id='status'></span>
  </div>
  <p><small>Tip: este QR incluye una URL a <code>/scan?claseId=...&nonce=...</code>. Los alumnos lo escanean y se registran.</small></p>
  <img id='qr' alt='QR de la clase' />
  <script>
    async function loadQr(){
      const id = document.getElementById('claseId').value;
      const r = await fetch(`/api/clases/${id}/qr`);
      const ok = r.ok;
      const j = await r.json().catch(()=>null);
      const s = document.getElementById('status');
      if(!ok || !j){ s.textContent = 'No se pudo obtener QR'; return; }
      document.getElementById('qr').src = 'data:image/png;base64,' + j.base64Png;
      s.textContent = 'Expira: ' + new Date(j.expiraUtc).toLocaleString() + ' | URL: ' + j.url;
    }
    document.getElementById('btn').onclick = loadQr;
    setInterval(loadQr, 60000); // rota cada 60s
    loadQr();
  </script>
</body>
</html>", "text/html"));

// =================== Página de escaneo (alumno) ======================
app.MapGet("/scan", (int claseId, string nonce) =>
{
    var html = $@"
<!doctype html>
<html lang='es'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Registro de Asistencia</title>
<style>
  body {{ font-family: system-ui; max-width: 680px; margin: auto; padding: 24px; }}
  input {{ padding: 8px 10px; width: 160px; }}
  button {{ padding: 8px 12px; cursor: pointer; }}
  .row {{ display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }}
  #msg {{ margin-top: 12px; font-weight: 600; }}
</style>
</head>
<body>
  <h1>Registro de Asistencia</h1>
  <p>Clase: <b>{claseId}</b></p>
  <div class='row'>
    <label>Alumno ID:
      <input id='alumnoId' type='number' min='1' placeholder='Ej: 1' />
    </label>
    <button id='btn'>Marcar asistencia</button>
  </div>
  <p id='msg'></p>
  <script>
    async function marcar(){{
      const alumnoId = document.getElementById('alumnoId').value;
      const msg = document.getElementById('msg');
      if(!alumnoId){{ msg.textContent = 'Ingresa tu Alumno ID.'; return; }}
      try {{
        const r = await fetch('/api/asistencias/alumno-scan', {{
          method: 'POST',
          headers: {{ 'Content-Type': 'application/json' }},
          body: JSON.stringify({{ alumnoId: Number(alumnoId), claseId: {claseId}, nonce: '{nonce}' }})
        }});
        const j = await r.json().catch(()=>({{}}));
        msg.textContent = r.ok ? (j.mensaje || 'Asistencia registrada') : (j || 'Error al registrar');
      }} catch (e) {{
        msg.textContent = 'Error de red';
      }}
    }}
    document.getElementById('btn').onclick = marcar;
  </script>
</body>
</html>";
    return Results.Content(html, "text/html");
});

app.Run();

// ======================== DTOs y modelos =========================
record AlumnoCreateDto(string Codigo, string Nombre);
record ClaseCreateDto(string Asignatura);
record ProfesorScanDto(int AlumnoId, int ClaseId);
record AlumnoScanDto(int AlumnoId, int ClaseId, string Nonce);

public class Alumno
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string QrAlumnoBase64 { get; set; } = "";
}

public class Clase
{
    public int Id { get; set; }
    public string Asignatura { get; set; } = "";
    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public bool Activa => FinUtc is null;
}

public class Asistencia
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int ClaseId { get; set; }
    public DateTime MarcadaUtc { get; set; }
    public string Metodo { get; set; } = "";
}

public class QrClaseToken
{
    public int ClaseId { get; set; }
    public string Nonce { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
}
