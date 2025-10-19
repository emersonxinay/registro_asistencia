// ==========================================
// SISTEMA DE REPORTES - JAVASCRIPT
// ==========================================

let cursosCache = [];
let ramosCache = [];
let alumnosCache = [];
let reporteActual = null;

// ==========================================
// INICIALIZACIÓN
// ==========================================
document.addEventListener('DOMContentLoaded', function() {
    console.log('[Reportes] Inicializando sistema de reportes...');

    // Cargar cursos al inicio
    cargarCursos();

    // Configurar fechas por defecto (último mes)
    configurarFechasPorDefecto();

    // Event Listeners
    document.getElementById('tipoReporte').addEventListener('change', onTipoReporteChange);
    document.getElementById('curso').addEventListener('change', onCursoChange);
    document.getElementById('btnGenerarReporte').addEventListener('click', generarReporte);
    document.getElementById('btnExportarCSV').addEventListener('click', exportarCSV);
    document.getElementById('btnLimpiar').addEventListener('click', limpiarFiltros);

    // Inicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    console.log('[Reportes] Sistema inicializado correctamente');
});

// ==========================================
// CONFIGURACIÓN INICIAL
// ==========================================
function configurarFechasPorDefecto() {
    const hoy = new Date();
    const hace30Dias = new Date();
    hace30Dias.setDate(hoy.getDate() - 30);

    document.getElementById('fechaInicio').valueAsDate = hace30Dias;
    document.getElementById('fechaFin').valueAsDate = hoy;
}

// ==========================================
// CARGA DE DATOS
// ==========================================
async function cargarCursos() {
    try {
        const response = await fetch('/api/reportes/cursos');
        if (!response.ok) throw new Error('Error al cargar cursos');

        cursosCache = await response.json();

        const selectCurso = document.getElementById('curso');
        selectCurso.innerHTML = '<option value="">-- Seleccionar Curso --</option>';

        cursosCache.forEach(curso => {
            const option = document.createElement('option');
            option.value = curso.id;
            option.textContent = `${curso.nombre} (${curso.codigo})`;
            selectCurso.appendChild(option);
        });

        console.log('[Reportes] Cursos cargados:', cursosCache.length);
    } catch (error) {
        console.error('[Reportes] Error al cargar cursos:', error);
        mostrarError('Error al cargar la lista de cursos');
    }
}

async function cargarRamosPorCurso(cursoId) {
    try {
        const response = await fetch(`/api/reportes/cursos/${cursoId}/ramos`);
        if (!response.ok) throw new Error('Error al cargar ramos');

        ramosCache = await response.json();

        const selectRamo = document.getElementById('ramo');
        selectRamo.innerHTML = '<option value="">-- Seleccionar Ramo --</option>';

        ramosCache.forEach(ramo => {
            const option = document.createElement('option');
            option.value = ramo.id;
            option.textContent = `${ramo.nombre} (${ramo.codigo})`;
            selectRamo.appendChild(option);
        });

        console.log('[Reportes] Ramos cargados:', ramosCache.length);
    } catch (error) {
        console.error('[Reportes] Error al cargar ramos:', error);
        mostrarError('Error al cargar la lista de ramos');
    }
}

async function cargarAlumnosPorCurso(cursoId) {
    try {
        console.log('[Reportes] Solicitando alumnos del curso:', cursoId);
        const response = await fetch(`/api/reportes/cursos/${cursoId}/alumnos`);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('[Reportes] Error en la respuesta:', response.status, errorText);
            throw new Error('Error al cargar alumnos');
        }

        alumnosCache = await response.json();
        console.log('[Reportes] Respuesta del servidor:', alumnosCache);

        const selectAlumno = document.getElementById('alumno');
        selectAlumno.innerHTML = '<option value="">-- Seleccionar Estudiante --</option>';

        if (alumnosCache.length === 0) {
            console.warn('[Reportes] No se encontraron alumnos para el curso:', cursoId);
            const option = document.createElement('option');
            option.value = '';
            option.textContent = '(No hay estudiantes en este curso)';
            option.disabled = true;
            selectAlumno.appendChild(option);
        } else {
            alumnosCache.forEach(alumno => {
                const option = document.createElement('option');
                option.value = alumno.id;
                option.textContent = `${alumno.nombre} (${alumno.codigo})`;
                selectAlumno.appendChild(option);
            });
        }

        console.log('[Reportes] Alumnos cargados correctamente:', alumnosCache.length);
    } catch (error) {
        console.error('[Reportes] Error al cargar alumnos:', error);
        mostrarError('Error al cargar la lista de estudiantes');
    }
}

