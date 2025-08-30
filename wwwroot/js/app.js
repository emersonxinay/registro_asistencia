// Utilidades generales
class Utils {
    static async request(url, options = {}) {
        try {
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });
            
            const contentType = response.headers.get('content-type');
            let data;
            
            if (contentType?.includes('application/json')) {
                data = await response.json();
            } else {
                data = await response.text();
            }
            
            if (!response.ok) {
                throw new Error(typeof data === 'string' ? data : data.message || 'Error en la solicitud');
            }
            
            return data;
        } catch (error) {
            console.error('Error en petición:', error);
            throw error;
        }
    }

    static showStatus(element, message, type = 'success') {
        element.innerHTML = `<div class="status status-${type}">${message}</div>`;
    }

    static formatDate(dateString) {
        return new Date(dateString).toLocaleString('es-ES', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    static showLoading(element, text = 'Cargando...') {
        element.innerHTML = `<div class="loading"><span class="spinner"></span>${text}</div>`;
    }
}

// Gestión de alumnos
class AlumnosManager {
    constructor() {
        this.bindEvents();
        this.loadAlumnos();
    }

    bindEvents() {
        const form = document.getElementById('alumnoForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleSubmit(e));
        }
    }

    async handleSubmit(e) {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = {
            codigo: formData.get('codigo'),
            nombre: formData.get('nombre')
        };

        try {
            const result = await Utils.request('/api/alumnos', {
                method: 'POST',
                body: JSON.stringify(data)
            });

            Utils.showStatus(
                document.getElementById('alumnoStatus'),
                `Alumno "${result.nombre}" creado exitosamente`,
                'success'
            );

            e.target.reset();
            this.loadAlumnos();
        } catch (error) {
            Utils.showStatus(
                document.getElementById('alumnoStatus'),
                `Error: ${error.message}`,
                'error'
            );
        }
    }

    async loadAlumnos() {
        const container = document.getElementById('alumnosList');
        if (!container) return;

        Utils.showLoading(container);

        try {
            const alumnos = await Utils.request('/api/alumnos');
            this.renderAlumnos(container, alumnos);
        } catch (error) {
            container.innerHTML = `<div class="status status-error">Error al cargar alumnos: ${error.message}</div>`;
        }
    }

    renderAlumnos(container, alumnos) {
        if (!alumnos.length) {
            container.innerHTML = '<div class="text-center text-sm">No hay alumnos registrados</div>';
            return;
        }

        container.innerHTML = `
            <div class="table-container">
                <table class="table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Código</th>
                            <th>Nombre</th>
                            <th>Acciones</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${alumnos.map(alumno => `
                            <tr>
                                <td>${alumno.id}</td>
                                <td>${alumno.codigo}</td>
                                <td>${alumno.nombre}</td>
                                <td>
                                    <button class="btn btn-sm btn-secondary" onclick="window.alumnosManager.showQr(${alumno.id})">
                                        Ver QR
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    async showQr(alumnoId) {
        try {
            const alumno = await Utils.request(`/api/alumnos/${alumnoId}`);
            const modal = this.createQrModal(alumno);
            document.body.appendChild(modal);
        } catch (error) {
            alert(`Error al obtener QR: ${error.message}`);
        }
    }

    createQrModal(alumno) {
        const modal = document.createElement('div');
        modal.className = 'modal-overlay';
        modal.innerHTML = `
            <div class="modal-content">
                <div class="modal-header">
                    <h3>QR de ${alumno.nombre}</h3>
                    <button class="modal-close">&times;</button>
                </div>
                <div class="modal-body text-center">
                    <img src="data:image/png;base64,${alumno.qrAlumnoBase64}" alt="QR Alumno" class="qr-image">
                    <p class="text-sm text-secondary">Código: ${alumno.codigo}</p>
                </div>
            </div>
        `;

        modal.querySelector('.modal-close').onclick = () => modal.remove();
        modal.onclick = (e) => e.target === modal && modal.remove();

        return modal;
    }
}

// Gestión de clases
class ClasesManager {
    constructor() {
        this.bindEvents();
        this.loadClases();
    }

    bindEvents() {
        const form = document.getElementById('claseForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleSubmit(e));
        }
    }

    async handleSubmit(e) {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = {
            asignatura: formData.get('asignatura')
        };

        try {
            const result = await Utils.request('/api/clases', {
                method: 'POST',
                body: JSON.stringify(data)
            });

            Utils.showStatus(
                document.getElementById('claseStatus'),
                `Clase "${result.asignatura}" creada exitosamente`,
                'success'
            );

            e.target.reset();
            this.loadClases();
        } catch (error) {
            Utils.showStatus(
                document.getElementById('claseStatus'),
                `Error: ${error.message}`,
                'error'
            );
        }
    }

    async loadClases() {
        const container = document.getElementById('clasesList');
        if (!container) return;

        Utils.showLoading(container);

        try {
            const clases = await Utils.request('/api/clases');
            this.renderClases(container, clases);
        } catch (error) {
            container.innerHTML = `<div class="status status-error">Error al cargar clases: ${error.message}</div>`;
        }
    }

    renderClases(container, clases) {
        if (!clases.length) {
            container.innerHTML = '<div class="text-center text-sm">No hay clases registradas</div>';
            return;
        }

        container.innerHTML = `
            <div class="table-container">
                <table class="table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Asignatura</th>
                            <th>Inicio</th>
                            <th>Estado</th>
                            <th>Acciones</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${clases.map(clase => `
                            <tr>
                                <td>${clase.id}</td>
                                <td>${clase.asignatura}</td>
                                <td>${Utils.formatDate(clase.inicioUtc)}</td>
                                <td>
                                    <span class="status ${clase.activa ? 'status-success' : 'status-warning'}">
                                        ${clase.activa ? 'Activa' : 'Cerrada'}
                                    </span>
                                </td>
                                <td>
                                    <div class="flex gap-2">
                                        ${clase.activa ? `
                                            <button class="btn btn-sm btn-primary" onclick="window.qrManager.showQr(${clase.id})">
                                                QR Clase
                                            </button>
                                            <button class="btn btn-sm btn-warning" onclick="window.clasesManager.cerrarClase(${clase.id})">
                                                Cerrar
                                            </button>
                                        ` : ''}
                                        <a href="/asistencias/${clase.id}" class="btn btn-sm btn-secondary">
                                            Ver Asistencias
                                        </a>
                                    </div>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    async cerrarClase(claseId) {
        if (!confirm('¿Está seguro de cerrar esta clase?')) return;

        try {
            await Utils.request(`/api/clases/${claseId}/cerrar`, {
                method: 'POST'
            });
            
            this.loadClases();
        } catch (error) {
            alert(`Error al cerrar clase: ${error.message}`);
        }
    }
}

// Gestión de QR de clases
class QRManager {
    constructor() {
        this.currentClassId = null;
        this.refreshInterval = null;
    }

    async showQr(claseId) {
        this.currentClassId = claseId;
        const container = document.getElementById('qrContainer');
        if (!container) {
            // Crear modal si no existe el contenedor
            this.createQrModal(claseId);
            return;
        }

        await this.loadQr();
        this.startAutoRefresh();
    }

    async loadQr() {
        if (!this.currentClassId) return;

        const container = document.getElementById('qrContainer') || document.querySelector('.qr-display');
        
        try {
            const result = await Utils.request(`/api/clases/${this.currentClassId}/qr`);
            
            container.innerHTML = `
                <img src="data:image/png;base64,${result.base64Png}" alt="QR Clase" class="qr-image">
                <div class="qr-info">
                    <div><strong>Expira:</strong> ${Utils.formatDate(result.expiraUtc)}</div>
                    <div class="text-xs mt-2">${result.url}</div>
                </div>
            `;
        } catch (error) {
            container.innerHTML = `<div class="status status-error">Error al cargar QR: ${error.message}</div>`;
        }
    }

    startAutoRefresh() {
        this.stopAutoRefresh();
        this.refreshInterval = setInterval(() => {
            this.loadQr();
        }, 60000); // Refrescar cada 60 segundos
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    createQrModal(claseId) {
        this.currentClassId = claseId;
        const modal = document.createElement('div');
        modal.className = 'modal-overlay';
        modal.innerHTML = `
            <div class="modal-content">
                <div class="modal-header">
                    <h3>QR de Clase ${claseId}</h3>
                    <button class="modal-close">&times;</button>
                </div>
                <div class="modal-body">
                    <div class="qr-display" id="modalQrContainer">
                        <div class="loading"><span class="spinner"></span>Generando QR...</div>
                    </div>
                </div>
            </div>
        `;

        modal.querySelector('.modal-close').onclick = () => {
            this.stopAutoRefresh();
            modal.remove();
        };
        modal.onclick = (e) => {
            if (e.target === modal) {
                this.stopAutoRefresh();
                modal.remove();
            }
        };

        document.body.appendChild(modal);
        
        // Cargar QR
        setTimeout(() => {
            this.loadQr();
            this.startAutoRefresh();
        }, 100);
    }
}

// Gestión de asistencias
class AsistenciasManager {
    constructor() {
        this.loadAsistencias();
    }

    async loadAsistencias() {
        const container = document.getElementById('asistenciasList');
        if (!container) return;

        const claseId = this.getClaseIdFromUrl();
        const endpoint = claseId ? `/api/asistencias/clase/${claseId}` : '/api/asistencias';

        Utils.showLoading(container);

        try {
            const asistencias = await Utils.request(endpoint);
            this.renderAsistencias(container, asistencias, claseId);
        } catch (error) {
            container.innerHTML = `<div class="status status-error">Error al cargar asistencias: ${error.message}</div>`;
        }
    }

    getClaseIdFromUrl() {
        const path = window.location.pathname;
        const matches = path.match(/\/asistencias\/(\d+)/);
        return matches ? matches[1] : null;
    }
    
    async loadAsistenciasByClase(claseId) {
        const container = document.getElementById('asistenciasList');
        if (!container) return;

        const endpoint = `/api/asistencias/clase/${claseId}`;
        Utils.showLoading(container);

        try {
            const asistencias = await Utils.request(endpoint);
            this.renderAsistencias(container, asistencias, claseId);
        } catch (error) {
            container.innerHTML = `<div class="status status-error">Error al cargar asistencias: ${error.message}</div>`;
        }
    }

    renderAsistencias(container, asistencias, claseId) {
        if (!asistencias.length) {
            container.innerHTML = '<div class="text-center text-sm">No hay asistencias registradas</div>';
            return;
        }

        const csvUrl = claseId ? `/api/asistencias/clase/${claseId}/csv` : '/api/asistencias/csv';

        container.innerHTML = `
            <div class="flex justify-between items-center mb-4">
                <h3>Asistencias ${claseId ? `- Clase ${claseId}` : ''}</h3>
                <a href="${csvUrl}" class="btn btn-sm btn-success">Descargar CSV</a>
            </div>
            <div class="table-container">
                <table class="table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Clase</th>
                            <th>Asignatura</th>
                            <th>Alumno</th>
                            <th>Código</th>
                            <th>Nombre</th>
                            <th>Método</th>
                            <th>Fecha</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${asistencias.map(asistencia => `
                            <tr>
                                <td>${asistencia.id}</td>
                                <td>${asistencia.claseId}</td>
                                <td>${asistencia.asignatura || '-'}</td>
                                <td>${asistencia.alumnoId}</td>
                                <td>${asistencia.codigo || '-'}</td>
                                <td>${asistencia.nombre || '-'}</td>
                                <td>
                                    <span class="status ${asistencia.metodo === 'PROFESOR_ESCANEA' ? 'status-warning' : 'status-success'}">
                                        ${asistencia.metodo}
                                    </span>
                                </td>
                                <td>${Utils.formatDate(asistencia.marcadaUtc)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }
}

// Inicialización global
document.addEventListener('DOMContentLoaded', () => {
    // Instanciar gestores globalmente
    window.alumnosManager = new AlumnosManager();
    window.clasesManager = new ClasesManager();
    window.qrManager = new QRManager();
    window.asistenciasManager = new AsistenciasManager();
});

// Estilos adicionales para modales
const modalStyles = document.createElement('style');
modalStyles.textContent = `
    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
    }

    .modal-content {
        background: white;
        border-radius: 12px;
        max-width: 500px;
        width: 90%;
        max-height: 90vh;
        overflow-y: auto;
    }

    .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.5rem;
        border-bottom: 1px solid var(--border);
    }

    .modal-header h3 {
        margin: 0;
        font-size: 1.25rem;
        font-weight: 600;
    }

    .modal-close {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0.25rem;
        color: var(--text-secondary);
    }

    .modal-close:hover {
        color: var(--text-primary);
    }

    .modal-body {
        padding: 1.5rem;
    }
`;
document.head.appendChild(modalStyles);