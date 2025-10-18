/**
 * =======================================
 * THEME MANAGER - Sistema de Modo Oscuro Persistente
 * Compatible con todas las plataformas
 * =======================================
 */

class ThemeManager {
  constructor() {
    this.STORAGE_KEY = 'user-theme-preference';
    this.THEME_ATTRIBUTE = 'data-theme';
    this.currentTheme = null;

    // Inicializar el tema inmediatamente para evitar flash
    this.init();
  }

  /**
   * Inicializar el sistema de temas
   */
  init() {
    // Cargar tema guardado o detectar preferencia del sistema
    this.currentTheme = this.getStoredTheme() || this.getSystemTheme();

    // Aplicar tema inmediatamente (antes del DOMContentLoaded)
    this.applyTheme(this.currentTheme, false);

    // Configurar listeners cuando el DOM esté listo
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => this.setupListeners());
    } else {
      this.setupListeners();
    }
  }

  /**
   * Obtener tema guardado en localStorage
   */
  getStoredTheme() {
    try {
      return localStorage.getItem(this.STORAGE_KEY);
    } catch (e) {
      console.warn('No se pudo acceder a localStorage:', e);
      return null;
    }
  }

  /**
   * Guardar tema en localStorage
   */
  setStoredTheme(theme) {
    try {
      localStorage.setItem(this.STORAGE_KEY, theme);
    } catch (e) {
      console.warn('No se pudo guardar el tema en localStorage:', e);
    }
  }

  /**
   * Detectar preferencia de tema del sistema
   */
  getSystemTheme() {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
    return 'light';
  }

  /**
   * Aplicar tema al documento
   */
  applyTheme(theme, animate = true) {
    const html = document.documentElement;
    const body = document.body;

    // Agregar clase para prevenir transiciones durante la carga inicial
    if (!animate) {
      html.classList.add('theme-loading');
    }

    // Aplicar el tema
    html.setAttribute(this.THEME_ATTRIBUTE, theme);
    this.currentTheme = theme;

    // Actualizar meta theme-color para navegadores móviles
    this.updateMetaThemeColor(theme);

    // Actualizar iconos del toggle
    this.updateToggleIcons();

    // Guardar preferencia
    this.setStoredTheme(theme);

    // Remover clase de carga después de un frame
    if (!animate) {
      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          html.classList.remove('theme-loading');
        });
      });
    }

    // Disparar evento personalizado
    this.dispatchThemeChangeEvent(theme);
  }

  /**
   * Actualizar color de tema en navegadores móviles
   */
  updateMetaThemeColor(theme) {
    let metaThemeColor = document.querySelector('meta[name="theme-color"]');

    if (!metaThemeColor) {
      metaThemeColor = document.createElement('meta');
      metaThemeColor.name = 'theme-color';
      document.head.appendChild(metaThemeColor);
    }

    // Colores optimizados para iOS y Android
    const colors = {
      light: '#ffffff',
      dark: '#0f172a'
    };

    metaThemeColor.content = colors[theme] || colors.light;
  }

  /**
   * Alternar entre temas
   */
  toggleTheme() {
    const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';

    // Agregar clase de transición al toggle
    const toggleButtons = document.querySelectorAll('.theme-toggle');
    toggleButtons.forEach(btn => {
      btn.classList.add('transitioning');
      setTimeout(() => btn.classList.remove('transitioning'), 300);
    });

    // Aplicar nuevo tema con animación
    this.applyTheme(newTheme, true);
  }

  /**
   * Actualizar iconos del toggle
   */
  updateToggleIcons() {
    const toggleButtons = document.querySelectorAll('.theme-toggle');

    toggleButtons.forEach(button => {
      const moonIcon = button.querySelector('.icon-moon');
      const sunIcon = button.querySelector('.icon-sun');

      if (moonIcon && sunIcon) {
        if (this.currentTheme === 'dark') {
          moonIcon.style.display = 'none';
          sunIcon.style.display = 'block';
        } else {
          moonIcon.style.display = 'block';
          sunIcon.style.display = 'none';
        }
      }
    });
  }

  /**
   * Configurar event listeners
   */
  setupListeners() {
    // Toggle buttons
    const toggleButtons = document.querySelectorAll('.theme-toggle');
    toggleButtons.forEach(button => {
      button.addEventListener('click', () => this.toggleTheme());
    });

    // Escuchar cambios en la preferencia del sistema
    if (window.matchMedia) {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

      // Método moderno
      if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', (e) => this.handleSystemThemeChange(e));
      }
      // Fallback para navegadores antiguos
      else if (mediaQuery.addListener) {
        mediaQuery.addListener((e) => this.handleSystemThemeChange(e));
      }
    }

    // Actualizar iconos inicialmente
    this.updateToggleIcons();
  }

  /**
   * Manejar cambios en la preferencia del sistema
   */
  handleSystemThemeChange(e) {
    // Solo cambiar automáticamente si no hay preferencia guardada
    if (!this.getStoredTheme()) {
      const newTheme = e.matches ? 'dark' : 'light';
      this.applyTheme(newTheme, true);
    }
  }

  /**
   * Disparar evento personalizado cuando cambia el tema
   */
  dispatchThemeChangeEvent(theme) {
    const event = new CustomEvent('themechange', {
      detail: { theme },
      bubbles: true,
      cancelable: false
    });
    document.dispatchEvent(event);
  }

  /**
   * Obtener tema actual
   */
  getCurrentTheme() {
    return this.currentTheme;
  }

  /**
   * Establecer tema específico
   */
  setTheme(theme) {
    if (theme === 'light' || theme === 'dark') {
      this.applyTheme(theme, true);
    } else {
      console.warn('Tema no válido. Use "light" o "dark".');
    }
  }

  /**
   * Resetear tema a la preferencia del sistema
   */
  resetToSystemTheme() {
    try {
      localStorage.removeItem(this.STORAGE_KEY);
    } catch (e) {
      console.warn('No se pudo eliminar la preferencia de tema:', e);
    }

    const systemTheme = this.getSystemTheme();
    this.applyTheme(systemTheme, true);
  }
}