// ==========================================
// EVENT HANDLERS
// ==========================================
function onTipoReporteChange() {
    const tipo = document.getElementById('tipoReporte').value;

    console.log('[Reportes] Tipo de reporte seleccionado:', tipo);

    // Limpiar selects
    document.getElementById('curso').value = '';
    document.getElementById('ramo').innerHTML = '<option value="">-- Seleccionar Ramo --</option>';
    document.getElementById('alumno').innerHTML = '<option value="">-- Seleccionar Estudiante --</option>';

    // Ocultar todos los grupos
    document.getElementById('grupoCurso').style.display = 'none';
    document.getElementById('grupoRamo').style.display = 'none';
    document.getElementById('grupoAlumno').style.display = 'none';

    // Mostrar según tipo
    if (tipo === 'curso') {
        document.getElementById('grupoCurso').style.display = 'block';
    } else if (tipo === 'ramo') {
        document.getElementById('grupoCurso').style.display = 'block';
        document.getElementById('grupoRamo').style.display = 'block';
    } else if (tipo === 'alumno') {
        document.getElementById('grupoCurso').style.display = 'block';
        document.getElementById('grupoAlumno').style.display = 'block';
        console.log('[Reportes] Mostrando selectores de Curso y Alumno');
    }
}

function onCursoChange() {
    const cursoId = document.getElementById('curso').value;
    const tipoReporte = document.getElementById('tipoReporte').value;

    console.log('[Reportes] Curso seleccionado:', cursoId, 'Tipo:', tipoReporte);

    if (!cursoId) return;

    if (tipoReporte === 'ramo') {
        cargarRamosPorCurso(cursoId);
    } else if (tipoReporte === 'alumno') {
        console.log('[Reportes] Cargando alumnos del curso:', cursoId);
        cargarAlumnosPorCurso(cursoId);
    }
}

// ==========================================
// GENERAR REPORTE
// ==========================================
async function generarReporte() {
    const tipo = document.getElementById('tipoReporte').value;
    const fechaInicio = document.getElementById('fechaInicio').value;
    const fechaFin = document.getElementById('fechaFin').value;

    // Validaciones
    if (!tipo) {
        mostrarError('Selecciona un tipo de reporte');
        return;
    }

    if (!fechaInicio || !fechaFin) {
        mostrarError('Selecciona las fechas de inicio y fin');
        return;
    }

    if (new Date(fechaInicio) > new Date(fechaFin)) {
        mostrarError('La fecha de inicio debe ser anterior a la fecha de fin');
        return;
    }

    // Mostrar loading
    mostrarLoading();

    try {
        if (tipo === 'curso') {
            await generarReporteCurso(fechaInicio, fechaFin);
        } else if (tipo === 'ramo') {
            await generarReporteRamo(fechaInicio, fechaFin);
        } else if (tipo === 'alumno') {
            await generarReporteAlumno(fechaInicio, fechaFin);
        }

        // Mostrar botón de exportar
        document.getElementById('btnExportarCSV').style.display = 'inline-flex';

        // Reinicializar iconos Lucide
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    } catch (error) {
        console.error('[Reportes] Error al generar reporte:', error);
        mostrarError('Error al generar el reporte: ' + error.message);
    }
}

async function generarReporteCurso(fechaInicio, fechaFin) {
    const cursoId = document.getElementById('curso').value;

    if (!cursoId) {
        throw new Error('Selecciona un curso');
    }

    const response = await fetch(`/api/reportes/curso?cursoId=${cursoId}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`);
    if (!response.ok) throw new Error('Error al generar reporte de curso');

    reporteActual = await response.json();
    renderReporteCurso(reporteActual);
}

async function generarReporteRamo(fechaInicio, fechaFin) {
    const ramoId = document.getElementById('ramo').value;

    if (!ramoId) {
        throw new Error('Selecciona un ramo');
    }

    const response = await fetch(`/api/reportes/ramo?ramoId=${ramoId}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`);
    if (!response.ok) throw new Error('Error al generar reporte de ramo');

    reporteActual = await response.json();
    renderReporteRamo(reporteActual);
}

