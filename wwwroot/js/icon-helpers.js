/**
 * =======================================
 * ICON HELPERS - Font Awesome Icons
 * Reemplazo de emojis por iconos escalables
 * =======================================
 */

const IconHelpers = {
    // Iconos de estado de asistencia
    getEstadoIcon(estado) {
        const icons = {
            'Presente': '<i class="fas fa-check-circle icon-presente"></i>',
            'Tardanza': '<i class="fas fa-clock icon-tardanza"></i>',
            'Ausente': '<i class="fas fa-times-circle icon-ausente"></i>',
            'Justificado': '<i class="fas fa-file-medical icon-justificado"></i>'
        };
        return icons[estado] || '<i class="fas fa-check-circle icon-success"></i>';
    },

    // Iconos de tipo de mensaje
    getMessageIcon(type) {
        const icons = {
            'success': '<i class="fas fa-check-circle icon-success"></i>',
            'error': '<i class="fas fa-times-circle icon-error"></i>',
            'warning': '<i class="fas fa-exclamation-triangle icon-warning"></i>',
            'info': '<i class="fas fa-info-circle icon-info"></i>',
            'loading': '<i class="fas fa-spinner fa-spin"></i>'
        };
        return icons[type] || icons.info;
    },

    // Iconos comunes
    icons: {
        check: '<i class="fas fa-check"></i>',
        times: '<i class="fas fa-times"></i>',
        plus: '<i class="fas fa-plus"></i>',
        minus: '<i class="fas fa-minus"></i>',
        edit: '<i class="fas fa-edit"></i>',
        trash: '<i class="fas fa-trash"></i>',
        save: '<i class="fas fa-save"></i>',
        cancel: '<i class="fas fa-ban"></i>',
        search: '<i class="fas fa-search"></i>',
        filter: '<i class="fas fa-filter"></i>',
        download: '<i class="fas fa-download"></i>',
        upload: '<i class="fas fa-upload"></i>',
        print: '<i class="fas fa-print"></i>',
        qrcode: '<i class="fas fa-qrcode"></i>',
        barcode: '<i class="fas fa-barcode"></i>',
        user: '<i class="fas fa-user"></i>',
        users: '<i class="fas fa-users"></i>',
        calendar: '<i class="fas fa-calendar"></i>',
        clock: '<i class="fas fa-clock"></i>',
        bell: '<i class="fas fa-bell"></i>',
        envelope: '<i class="fas fa-envelope"></i>',
        phone: '<i class="fas fa-phone"></i>',
        home: '<i class="fas fa-home"></i>',
        settings: '<i class="fas fa-cog"></i>',
        logout: '<i class="fas fa-sign-out-alt"></i>',
        login: '<i class="fas fa-sign-in-alt"></i>',
        dashboard: '<i class="fas fa-tachometer-alt"></i>',
        chart: '<i class="fas fa-chart-line"></i>',
        table: '<i class="fas fa-table"></i>',
        list: '<i class="fas fa-list"></i>',
        grid: '<i class="fas fa-th"></i>',
        camera: '<i class="fas fa-camera"></i>',
        image: '<i class="fas fa-image"></i>',
        file: '<i class="fas fa-file"></i>',
        folder: '<i class="fas fa-folder"></i>',
        link: '<i class="fas fa-link"></i>',
        lock: '<i class="fas fa-lock"></i>',
        unlock: '<i class="fas fa-unlock"></i>',
        key: '<i class="fas fa-key"></i>',
        shield: '<i class="fas fa-shield-alt"></i>',
        eye: '<i class="fas fa-eye"></i>',
        eyeSlash: '<i class="fas fa-eye-slash"></i>',
        heart: '<i class="fas fa-heart"></i>',
        star: '<i class="fas fa-star"></i>',
        bookmark: '<i class="fas fa-bookmark"></i>',
        flag: '<i class="fas fa-flag"></i>',
        tag: '<i class="fas fa-tag"></i>',
        comment: '<i class="fas fa-comment"></i>',
        share: '<i class="fas fa-share"></i>',
        refresh: '<i class="fas fa-sync"></i>',
        undo: '<i class="fas fa-undo"></i>',
        redo: '<i class="fas fa-redo"></i>',
        copy: '<i class="fas fa-copy"></i>',
        paste: '<i class="fas fa-paste"></i>',
        cut: '<i class="fas fa-cut"></i>',
        paperclip: '<i class="fas fa-paperclip"></i>',
        expand: '<i class="fas fa-expand"></i>',
        compress: '<i class="fas fa-compress"></i>',
        arrowUp: '<i class="fas fa-arrow-up"></i>',
        arrowDown: '<i class="fas fa-arrow-down"></i>',
        arrowLeft: '<i class="fas fa-arrow-left"></i>',
        arrowRight: '<i class="fas fa-arrow-right"></i>',
        chevronUp: '<i class="fas fa-chevron-up"></i>',
        chevronDown: '<i class="fas fa-chevron-down"></i>',
        chevronLeft: '<i class="fas fa-chevron-left"></i>',
        chevronRight: '<i class="fas fa-chevron-right"></i>',
        angleUp: '<i class="fas fa-angle-up"></i>',
        angleDown: '<i class="fas fa-angle-down"></i>',
        angleLeft: '<i class="fas fa-angle-left"></i>',
        angleRight: '<i class="fas fa-angle-right"></i>',
        ellipsisV: '<i class="fas fa-ellipsis-v"></i>',
        ellipsisH: '<i class="fas fa-ellipsis-h"></i>',
        bars: '<i class="fas fa-bars"></i>',
        grip: '<i class="fas fa-grip-horizontal"></i>',
        spinner: '<i class="fas fa-spinner fa-spin"></i>',
        circle: '<i class="fas fa-circle"></i>',
        square: '<i class="fas fa-square"></i>',

        // Iconos específicos del sistema
        presente: '<i class="fas fa-check-circle icon-presente"></i>',
        tardanza: '<i class="fas fa-clock icon-tardanza"></i>',
        ausente: '<i class="fas fa-times-circle icon-ausente"></i>',
        justificado: '<i class="fas fa-file-medical icon-justificado"></i>',

        // Estados
        success: '<i class="fas fa-check-circle icon-success"></i>',
        error: '<i class="fas fa-times-circle icon-error"></i>',
        warning: '<i class="fas fa-exclamation-triangle icon-warning"></i>',
        info: '<i class="fas fa-info-circle icon-info"></i>',

        // Utilidades
        lightbulb: '<i class="fas fa-lightbulb"></i>',
        ban: '<i class="fas fa-ban"></i>',
        exclamation: '<i class="fas fa-exclamation-circle"></i>',
        question: '<i class="fas fa-question-circle"></i>',
        hourglass: '<i class="fas fa-hourglass-half"></i>',
        hourglassEnd: '<i class="fas fa-hourglass-end"></i>',
        mobile: '<i class="fas fa-mobile-alt"></i>',
        desktop: '<i class="fas fa-desktop"></i>',
        laptop: '<i class="fas fa-laptop"></i>',
        tablet: '<i class="fas fa-tablet-alt"></i>',
        wifi: '<i class="fas fa-wifi"></i>',
        signal: '<i class="fas fa-signal"></i>',
        battery: '<i class="fas fa-battery-full"></i>',
        bolt: '<i class="fas fa-bolt"></i>',
        plug: '<i class="fas fa-plug"></i>',
        power: '<i class="fas fa-power-off"></i>',
        sun: '<i class="fas fa-sun"></i>',
        moon: '<i class="fas fa-moon"></i>',
        cloudSun: '<i class="fas fa-cloud-sun"></i>',
        cloudMoon: '<i class="fas fa-cloud-moon"></i>'
    },

    // Método para obtener cualquier icono
    get(name) {
        return this.icons[name] || '';
    },

    // Crear icono con clase personalizada
    create(iconClass, additionalClasses = '') {
        return `<i class="${iconClass} ${additionalClasses}"></i>`;
    },

    // Crear icono con tamaño
    sized(iconClass, size = 'md') {
        const sizeClass = `icon-${size}`;
        return `<i class="${iconClass} ${sizeClass}"></i>`;
    }
};

// Exponer globalmente
window.IconHelpers = IconHelpers;

// Alias más corto
window.Icon = IconHelpers;

console.log('✓ Icon Helpers cargados');