// Crear instancia global del ThemeManager
// Se ejecuta inmediatamente para evitar flash
const themeManager = new ThemeManager();

// Exponer API global
window.ThemeManager = {
  toggle: () => themeManager.toggleTheme(),
  setTheme: (theme) => themeManager.setTheme(theme),
  getCurrentTheme: () => themeManager.getCurrentTheme(),
  resetToSystem: () => themeManager.resetToSystemTheme()
};

// Log para debugging
console.log('🎨 Theme Manager inicializado. Tema actual:', themeManager.getCurrentTheme());

/**
 * =======================================
 * UTILIDADES ADICIONALES
 * =======================================
 */

// Detectar si el dispositivo prefiere colores oscuros
function prefersDarkMode() {
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

// Detectar plataforma
function detectPlatform() {
  const ua = navigator.userAgent.toLowerCase();

  if (/iphone|ipad|ipod/.test(ua)) {
    return 'ios';
  } else if (/android/.test(ua)) {
    return 'android';
  } else if (/windows/.test(ua)) {
    return 'windows';
  } else if (/macintosh|mac os x/.test(ua)) {
    return 'macos';
  }

  return 'unknown';
}

// Agregar clase de plataforma al body
document.addEventListener('DOMContentLoaded', () => {
  const platform = detectPlatform();
  document.body.classList.add(`platform-${platform}`);

  // Agregar clase para dispositivos móviles
  if (platform === 'ios' || platform === 'android') {
    document.body.classList.add('platform-mobile');
  } else {
    document.body.classList.add('platform-desktop');
  }

  console.log('📱 Plataforma detectada:', platform);
});

/**
 * =======================================
 * COMPATIBILIDAD CON NAVEGADORES ANTIGUOS
 * =======================================
 */

// Polyfill para CustomEvent (IE11 y navegadores antiguos)
(function() {
  if (typeof window.CustomEvent === 'function') return;

  function CustomEvent(event, params) {
    params = params || { bubbles: false, cancelable: false, detail: null };
    const evt = document.createEvent('CustomEvent');
    evt.initCustomEvent(event, params.bubbles, params.cancelable, params.detail);
    return evt;
  }

  window.CustomEvent = CustomEvent;
})();

// Polyfill para requestAnimationFrame
(function() {
  let lastTime = 0;
  const vendors = ['webkit', 'moz'];

  for (let x = 0; x < vendors.length && !window.requestAnimationFrame; ++x) {
    window.requestAnimationFrame = window[vendors[x] + 'RequestAnimationFrame'];
    window.cancelAnimationFrame = window[vendors[x] + 'CancelAnimationFrame'] ||
                                   window[vendors[x] + 'CancelRequestAnimationFrame'];
  }

  if (!window.requestAnimationFrame) {
    window.requestAnimationFrame = function(callback) {
      const currTime = new Date().getTime();
      const timeToCall = Math.max(0, 16 - (currTime - lastTime));
      const id = window.setTimeout(() => callback(currTime + timeToCall), timeToCall);
      lastTime = currTime + timeToCall;
      return id;
    };
  }

  if (!window.cancelAnimationFrame) {
    window.cancelAnimationFrame = function(id) {
      clearTimeout(id);
    };
  }
})();

/**
 * =======================================
 * EXPORTAR PARA USO EN OTROS SCRIPTS
 * =======================================
 */
if (typeof module !== 'undefined' && module.exports) {
  module.exports = ThemeManager;
}