async function generarReporteAlumno(fechaInicio, fechaFin) {
    const alumnoId = document.getElementById('alumno').value;

    if (!alumnoId) {
        throw new Error('Selecciona un estudiante');
    }

    const response = await fetch(`/api/reportes/alumno?alumnoId=${alumnoId}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`);
    if (!response.ok) throw new Error('Error al generar reporte de alumno');

    reporteActual = await response.json();
    renderReporteAlumno(reporteActual);
}

// ==========================================
// VARIABLES GLOBALES PARA GRÁFICAS
// ==========================================
let chartInstances = [];

// ==========================================
// RENDERIZADO - REPORTE POR CURSO
// ==========================================
function renderReporteCurso(reporte) {
    // Destruir gráficas anteriores
    destroyCharts();

    const html = `
        <div>
            <h2 style="color: var(--text-primary); margin-bottom: var(--space-4); display: flex; align-items: center; gap: var(--space-2);">
                <i data-lucide="graduation-cap"></i>
                ${reporte.cursoNombre} (${reporte.cursoCodigo})
            </h2>
            <p style="color: var(--text-secondary); margin-bottom: var(--space-6);">
                Periodo: ${formatDate(reporte.fechaInicio)} - ${formatDate(reporte.fechaFin)}
            </p>

            <!-- Estadísticas Generales -->
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="users"></i>
                        Total Estudiantes
                    </div>
                    <div class="stat-card-value">${reporte.totalEstudiantes}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="calendar-days"></i>
                        Total Clases
                    </div>
                    <div class="stat-card-value">${reporte.totalClasesEnPeriodo}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="percent"></i>
                        Promedio Asistencia
                    </div>
                    <div class="stat-card-value">${reporte.promedioAsistenciaGeneral.toFixed(1)}%</div>
                </div>
            </div>

            <!-- Gráficas -->
            <div class="charts-container">
                <div class="chart-card">
                    <h4><i data-lucide="pie-chart"></i> Distribución de Asistencia</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartAsistenciaCurso"></canvas>
                    </div>
                </div>
                <div class="chart-card">
                    <h4><i data-lucide="bar-chart"></i> Asistencia por Materia</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartAsistenciaRamos"></canvas>
                    </div>
                </div>
            </div>

            <!-- Desglose por Ramo -->
            <h3 style="color: var(--text-primary); margin: var(--space-6) 0 var(--space-4) 0; display: flex; align-items: center; gap: var(--space-2);">
                <i data-lucide="book-open"></i>
                Desglose por Materia/Ramo
            </h3>
            <div class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Código</th>
                            <th>Nombre</th>
                            <th>Total Clases</th>
                            <th>Presentes</th>
                            <th>Tardanzas</th>
                            <th>Ausentes</th>
                            <th>% Asistencia</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${reporte.ramos.map(ramo => `
                            <tr>
                                <td><strong>${ramo.ramoCodigo}</strong></td>
                                <td>${ramo.ramoNombre}</td>
                                <td>${ramo.totalClases}</td>
                                <td><span class="badge badge-success">${ramo.totalPresentes}</span></td>
                                <td><span class="badge badge-warning">${ramo.totalTardanzas}</span></td>
                                <td><span class="badge badge-danger">${ramo.totalAusentes}</span></td>
                                <td><strong>${ramo.promedioAsistencia.toFixed(1)}%</strong></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>

            <!-- Top Estudiantes -->
            ${reporte.topEstudiantes.length > 0 ? `
                <h3 style="color: var(--text-primary); margin: var(--space-6) 0 var(--space-4) 0; display: flex; align-items: center; gap: var(--space-2);">
                    <i data-lucide="trophy"></i>
                    Top Estudiantes
                </h3>
                <div class="table-container">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Posición</th>
                                <th>Código</th>
                                <th>Nombre</th>
                                <th>Presentes</th>
                                <th>Ausentes</th>
                                <th>% Asistencia</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${reporte.topEstudiantes.map((est, index) => `
                                <tr>
                                    <td><strong>${index + 1}</strong></td>
                                    <td>${est.alumnoCodigo}</td>
                                    <td>${est.alumnoNombre}</td>
                                    <td><span class="badge badge-success">${est.totalPresentes}</span></td>
                                    <td><span class="badge badge-danger">${est.totalAusentes}</span></td>
                                    <td><strong>${est.porcentajeAsistencia.toFixed(1)}%</strong></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            ` : ''}

            <!-- Estudiantes en Riesgo -->
            ${reporte.estudiantesEnRiesgo.length > 0 ? `
                <h3 style="color: var(--danger); margin: var(--space-6) 0 var(--space-4) 0; display: flex; align-items: center; gap: var(--space-2);">
                    <i data-lucide="alert-triangle"></i>
                    Estudiantes en Riesgo (< 75%)
                </h3>
                <div class="table-container">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Código</th>
                                <th>Nombre</th>
                                <th>Presentes</th>
                                <th>Ausentes</th>
                                <th>% Asistencia</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${reporte.estudiantesEnRiesgo.map(est => `
                                <tr>
                                    <td>${est.alumnoCodigo}</td>
                                    <td>${est.alumnoNombre}</td>
                                    <td><span class="badge badge-success">${est.totalPresentes}</span></td>
                                    <td><span class="badge badge-danger">${est.totalAusentes}</span></td>
                                    <td><strong style="color: var(--danger);">${est.porcentajeAsistencia.toFixed(1)}%</strong></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            ` : ''}
        </div>
    `;

    document.getElementById('resultados').innerHTML = html;

    // Reinicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Renderizar gráficas
    setTimeout(() => {
        createChartAsistenciaCurso(reporte);
        createChartAsistenciaRamos(reporte);
    }, 100);
}

// ==========================================
// RENDERIZADO - REPORTE POR RAMO
// ==========================================
function renderReporteRamo(reporte) {
    // Destruir gráficas anteriores
    destroyCharts();

    const html = `
        <div>
            <h2 style="color: var(--text-primary); margin-bottom: var(--space-4); display: flex; align-items: center; gap: var(--space-2);">
                <i data-lucide="book-open"></i>
                ${reporte.ramoNombre} (${reporte.ramoCodigo})
            </h2>
            <p style="color: var(--text-secondary); margin-bottom: var(--space-2);">
                Curso: ${reporte.cursoNombre}
            </p>
            <p style="color: var(--text-secondary); margin-bottom: var(--space-6);">
                Periodo: ${formatDate(reporte.fechaInicio)} - ${formatDate(reporte.fechaFin)}
            </p>

            <!-- Estadísticas -->
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="calendar"></i>
                        Total Clases
                    </div>
                    <div class="stat-card-value">${reporte.totalClases}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="user"></i>
                        Total Estudiantes
                    </div>
                    <div class="stat-card-value">${reporte.totalEstudiantes}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="percent"></i>
                        Promedio Asistencia
                    </div>
                    <div class="stat-card-value">${reporte.promedioAsistencia.toFixed(1)}%</div>
                </div>
            </div>

            <!-- Gráficas -->
            <div class="charts-container">
                <div class="chart-card">
                    <h4><i data-lucide="users"></i> Top 10 Estudiantes</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartTopEstudiantes"></canvas>
                    </div>
                </div>
                <div class="chart-card">
                    <h4><i data-lucide="pie-chart"></i> Situación de Estudiantes</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartSituacionEstudiantes"></canvas>
                    </div>
                </div>
            </div>

            <!-- Asistencia por Estudiante -->
            <h3 style="color: var(--text-primary); margin: var(--space-6) 0 var(--space-4) 0; display: flex; align-items: center; gap: var(--space-2);">
                <i data-lucide="list"></i>
                Asistencia por Estudiante
            </h3>
            <div class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Código</th>
                            <th>Nombre</th>
                            <th>Total</th>
                            <th>Presentes</th>
                            <th>Tardanzas</th>
                            <th>Ausentes</th>
                            <th>% Asistencia</th>
                            <th>Situación</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${reporte.asistenciasPorAlumno.map(alumno => `
                            <tr>
                                <td>${alumno.alumnoCodigo}</td>
                                <td>${alumno.alumnoNombre}</td>
                                <td>${alumno.totalClases}</td>
                                <td><span class="badge badge-success">${alumno.totalPresentes}</span></td>
                                <td><span class="badge badge-warning">${alumno.totalTardanzas}</span></td>
                                <td><span class="badge badge-danger">${alumno.totalAusentes}</span></td>
                                <td><strong>${alumno.porcentajeAsistencia.toFixed(1)}%</strong></td>
                                <td>${getBadgeSituacion(alumno.situacion)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </div>
    `;

    document.getElementById('resultados').innerHTML = html;

    // Reinicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Renderizar gráficas
    setTimeout(() => {
        createChartTopEstudiantes(reporte);
        createChartSituacionEstudiantes(reporte);
    }, 100);
}

// ==========================================
// RENDERIZADO - REPORTE POR ALUMNO
// ==========================================
function renderReporteAlumno(reporte) {
    // Destruir gráficas anteriores
    destroyCharts();

    const html = `
        <div>
            <h2 style="color: var(--text-primary); margin-bottom: var(--space-4); display: flex; align-items: center; gap: var(--space-2);">
                <i data-lucide="user-circle"></i>
                ${reporte.alumnoNombre} (${reporte.alumnoCodigo})
            </h2>
            <p style="color: var(--text-secondary); margin-bottom: var(--space-6);">
                Periodo: ${formatDate(reporte.fechaInicio)} - ${formatDate(reporte.fechaFin)}
            </p>

            <!-- Estadísticas Globales -->
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="clipboard-list"></i>
                        Total Clases
                    </div>
                    <div class="stat-card-value">${reporte.totalClasesEnPeriodo}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="check-circle"></i>
                        Presentes
                    </div>
                    <div class="stat-card-value" style="color: var(--success);">${reporte.totalPresentes}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="clock"></i>
                        Tardanzas
                    </div>
                    <div class="stat-card-value" style="color: var(--warning);">${reporte.totalTardanzas}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="x-circle"></i>
                        Ausentes
                    </div>
                    <div class="stat-card-value" style="color: var(--danger);">${reporte.totalAusentes}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-card-header">
                        <i data-lucide="percent"></i>
                        % Asistencia Global
                    </div>
                    <div class="stat-card-value">${reporte.porcentajeAsistenciaGlobal.toFixed(1)}%</div>
                </div>
            </div>

            <!-- Gráficas -->
            <div class="charts-container">
                <div class="chart-card">
                    <h4><i data-lucide="pie-chart"></i> Distribución de Asistencia</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartAsistenciaAlumno"></canvas>
                    </div>
                </div>
                <div class="chart-card">
                    <h4><i data-lucide="bar-chart"></i> Asistencia por Materia</h4>
                    <div class="chart-wrapper">
                        <canvas id="chartAsistenciaRamosAlumno"></canvas>
                    </div>
                </div>
            </div>

            <!-- Desglose por Ramo -->
            ${reporte.porRamo.length > 0 ? `
                <h3 style="color: var(--text-primary); margin: var(--space-6) 0 var(--space-4) 0; display: flex; align-items: center; gap: var(--space-2);">
                    <i data-lucide="book"></i>
                    Desglose por Materia
                </h3>
                <div class="table-container">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Código</th>
                                <th>Materia/Ramo</th>
                                <th>Curso</th>
                                <th>Total Clases</th>
                                <th>Presentes</th>
                                <th>Tardanzas</th>
                                <th>Ausentes</th>
                                <th>% Asistencia</th>
                                <th>Situación</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${reporte.porRamo.map(ramo => `
                                <tr>
                                    <td><strong>${ramo.ramoCodigo || 'N/A'}</strong></td>
                                    <td>${ramo.ramoNombre || 'Sin nombre'}</td>
                                    <td>${ramo.cursoNombre || 'N/A'}</td>
                                    <td>${ramo.totalClases}</td>
                                    <td><span class="badge badge-success">${ramo.totalPresentes}</span></td>
                                    <td><span class="badge badge-warning">${ramo.totalTardanzas}</span></td>
                                    <td><span class="badge badge-danger">${ramo.totalAusentes}</span></td>
                                    <td><strong>${ramo.porcentajeAsistencia.toFixed(1)}%</strong></td>
                                    <td>${getBadgeSituacion(ramo.situacion)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            ` : '<p style="color: var(--text-muted); text-align: center; padding: var(--space-6);">No hay datos por ramo en este periodo</p>'}
        </div>
    `;

    document.getElementById('resultados').innerHTML = html;

    // Reinicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Renderizar gráficas
    setTimeout(() => {
        createChartAsistenciaAlumno(reporte);
        createChartAsistenciaRamosAlumno(reporte);
    }, 100);
}

// ==========================================
// EXPORTAR CSV
// ==========================================
function exportarCSV() {
    const tipo = document.getElementById('tipoReporte').value;
    const fechaInicio = document.getElementById('fechaInicio').value;
    const fechaFin = document.getElementById('fechaFin').value;

    let url = '';

    if (tipo === 'curso') {
        const cursoId = document.getElementById('curso').value;
        url = `/api/reportes/curso/export-csv?cursoId=${cursoId}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`;
    } else if (tipo === 'ramo') {
        const ramoId = document.getElementById('ramo').value;
        url = `/api/reportes/ramo/export-csv?ramoId=${ramoId}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`;
    } else {
        mostrarError('Exportación no disponible para este tipo de reporte');
        return;
    }

    // Descargar archivo
    window.location.href = url;
}

// ==========================================
// UTILIDADES
// ==========================================
function limpiarFiltros() {
    document.getElementById('tipoReporte').value = '';
    document.getElementById('curso').value = '';
    document.getElementById('ramo').value = '';
    document.getElementById('alumno').value = '';
    configurarFechasPorDefecto();

    document.getElementById('grupoCurso').style.display = 'none';
    document.getElementById('grupoRamo').style.display = 'none';
    document.getElementById('grupoAlumno').style.display = 'none';
    document.getElementById('btnExportarCSV').style.display = 'none';

    document.getElementById('resultados').innerHTML = `
        <div class="empty-state">
            <i data-lucide="pie-chart"></i>
            <h3>Selecciona los filtros y genera un reporte</h3>
            <p>Elige el tipo de reporte, las fechas y los parámetros necesarios</p>
        </div>
    `;

    // Reinicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

function mostrarLoading() {
    document.getElementById('resultados').innerHTML = `
        <div class="loading">
            <i data-lucide="loader"></i>
            <p>Generando reporte...</p>
        </div>
    `;

    // Reinicializar iconos Lucide
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

function mostrarError(mensaje) {
    alert(mensaje);
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES', { year: 'numeric', month: 'long', day: 'numeric' });
}

function getBadgeSituacion(situacion) {
    const badges = {
        'Excelente': '<span class="badge badge-success">Excelente</span>',
        'Bueno': '<span class="badge badge-info">Bueno</span>',
        'Regular': '<span class="badge badge-warning">Regular</span>',
        'En Riesgo': '<span class="badge badge-danger">En Riesgo</span>',
        'Crítico': '<span class="badge badge-danger">Crítico</span>'
    };
    return badges[situacion] || situacion;
}

// ==========================================
// GRÁFICAS - CHART.JS
// ==========================================
function destroyCharts() {
    chartInstances.forEach(chart => chart.destroy());
    chartInstances = [];
}

function getChartColors() {
    const style = getComputedStyle(document.documentElement);
    return {
        success: style.getPropertyValue('--success').trim() || '#10b981',
        warning: style.getPropertyValue('--warning').trim() || '#f59e0b',
        danger: style.getPropertyValue('--danger').trim() || '#ef4444',
        info: style.getPropertyValue('--info').trim() || '#3b82f6',
        primary: style.getPropertyValue('--primary').trim() || '#8b5cf6',
        textPrimary: style.getPropertyValue('--text-primary').trim() || '#111827',
        textSecondary: style.getPropertyValue('--text-secondary').trim() || '#6b7280'
    };
}

function createChartAsistenciaCurso(reporte) {
    const ctx = document.getElementById('chartAsistenciaCurso');
    if (!ctx) return;

    const colors = getChartColors();
    const totalPresentes = reporte.ramos.reduce((sum, r) => sum + r.totalPresentes, 0);
    const totalTardanzas = reporte.ramos.reduce((sum, r) => sum + r.totalTardanzas, 0);
    const totalAusentes = reporte.ramos.reduce((sum, r) => sum + r.totalAusentes, 0);

    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Presentes', 'Tardanzas', 'Ausentes'],
            datasets: [{
                data: [totalPresentes, totalTardanzas, totalAusentes],
                backgroundColor: [colors.success, colors.warning, colors.danger],
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { color: colors.textPrimary, padding: 15, font: { size: 12 } }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const value = context.parsed;
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${context.label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });

    chartInstances.push(chart);
}

function createChartAsistenciaRamos(reporte) {
    const ctx = document.getElementById('chartAsistenciaRamos');
    if (!ctx) return;

    const colors = getChartColors();
    const labels = reporte.ramos.map(r => r.ramoNombre.length > 15 ? r.ramoNombre.substring(0, 15) + '...' : r.ramoNombre);
    const data = reporte.ramos.map(r => r.promedioAsistencia);

    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: '% Asistencia',
                data: data,
                backgroundColor: colors.primary,
                borderColor: colors.primary,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `Asistencia: ${context.parsed.y.toFixed(1)}%`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: { color: colors.textSecondary, callback: value => value + '%' },
                    grid: { color: 'rgba(0,0,0,0.05)' }
                },
                x: {
                    ticks: { color: colors.textSecondary },
                    grid: { display: false }
                }
            }
        }
    });

    chartInstances.push(chart);
}
