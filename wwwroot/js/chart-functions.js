// Additional chart functions for reports

function createChartTopEstudiantes(reporte) {
    const ctx = document.getElementById('chartTopEstudiantes');
    if (!ctx) return;

    const colors = getChartColors();

    // Ordenar estudiantes por porcentaje y tomar top 10
    const topEstudiantes = [...reporte.asistenciasPorAlumno]
        .sort((a, b) => b.porcentajeAsistencia - a.porcentajeAsistencia)
        .slice(0, 10);

    const labels = topEstudiantes.map(e => e.alumnoNombre.length > 20 ? e.alumnoNombre.substring(0, 20) + '...' : e.alumnoNombre);
    const data = topEstudiantes.map(e => e.porcentajeAsistencia);

    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: '% Asistencia',
                data: data,
                backgroundColor: colors.success,
                borderColor: colors.success,
                borderWidth: 1
            }]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return 'Asistencia: ' + context.parsed.x.toFixed(1) + '%';
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    max: 100,
                    ticks: { color: colors.textSecondary, callback: value => value + '%' },
                    grid: { color: 'rgba(0,0,0,0.05)' }
                },
                y: {
                    ticks: { color: colors.textSecondary },
                    grid: { display: false }
                }
            }
        }
    });

    chartInstances.push(chart);
}

function createChartSituacionEstudiantes(reporte) {
    const ctx = document.getElementById('chartSituacionEstudiantes');
    if (!ctx) return;

    const colors = getChartColors();

    // Contar estudiantes por situación
    const situaciones = {
        'Excelente': 0,
        'Bueno': 0,
        'Regular': 0,
        'En Riesgo': 0,
        'Crítico': 0
    };

    reporte.asistenciasPorAlumno.forEach(alumno => {
        if (situaciones.hasOwnProperty(alumno.situacion)) {
            situaciones[alumno.situacion]++;
        }
    });

    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: Object.keys(situaciones),
            datasets: [{
                data: Object.values(situaciones),
                backgroundColor: [
                    colors.success,
                    colors.info,
                    colors.warning,
                    '#f97316',
                    colors.danger
                ],
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
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return context.label + ': ' + value + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });

    chartInstances.push(chart);
}

function createChartAsistenciaAlumno(reporte) {
    const ctx = document.getElementById('chartAsistenciaAlumno');
    if (!ctx) return;

    const colors = getChartColors();

    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Presentes', 'Tardanzas', 'Ausentes'],
            datasets: [{
                data: [reporte.totalPresentes, reporte.totalTardanzas, reporte.totalAusentes],
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
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return context.label + ': ' + value + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });

    chartInstances.push(chart);
}

function createChartAsistenciaRamosAlumno(reporte) {
    const ctx = document.getElementById('chartAsistenciaRamosAlumno');
    if (!ctx) return;

    const colors = getChartColors();

    if (reporte.porRamo.length === 0) return;

    const labels = reporte.porRamo.map(r => r.ramoNombre && r.ramoNombre.length > 15 ? r.ramoNombre.substring(0, 15) + '...' : r.ramoNombre || 'Sin nombre');
    const data = reporte.porRamo.map(r => r.porcentajeAsistencia);

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
                            return 'Asistencia: ' + context.parsed.y.toFixed(1) + '%';
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
