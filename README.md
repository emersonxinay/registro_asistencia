# Sistema de Registro de Asistencia

Aplicación web ASP.NET Core para el registro de asistencia de estudiantes mediante códigos QR.

## Funcionalidades Actuales

### Implementado
- **Registro de alumnos** con código y nombre
- **Gestión de clases** por asignatura con fechas de inicio/fin
- **Códigos QR dinámicos** para alumnos y clases
- **Dos métodos de registro de asistencia**:
  - Profesor escanea QR del alumno
  - Alumno escanea QR de la clase
- **Visualización de asistencias** por clase o general
- **Exportación a CSV** de registros de asistencia
- **Dashboard web** con interfaz para gestión
- **API REST** completa con Swagger
- **Scanner de QR** integrado en la aplicación web

### Tecnologías
- .NET 9.0
- ASP.NET Core MVC
- QRCoder para generación de códigos QR
- Almacenamiento en memoria (concurrente)
- Swagger/OpenAPI para documentación de API

## Requisitos

- .NET 9.0 SDK
- Navegador web moderno
- Cámara (para escaneado de QR)

## Instalación y Ejecución

1. Clonar el repositorio
2. Restaurar dependencias:
   ```
   dotnet restore
   ```
3. Ejecutar la aplicación:
   ```
   dotnet run
   ```
4. Acceder a la aplicación en `https://localhost:5001` o `http://localhost:5000`

## Uso

### Dashboard Principal
- Ver clases activas e inactivas
- Generar códigos QR para clases
- Acceder al scanner del docente

### Registro de Asistencia
1. **Método Profesor**: Usar el scanner del docente para escanear QR de alumnos
2. **Método Alumno**: Estudiantes escanean QR de la clase desde sus dispositivos

### Exportación de Datos
- Descargar asistencias en formato CSV
- Filtrar por clase específica
- Incluye información completa de alumno y clase

## API Endpoints

### Alumnos
- `GET /api/alumnos` - Listar alumnos
- `POST /api/alumnos` - Crear alumno
- `GET /api/alumnos/{id}` - Obtener alumno por ID
- `GET /api/alumnos/{id}/qr` - Obtener QR del alumno

### Clases
- `GET /api/clases` - Listar clases
- `POST /api/clases` - Crear clase
- `GET /api/clases/{id}` - Obtener clase por ID
- `POST /api/clases/{id}/close` - Cerrar clase
- `GET /api/clases/{id}/qr` - Generar QR para clase

### Asistencias
- `GET /api/asistencias` - Listar todas las asistencias
- `GET /api/asistencias/clase/{id}` - Asistencias por clase
- `POST /api/asistencias/profesor-scan` - Registro por profesor
- `POST /api/asistencias/alumno-scan` - Registro por alumno
- `GET /api/asistencias/csv` - Exportar CSV general
- `GET /api/asistencias/clase/{id}/csv` - Exportar CSV por clase

## Estructura del Proyecto

```
registroAsistencia/
├── Controllers/           # Controladores MVC y API
├── Models/               # Modelos de datos
├── Services/             # Servicios de negocio
├── Views/                # Vistas Razor
├── wwwroot/              # Archivos estáticos
└── Program.cs            # Punto de entrada
```

## Pendiente por Implementar

### Base de Datos Persistente
- Migrar de almacenamiento en memoria a base de datos
- Implementar Entity Framework Core
- Configurar migraciones

### Autenticación y Autorización
- Sistema de login para docentes
- Roles y permisos
- Seguridad en endpoints

### Funcionalidades Adicionales
- Reportes estadísticos de asistencia
- Notificaciones automáticas
- Gestión de horarios de clase
- Importación masiva de alumnos
- Configuración de tolerancia de llegadas tardías

### Mejoras de UI/UX
- Diseño responsive mejorado
- Modo offline para el scanner
- Notificaciones en tiempo real
- Validación de formularios del lado cliente

### Integración
- API para sistemas externos
- Sincronización con sistemas académicos
- Webhook para eventos de asistencia

## Datos de Prueba

La aplicación se inicia con datos de ejemplo:
- 3 alumnos: Juan Pérez (EST001), María García (EST002), Carlos López (EST003)
- 1 clase: Matemáticas I