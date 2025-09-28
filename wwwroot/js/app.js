/**
 * QuantumAttend - Advanced Attendance Management System
 * Ultra-modern JavaScript architecture with real-time features
 */

'use strict';

// ============================================================================
// 🌟 GLOBAL UTILITIES & CONFIGURATION
// ============================================================================

window.QuantumAttend = {
    version: '2.0.0',
    theme: 'system',
    config: {
        apiBase: '/api',
        refreshInterval: 30000,
        qrRefreshInterval: 300000, // 5 minutes
        animationDuration: 300
    }
};

// HTTP Request Utility with Advanced Error Handling
window.Utils = {
    async request(url, options = {}) {
        const config = {
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                ...options.headers
            },
            ...options
        };

        try {
            console.log(`🌐 API Request: ${options.method || 'GET'} ${url}`);
            
            const response = await fetch(url, config);
            
            if (!response.ok) {
                let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
                
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.title || errorMessage;
                } catch (e) {
                    const errorText = await response.text();
                    if (errorText) errorMessage = errorText;
                }
                
                throw new Error(errorMessage);
            }

            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                console.log(`✅ API Response:`, data);
                return data;
            }
            
            return await response.text();
        } catch (error) {
            console.error(`❌ API Error:`, error);
            
            // Enhanced error messages for better UX
            if (error.name === 'TypeError' && error.message.includes('fetch')) {
                throw new Error('Error de conexión. Verifica tu conexión a internet.');
            }
            
            if (error.message.includes('NetworkError')) {
                throw new Error('Error de red. El servidor podría estar inaccesible.');
            }
            
            throw error;
        }
    },

    // Debounce utility for performance optimization
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func.apply(this, args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    // Throttle utility for scroll/resize events
    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    // Format date with locale support
    formatDate(date, options = {}) {
        const defaultOptions = {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };
        return new Intl.DateTimeFormat('es-ES', { ...defaultOptions, ...options }).format(new Date(date));
    },

    // Generate unique ID
    generateId() {
        return Date.now().toString(36) + Math.random().toString(36).substr(2);
    },

    // Copy to clipboard with feedback
    async copyToClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
            this.showNotification('Copiado al portapapeles', 'success');
            return true;
        } catch (err) {
            console.error('Failed to copy text: ', err);
            this.showNotification('Error al copiar', 'error');
            return false;
        }
    },

    // Advanced notification system
    showNotification(message, type = 'info', duration = 5000) {
        const notification = document.createElement('div');
        notification.className = `quantum-notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i data-lucide="${this.getNotificationIcon(type)}"></i>
                <span>${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">
                    <i data-lucide="x"></i>
                </button>
            </div>
        `;

        // Create container if it doesn't exist
        let container = document.getElementById('notification-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'notification-container';
            container.className = 'notification-container';
            document.body.appendChild(container);
        }

        container.appendChild(notification);
        
        // Initialize icons
        if (window.lucide) {
            lucide.createIcons();
        }

        // Auto remove
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => notification.remove(), 300);
        }, duration);

        return notification;
    },

    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'alert-circle',
            warning: 'alert-triangle',
            info: 'info'
        };
        return icons[type] || icons.info;
    },

    // Local storage with expiration
    setStorageItem(key, value, expirationMs = null) {
        const item = {
            value,
            timestamp: Date.now(),
            expiration: expirationMs ? Date.now() + expirationMs : null
        };
        localStorage.setItem(key, JSON.stringify(item));
    },

    getStorageItem(key) {
        const item = localStorage.getItem(key);
        if (!item) return null;

        try {
            const parsed = JSON.parse(item);
            if (parsed.expiration && Date.now() > parsed.expiration) {
                localStorage.removeItem(key);
                return null;
            }
            return parsed.value;
        } catch (e) {
            localStorage.removeItem(key);
            return null;
        }
    },

    // Legacy methods for compatibility
    showStatus(element, message, type = 'success') {
        if (!element) return;
        element.innerHTML = `<div class="quantum-alert alert-${type}">${message}</div>`;
        setTimeout(() => {
            if (element.innerHTML.includes(message)) {
                element.innerHTML = '';
            }
        }, 5000);
    },

    showLoading(element, text = 'Cargando...') {
        if (!element) return;
        element.innerHTML = `
            <div class="loading-state">
                <div class="quantum-spinner">
                    <div class="spinner-ring"></div>
                    <div class="spinner-ring"></div>
                    <div class="spinner-ring"></div>
                </div>
                <p>${text}</p>
            </div>
        `;
    }
};

// ============================================================================
// 🎨 THEME MANAGEMENT SYSTEM
// ============================================================================

window.ThemeManager = {
    init() {
        this.loadTheme();
        this.setupThemeToggle();
        this.watchSystemTheme();
        console.log('🎨 Theme Manager initialized');
    },

    loadTheme() {
        const savedTheme = Utils.getStorageItem('theme') || 'system';
        this.setTheme(savedTheme);
    },

    setTheme(theme) {
        const html = document.documentElement;
        const body = document.body;

        // Remove existing theme classes
        html.classList.remove('theme-light', 'theme-dark', 'theme-system');
        body.classList.remove('theme-transition');

        // Add transition class for smooth theme switching
        body.classList.add('theme-transition');

        if (theme === 'system') {
            html.classList.add('theme-system');
            // Remove data-theme to let CSS media query handle it
            html.removeAttribute('data-theme');
        } else {
            html.classList.add(`theme-${theme}`);
            html.setAttribute('data-theme', theme);
        }

        // Update theme toggle button
        this.updateThemeToggle(theme);

        // Save preference
        Utils.setStorageItem('theme', theme);
        window.QuantumAttend.theme = theme;

        // Dispatch theme change event
        window.dispatchEvent(new CustomEvent('themechange', { detail: { theme } }));

        // Remove transition class after animation
        setTimeout(() => {
            body.classList.remove('theme-transition');
        }, 300);

        console.log(`🎨 Theme changed to: ${theme}`);
    },

    setupThemeToggle() {
        const toggle = document.getElementById('themeToggle');
        if (!toggle) return;

        toggle.addEventListener('click', () => {
            const currentTheme = window.QuantumAttend.theme;
            const themes = ['light', 'dark', 'system'];
            const currentIndex = themes.indexOf(currentTheme);
            const nextTheme = themes[(currentIndex + 1) % themes.length];
            this.setTheme(nextTheme);
        });
    },

    updateThemeToggle(theme) {
        const toggle = document.getElementById('themeToggle');
        if (!toggle) return;

        const lightIcon = toggle.querySelector('.light-icon');
        const darkIcon = toggle.querySelector('.dark-icon');

        if (theme === 'light') {
            lightIcon.style.opacity = '1';
            lightIcon.style.transform = 'rotate(0deg) scale(1)';
            darkIcon.style.opacity = '0';
            darkIcon.style.transform = 'rotate(180deg) scale(0.8)';
        } else if (theme === 'dark') {
            lightIcon.style.opacity = '0';
            lightIcon.style.transform = 'rotate(-180deg) scale(0.8)';
            darkIcon.style.opacity = '1';
            darkIcon.style.transform = 'rotate(0deg) scale(1)';
        } else {
            // System theme - show appropriate icon based on system preference
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (prefersDark) {
                lightIcon.style.opacity = '0';
                darkIcon.style.opacity = '1';
            } else {
                lightIcon.style.opacity = '1';
                darkIcon.style.opacity = '0';
            }
        }
    },

    watchSystemTheme() {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        mediaQuery.addEventListener('change', () => {
            if (window.QuantumAttend.theme === 'system') {
                this.updateThemeToggle('system');
            }
        });
    }
};

// ============================================================================
// 🔄 QR MANAGEMENT SYSTEM
// ============================================================================

window.QRManager = {
    currentClassId: null,
    refreshTimer: null,
    countdownTimer: null,
    container: null,

    init() {
        this.container = document.getElementById('qrContainer');
        console.log('📱 QR Manager initialized');
    },

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
    },

    async loadQr() {
        if (!this.currentClassId || !this.container) return;

        try {
            const qrData = await Utils.request(`/api/clases/${this.currentClassId}/qr`);
            this.displayQR(qrData);
            console.log('📱 QR loaded successfully');
        } catch (error) {
            console.error('📱 QR loading failed:', error);
            Utils.showNotification('Error cargando QR: ' + error.message, 'error');
        }
    },

    displayQR(qrData) {
        if (!this.container) return;

        const expirationTime = new Date(qrData.expiraUtc);
        const now = new Date();
        const timeLeft = Math.max(0, Math.floor((expirationTime - now) / 1000));

        this.container.innerHTML = `
            <div class="qr-display">
                <div class="qr-header">
                    <h4>📱 ${qrData.claseInfo.asignatura}</h4>
                    <div class="qr-timer" data-expires="${qrData.expiraUtc}">
                        <i data-lucide="clock"></i>
                        <span class="timer-text">${this.formatTimeLeft(timeLeft)}</span>
                    </div>
                </div>
                
                <div class="qr-image-container">
                    <img src="data:image/png;base64,${qrData.base64Png}" 
                         alt="QR Code" 
                         class="qr-image" />
                    <div class="qr-overlay">
                        <button class="qr-action-btn" onclick="QRManager.downloadQR('${qrData.base64Png}', '${qrData.claseInfo.asignatura}')">
                            <i data-lucide="download"></i>
                            Descargar
                        </button>
                        <button class="qr-action-btn" onclick="Utils.copyToClipboard('${qrData.url}')">
                            <i data-lucide="copy"></i>
                            Copiar URL
                        </button>
                    </div>
                </div>
                
                <div class="qr-info">
                    <div class="qr-details">
                        <div class="detail-item">
                            <i data-lucide="link"></i>
                            <span>URL: <code>${qrData.url}</code></span>
                        </div>
                        <div class="detail-item">
                            <i data-lucide="shield"></i>
                            <span>Token seguro con expiración automática</span>
                        </div>
                        <div class="detail-item">
                            <i data-lucide="smartphone"></i>
                            <span>Compatible con cualquier app de QR</span>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Initialize icons
        if (window.lucide) {
            lucide.createIcons();
        }

        // Start countdown timer
        this.startCountdown();
    },

    startCountdown() {
        const timer = this.container?.querySelector('.qr-timer');
        if (!timer) return;

        const expiration = new Date(timer.getAttribute('data-expires'));
        
        const updateTimer = () => {
            const now = new Date();
            const timeLeft = Math.max(0, Math.floor((expiration - now) / 1000));
            
            const timerText = timer.querySelector('.timer-text');
            if (timerText) {
                timerText.textContent = this.formatTimeLeft(timeLeft);
                
                // Change color based on time left
                if (timeLeft < 60) {
                    timer.classList.add('timer-warning');
                } else if (timeLeft < 30) {
                    timer.classList.add('timer-danger');
                }
                
                if (timeLeft === 0) {
                    timerText.textContent = 'Expirado';
                    timer.classList.add('timer-expired');
                    Utils.showNotification('QR expirado. Generando nuevo código...', 'warning');
                    setTimeout(() => this.loadQr(), 1000);
                }
            }
        };

        updateTimer();
        this.countdownTimer = setInterval(updateTimer, 1000);
    },

    formatTimeLeft(seconds) {
        if (seconds <= 0) return 'Expirado';
        
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        
        if (minutes > 0) {
            return `${minutes}m ${secs}s`;
        }
        return `${secs}s`;
    },

    startAutoRefresh() {
        this.stopAutoRefresh();
        
        // Only refresh if page is visible and user is active
        this.refreshTimer = setInterval(() => {
            if (this.currentClassId && !document.hidden) {
                this.loadQr();
            }
        }, window.QuantumAttend.config.qrRefreshInterval);
        
        console.log('📱 QR auto-refresh started');
    },

    stopAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
            this.refreshTimer = null;
        }
        
        if (this.countdownTimer) {
            clearInterval(this.countdownTimer);
            this.countdownTimer = null;
        }
    },

    downloadQR(base64Data, className) {
        try {
            const link = document.createElement('a');
            link.download = `QR-${className.replace(/[^a-z0-9]/gi, '_')}.png`;
            link.href = `data:image/png;base64,${base64Data}`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            
            Utils.showNotification('QR descargado exitosamente', 'success');
        } catch (error) {
            Utils.showNotification('Error al descargar QR', 'error');
        }
    },

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
                        <div class="loading-state">
                            <div class="quantum-spinner">
                                <div class="spinner-ring"></div>
                                <div class="spinner-ring"></div>
                                <div class="spinner-ring"></div>
                            </div>
                            <p>Generando QR...</p>
                        </div>
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
        
        // Set container to modal container
        this.container = modal.querySelector('#modalQrContainer');
        
        // Cargar QR
        setTimeout(() => {
            this.loadQr();
            this.startAutoRefresh();
        }, 100);
    }
};

// ============================================================================
// 📊 REAL-TIME STATS MANAGER
// ============================================================================

window.StatsManager = {
    updateInterval: null,

    init() {
        this.loadQuickStats();
        this.startRealTimeUpdates();
        console.log('📊 Stats Manager initialized');
    },

    async loadQuickStats() {
        try {
            const [students, classes, attendances] = await Promise.all([
                Utils.request('/api/alumnos'),
                Utils.request('/api/clases'),
                Utils.request('/api/asistencias')
            ]);

            const stats = {
                totalStudents: students.length,
                activeClasses: classes.filter(c => c.activa).length,
                todayAttendance: this.getTodayAttendances(attendances).length
            };

            this.updateHeaderStats(stats);
            
        } catch (error) {
            console.error('📊 Error loading stats:', error);
        }
    },

    getTodayAttendances(attendances) {
        const today = new Date().toDateString();
        return attendances.filter(a => 
            new Date(a.marcadaUtc).toDateString() === today
        );
    },

    updateHeaderStats(stats) {
        this.animateCounter('totalStudents', stats.totalStudents);
        this.animateCounter('activeClasses', stats.activeClasses);
        this.animateCounter('todayAttendance', stats.todayAttendance);
    },

    animateCounter(elementId, targetValue) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const currentValue = parseInt(element.textContent) || 0;
        const safeTargetValue = Math.max(0, targetValue || 0); // Prevent negative values
        
        if (currentValue === safeTargetValue) return; // Skip if no change
        
        // Prevent multiple animations on same element
        if (element.dataset.animating === 'true') {
            element.textContent = safeTargetValue;
            return;
        }
        
        element.dataset.animating = 'true';
        
        // Simple direct update to avoid loops
        element.textContent = safeTargetValue;
        
        // Flash effect on change
        element.style.color = 'var(--primary)';
        setTimeout(() => {
            element.style.color = '';
            element.dataset.animating = 'false';
        }, 300);
    },

    startRealTimeUpdates() {
        // Clear existing interval
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
        }
        
        this.updateInterval = setInterval(() => {
            // Only update if page is visible
            if (!document.hidden) {
                this.loadQuickStats();
            }
        }, window.QuantumAttend.config.refreshInterval * 2); // Double the interval
    },

    stop() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
    }
};

// ============================================================================
// 🎬 ANIMATION & UX ENHANCEMENTS
// ============================================================================

window.AnimationManager = {
    scrollProgress: null,
    parallaxElements: [],
    
    init() {
        this.setupScrollAnimations();
        this.setupHoverEffects();
        this.setupLoadingStates();
        this.setupScrollProgress();
        this.setupParallaxEffect();
        this.setupStaggerAnimations();
        this.createFloatingActionButton();
        console.log('🎬 Animation Manager initialized');
    },

    setupScrollAnimations() {
        // Skip animations completely for now to fix visibility issues
        console.log('🎬 Scroll animations disabled to fix visibility issues');
        
        // Ensure all elements are immediately visible
        document.querySelectorAll('.quantum-card, .slide-reveal, .text-reveal, .stagger-parent, .animate-in').forEach(element => {
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
            element.style.visibility = 'visible';
            
            // Add revealed classes immediately
            if (element.classList.contains('slide-reveal')) {
                element.classList.add('revealed');
            }
            if (element.classList.contains('text-reveal')) {
                element.classList.add('revealed');
            }
        });
    },

    setupHoverEffects() {
        // Skip heavy effects on mobile/touch devices
        if ('ontouchstart' in window) return;
        
        // Use event delegation for better performance
        document.addEventListener('mouseenter', (e) => {
            if (e.target && e.target.closest && e.target.closest('.quantum-btn')) {
                this.addMagneticEffect(e);
            }
        }, true);
        
        document.addEventListener('mouseleave', (e) => {
            if (e.target && e.target.closest && e.target.closest('.quantum-btn')) {
                this.removeMagneticEffect(e);
            }
        }, true);
        
        // Add tilt effect to cards with throttling
        document.addEventListener('mousemove', Utils.throttle((e) => {
            if (e.target && e.target.closest) {
                const card = e.target.closest('.tilt-card');
                if (card) {
                    this.handleTiltEffect(e);
                }
            }
        }, 16), true); // ~60fps
        
        document.addEventListener('mouseleave', (e) => {
            if (e.target && e.target.closest && e.target.closest('.tilt-card')) {
                this.resetTiltEffect(e);
            }
        }, true);
    },

    addMagneticEffect(e) {
        const btn = e.target && e.target.closest ? e.target.closest('.quantum-btn') : null;
        if (!btn) return;
        
        btn.classList.add('magnetic-btn');
        
        const handleMouseMove = (e) => {
            const rect = btn.getBoundingClientRect();
            const x = e.clientX - rect.left - rect.width / 2;
            const y = e.clientY - rect.top - rect.height / 2;
            
            const intensity = 0.2;
            btn.style.transform = `translate(${x * intensity}px, ${y * intensity}px)`;
        };

        btn.addEventListener('mousemove', handleMouseMove);
        btn._magneticHandler = handleMouseMove;
    },

    removeMagneticEffect(e) {
        const btn = e.target && e.target.closest ? e.target.closest('.quantum-btn') : null;
        if (!btn) return;
        
        btn.style.transform = '';
        btn.classList.remove('magnetic-btn');
        
        if (btn._magneticHandler) {
            btn.removeEventListener('mousemove', btn._magneticHandler);
            delete btn._magneticHandler;
        }
    },

    handleTiltEffect(e) {
        const card = e.currentTarget;
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        const centerX = rect.width / 2;
        const centerY = rect.height / 2;
        
        const rotateX = (y - centerY) / centerY * -10;
        const rotateY = (x - centerX) / centerX * 10;
        
        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
    },

    resetTiltEffect(e) {
        const card = e.currentTarget;
        card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg)';
    },

    setupLoadingStates() {
        // Add loading animation to forms
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', (e) => {
                const submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) {
                    submitBtn.classList.add('loading');
                    setTimeout(() => {
                        submitBtn.classList.remove('loading');
                    }, 2000);
                }
            });
        });
        
        // Add skeleton loading states
        this.createSkeletonLoaders();
    },

    setupScrollProgress() {
        this.scrollProgress = document.createElement('div');
        this.scrollProgress.className = 'scroll-progress';
        document.body.appendChild(this.scrollProgress);
        
        window.addEventListener('scroll', Utils.throttle(() => {
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.scrollHeight - windowHeight;
            const scrolled = window.scrollY;
            const progress = (scrolled / documentHeight) * 100;
            
            this.scrollProgress.style.width = `${Math.min(100, Math.max(0, progress))}%`;
        }, 10));
    },

    setupParallaxEffect() {
        this.parallaxElements = document.querySelectorAll('.parallax-element');
        
        if (this.parallaxElements.length > 0) {
            window.addEventListener('scroll', Utils.throttle(() => {
                const scrolled = window.pageYOffset;
                
                this.parallaxElements.forEach((element, index) => {
                    const rate = scrolled * -0.1 * (index + 1);
                    element.style.transform = `translateY(${rate}px)`;
                });
            }, 10));
        }
    },

    setupStaggerAnimations() {
        // Disable stagger animations to fix visibility
        console.log('🎬 Stagger animations disabled to fix visibility issues');
        
        // Ensure all grid items are visible
        document.querySelectorAll('.quantum-grid').forEach(grid => {
            grid.style.opacity = '1';
            grid.style.visibility = 'visible';
            
            // Make all children visible too
            Array.from(grid.children).forEach(child => {
                child.style.opacity = '1';
                child.style.transform = 'translateY(0)';
                child.style.visibility = 'visible';
            });
        });
    },

    createFloatingActionButton() {
        const fab = document.createElement('button');
        fab.className = 'fab';
        fab.innerHTML = '<i data-lucide="arrow-up"></i>';
        fab.style.opacity = '0';
        fab.style.transform = 'translateY(100px)';
        
        fab.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
        
        document.body.appendChild(fab);
        
        // Show/hide FAB based on scroll position
        window.addEventListener('scroll', Utils.throttle(() => {
            const scrolled = window.pageYOffset;
            
            if (scrolled > 300) {
                fab.style.opacity = '1';
                fab.style.transform = 'translateY(0)';
            } else {
                fab.style.opacity = '0';
                fab.style.transform = 'translateY(100px)';
            }
        }, 100));
        
        // Initialize icon
        if (window.lucide) {
            lucide.createIcons();
        }
    },

    createSkeletonLoaders() {
        // Add skeleton states to tables while loading (skip elements with data-no-skeleton)
        document.querySelectorAll('.loading-state').forEach(loader => {
            if (loader.getAttribute('data-no-skeleton') === 'true') {
                return; // Skip this element
            }
            loader.innerHTML = `
                <div class="skeleton skeleton-title"></div>
                <div class="skeleton skeleton-text"></div>
                <div class="skeleton skeleton-text" style="width: 80%;"></div>
                <div class="skeleton skeleton-button"></div>
            `;
        });
    },

    // Utility methods for triggering animations
    triggerPulse(element) {
        element.classList.add('pulse-effect');
        setTimeout(() => element.classList.remove('pulse-effect'), 2000);
    },

    triggerBounce(element) {
        element.classList.add('bounce-effect');
        setTimeout(() => element.classList.remove('bounce-effect'), 600);
    },

    createParticleEffect(container, count = 20) {
        if (!container) return;
        
        for (let i = 0; i < count; i++) {
            const particle = document.createElement('div');
            particle.className = 'particle';
            particle.style.left = Math.random() * 100 + '%';
            particle.style.animationDelay = Math.random() * 3 + 's';
            particle.style.animationDuration = (2 + Math.random() * 2) + 's';
            
            container.appendChild(particle);
            
            // Remove particle after animation
            setTimeout(() => {
                if (particle.parentNode) {
                    particle.parentNode.removeChild(particle);
                }
            }, 3000);
        }
    },

    addRippleEffect(button, event) {
        const rect = button.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = event.clientX - rect.left - size / 2;
        const y = event.clientY - rect.top - size / 2;
        
        const ripple = document.createElement('div');
        ripple.style.cssText = `
            position: absolute;
            width: ${size}px;
            height: ${size}px;
            left: ${x}px;
            top: ${y}px;
            background: rgba(255, 255, 255, 0.6);
            border-radius: 50%;
            pointer-events: none;
            animation: ripple 0.6s ease-out;
        `;
        
        button.appendChild(ripple);
        
        setTimeout(() => ripple.remove(), 600);
    }
};

// ============================================================================
// 🔧 LEGACY SUPPORT FOR EXISTING CODE
// ============================================================================

// Legacy managers for compatibility
class AlumnosManager {
    constructor() {
        console.log('📚 Legacy AlumnosManager initialized');
    }

    async showQr(alumnoId) {
        try {
            const alumno = await Utils.request(`/api/alumnos/${alumnoId}`);
            const modal = this.createQrModal(alumno);
            document.body.appendChild(modal);
        } catch (error) {
            Utils.showNotification(`Error al obtener QR: ${error.message}`, 'error');
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

class ClasesManager {
    constructor() {
        console.log('📖 Legacy ClasesManager initialized');
    }

    async cerrarClase(claseId) {
        if (!confirm('¿Está seguro de cerrar esta clase?')) return;

        try {
            await Utils.request(`/api/clases/${claseId}/cerrar`, {
                method: 'POST'
            });
            
            Utils.showNotification('Clase cerrada exitosamente', 'success');
            
            // Trigger refresh if available
            if (window.Dashboard && window.Dashboard.loadClasses) {
                window.Dashboard.loadClasses();
            }
        } catch (error) {
            Utils.showNotification(`Error al cerrar clase: ${error.message}`, 'error');
        }
    }
}

class AsistenciasManager {
    constructor() {
        console.log('📋 Legacy AsistenciasManager initialized');
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
            container.innerHTML = `<div class="quantum-alert alert-danger">Error al cargar asistencias: ${error.message}</div>`;
        }
    }

    renderAsistencias(container, asistencias, claseId) {
        console.log('renderAsistencias called with:', { asistencias, length: asistencias?.length, type: typeof asistencias, isArray: Array.isArray(asistencias) });
        
        if (!asistencias || !Array.isArray(asistencias) || asistencias.length === 0) {
            console.log('No asistencias found or empty array');
            container.innerHTML = '<div class="text-center text-sm">No hay asistencias registradas</div>';
            return;
        }
        
        console.log('Rendering', asistencias.length, 'asistencias');

        const csvUrl = claseId ? `/api/asistencias/clase/${claseId}/csv` : '/api/asistencias/csv';

        container.innerHTML = `
            <div class="flex justify-between items-center mb-4">
                <h3>Asistencias ${claseId ? `- Clase ${claseId}` : ''}</h3>
                <a href="${csvUrl}" class="quantum-btn btn-sm btn-success">Descargar CSV</a>
            </div>
            <div class="quantum-table-container">
                <table class="quantum-table">
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
                                    <span class="status-badge ${asistencia.metodo === 'PROFESOR_ESCANEA' ? 'warning' : 'success'}">
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

// ============================================================================
// 📱 MOBILE OPTIMIZATION & GESTURE SUPPORT
// ============================================================================

window.MobileManager = {
    touchStartX: 0,
    touchStartY: 0,
    touchEndX: 0,
    touchEndY: 0,
    swipeThreshold: 50,
    
    init() {
        this.setupTouchGestures();
        this.setupPullToRefresh();
        this.setupMobileNavigation();
        this.setupTouchFeedback();
        this.optimizeForMobile();
        console.log('📱 Mobile Manager initialized');
    },

    setupTouchGestures() {
        // Swipe gestures for cards and navigation
        document.addEventListener('touchstart', (e) => {
            this.touchStartX = e.changedTouches[0].screenX;
            this.touchStartY = e.changedTouches[0].screenY;
        }, { passive: true });

        document.addEventListener('touchend', (e) => {
            this.touchEndX = e.changedTouches[0].screenX;
            this.touchEndY = e.changedTouches[0].screenY;
            this.handleSwipe(e);
        }, { passive: true });
    },

    handleSwipe(e) {
        const deltaX = this.touchEndX - this.touchStartX;
        const deltaY = this.touchEndY - this.touchStartY;
        
        // Determine swipe direction
        if (Math.abs(deltaX) > Math.abs(deltaY)) {
            // Horizontal swipe
            if (Math.abs(deltaX) > this.swipeThreshold) {
                if (deltaX > 0) {
                    this.onSwipeRight(e);
                } else {
                    this.onSwipeLeft(e);
                }
            }
        } else {
            // Vertical swipe
            if (Math.abs(deltaY) > this.swipeThreshold) {
                if (deltaY > 0) {
                    this.onSwipeDown(e);
                } else {
                    this.onSwipeUp(e);
                }
            }
        }
    },

    onSwipeLeft(e) {
        // Navigate to next section or close modals
        const modal = document.querySelector('.modal-overlay');
        if (modal) {
            modal.style.transform = 'translateX(-100%)';
            setTimeout(() => modal.remove(), 300);
        }
    },

    onSwipeRight(e) {
        // Navigate to previous section or open menu
        // Could implement navigation drawer
    },

    onSwipeUp(e) {
        // Show more content or go to top
        if (window.pageYOffset > 300) {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    },

    onSwipeDown(e) {
        // Pull to refresh functionality
        if (window.pageYOffset === 0) {
            this.triggerRefresh();
        }
    },

    setupPullToRefresh() {
        let pullDistance = 0;
        let isPulling = false;
        
        const pullIndicator = document.createElement('div');
        pullIndicator.className = 'pull-to-refresh-indicator';
        pullIndicator.innerHTML = `
            <div class="pull-icon">
                <i data-lucide="refresh-cw"></i>
            </div>
            <span>Desliza para actualizar</span>
        `;
        document.body.appendChild(pullIndicator);

        document.addEventListener('touchstart', (e) => {
            if (window.pageYOffset === 0) {
                isPulling = true;
                pullDistance = 0;
            }
        }, { passive: true });

        document.addEventListener('touchmove', (e) => {
            if (isPulling && window.pageYOffset === 0) {
                pullDistance = Math.max(0, e.touches[0].clientY - this.touchStartY);
                
                if (pullDistance > 0) {
                    e.preventDefault();
                    const maxPull = 100;
                    const normalizedDistance = Math.min(pullDistance / maxPull, 1);
                    
                    pullIndicator.style.transform = `translateY(${Math.min(pullDistance, maxPull)}px)`;
                    pullIndicator.style.opacity = normalizedDistance;
                    
                    if (pullDistance > 80) {
                        pullIndicator.classList.add('ready-to-refresh');
                    } else {
                        pullIndicator.classList.remove('ready-to-refresh');
                    }
                }
            }
        }, { passive: false });

        document.addEventListener('touchend', (e) => {
            if (isPulling) {
                isPulling = false;
                
                if (pullDistance > 80) {
                    this.triggerRefresh();
                }
                
                pullIndicator.style.transform = 'translateY(-100px)';
                pullIndicator.style.opacity = '0';
                pullIndicator.classList.remove('ready-to-refresh');
                
                setTimeout(() => {
                    pullIndicator.style.transform = 'translateY(-100px)';
                }, 300);
            }
        }, { passive: true });
    },

    async triggerRefresh() {
        const indicator = document.querySelector('.pull-to-refresh-indicator');
        indicator.classList.add('refreshing');
        
        try {
            // Trigger actual refresh logic
            if (window.Dashboard && window.Dashboard.loadInitialData) {
                await window.Dashboard.loadInitialData();
            }
            
            if (window.StatsManager && window.StatsManager.loadQuickStats) {
                await window.StatsManager.loadQuickStats();
            }
            
            Utils.showNotification('¡Contenido actualizado!', 'success');
        } catch (error) {
            Utils.showNotification('Error al actualizar', 'error');
        } finally {
            indicator.classList.remove('refreshing');
        }
    },

    setupMobileNavigation() {
        // Add bottom navigation for mobile
        if (window.innerWidth <= 768) {
            this.createBottomNavigation();
        }
        
        // Handle orientation changes
        window.addEventListener('orientationchange', () => {
            setTimeout(() => {
                this.optimizeForOrientation();
            }, 100);
        });
    },

    createBottomNavigation() {
        const bottomNav = document.createElement('div');
        bottomNav.className = 'bottom-navigation';
        bottomNav.innerHTML = `
            <div class="bottom-nav-items">
                <button class="bottom-nav-item active" data-section="dashboard">
                    <i data-lucide="home"></i>
                    <span>Inicio</span>
                </button>
                <button class="bottom-nav-item" data-section="students">
                    <i data-lucide="users"></i>
                    <span>Estudiantes</span>
                </button>
                <button class="bottom-nav-item" data-section="qr">
                    <i data-lucide="qr-code"></i>
                    <span>QR</span>
                </button>
                <button class="bottom-nav-item" data-section="stats">
                    <i data-lucide="bar-chart-3"></i>
                    <span>Estadísticas</span>
                </button>
            </div>
        `;
        
        document.body.appendChild(bottomNav);
        
        // Add navigation functionality
        bottomNav.querySelectorAll('.bottom-nav-item').forEach(item => {
            item.addEventListener('click', (e) => {
                const section = e.currentTarget.dataset.section;
                this.navigateToSection(section);
                
                // Update active state
                bottomNav.querySelectorAll('.bottom-nav-item').forEach(i => i.classList.remove('active'));
                e.currentTarget.classList.add('active');
            });
        });
        
        if (window.lucide) {
            lucide.createIcons();
        }
    },

    navigateToSection(section) {
        let targetElement;
        
        switch (section) {
            case 'dashboard':
                targetElement = document.querySelector('.quantum-grid.grid-hero');
                break;
            case 'students':
                targetElement = document.querySelector('#studentsTableContainer').closest('.quantum-card');
                break;
            case 'qr':
                targetElement = document.querySelector('.qr-section');
                break;
            case 'stats':
                targetElement = document.querySelector('.stats-panel');
                break;
        }
        
        if (targetElement) {
            targetElement.scrollIntoView({ 
                behavior: 'smooth', 
                block: 'center' 
            });
        }
    },

    setupTouchFeedback() {
        // Add haptic feedback for supported devices
        document.addEventListener('touchstart', (e) => {
            const element = e.target && e.target.closest ? e.target.closest('.quantum-btn, .action-btn, .link-card') : null;
            if (element) {
                // Add visual feedback
                element.style.transform = 'scale(0.95)';
                element.style.transition = 'transform 0.1s ease';
                
                // Add haptic feedback if supported
                if ('vibrate' in navigator) {
                    navigator.vibrate(10);
                }
            }
        }, { passive: true });

        document.addEventListener('touchend', (e) => {
            const element = e.target && e.target.closest ? e.target.closest('.quantum-btn, .action-btn, .link-card') : null;
            if (element) {
                element.style.transform = '';
                setTimeout(() => {
                    element.style.transition = '';
                }, 100);
            }
        }, { passive: true });
    },

    optimizeForMobile() {
        // Add mobile-specific classes
        if ('ontouchstart' in window) {
            document.body.classList.add('touch-device');
        }
        
        // Optimize viewport for mobile
        let viewport = document.querySelector('meta[name=viewport]');
        if (viewport) {
            viewport.setAttribute('content', 
                'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes'
            );
        }
        
        // Add iOS specific optimizations
        if (/iPad|iPhone|iPod/.test(navigator.userAgent)) {
            document.body.classList.add('ios-device');
            
            // Prevent zoom on input focus
            document.querySelectorAll('input, select, textarea').forEach(input => {
                input.addEventListener('focus', () => {
                    viewport.setAttribute('content', 
                        'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'
                    );
                });
                
                input.addEventListener('blur', () => {
                    viewport.setAttribute('content', 
                        'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes'
                    );
                });
            });
        }
    },

    optimizeForOrientation() {
        const isLandscape = window.innerHeight < window.innerWidth;
        
        if (isLandscape) {
            document.body.classList.add('landscape-mode');
            document.body.classList.remove('portrait-mode');
        } else {
            document.body.classList.add('portrait-mode');
            document.body.classList.remove('landscape-mode');
        }
        
        // Adjust layout for orientation
        if (isLandscape && window.innerWidth <= 768) {
            // Hide bottom navigation in landscape mobile
            const bottomNav = document.querySelector('.bottom-navigation');
            if (bottomNav) {
                bottomNav.style.display = 'none';
            }
        } else {
            const bottomNav = document.querySelector('.bottom-navigation');
            if (bottomNav) {
                bottomNav.style.display = 'flex';
            }
        }
    },

    // Double tap to zoom functionality
    setupDoubleTapZoom() {
        let lastTap = 0;
        
        document.addEventListener('touchend', (e) => {
            const currentTime = new Date().getTime();
            const tapLength = currentTime - lastTap;
            
            if (tapLength < 500 && tapLength > 0) {
                // Double tap detected
                const target = e.target && e.target.closest ? e.target.closest('.quantum-card') : null;
                if (target && !target.classList.contains('zoomed')) {
                    target.style.transform = 'scale(1.1)';
                    target.style.zIndex = '1000';
                    target.classList.add('zoomed');
                    
                    setTimeout(() => {
                        target.style.transform = '';
                        target.style.zIndex = '';
                        target.classList.remove('zoomed');
                    }, 2000);
                }
            }
            
            lastTap = currentTime;
        });
    }
};

// ============================================================================
// 🚀 INITIALIZATION & SETUP
// ============================================================================

function initializeQuantumAttend() {
    console.log('🚀 Initializing QuantumAttend v' + window.QuantumAttend.version);

    // Initialize core systems
    ThemeManager.init();
    QRManager.init();
    StatsManager.init();
    AnimationManager.init();
    
    // Initialize mobile optimizations
    if ('ontouchstart' in window || navigator.maxTouchPoints > 0) {
        MobileManager.init();
    }

    // Initialize legacy managers for compatibility
    window.alumnosManager = new AlumnosManager();
    window.clasesManager = new ClasesManager();
    window.qrManager = QRManager; // Use new QRManager
    window.asistenciasManager = new AsistenciasManager();

    // Setup global error handler
    window.addEventListener('error', (e) => {
        console.error('🔥 Global error:', e.error, e.filename, e.lineno);
        
        // Only show notification for non-script errors or critical errors
        if (e.error && e.error.name !== 'TypeError') {
            Utils.showNotification('Se produjo un error inesperado. Revisa la consola para más detalles.', 'error');
        }
    });

    // Setup unhandled promise rejection handler
    window.addEventListener('unhandledrejection', (e) => {
        console.error('🔥 Unhandled promise rejection:', e.reason);
        
        // Don't show user notification for network errors during background updates
        if (e.reason && !e.reason.message?.includes('fetch')) {
            Utils.showNotification('Error de conexión con el servidor', 'error');
        }
        
        e.preventDefault();
    });

    // Setup visibility change handler for performance
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            // Pause animations and updates when tab is not visible
            StatsManager.stop();
            QRManager.stopAutoRefresh();
        } else {
            // Resume when tab becomes visible
            StatsManager.startRealTimeUpdates();
            if (QRManager.currentClassId) {
                QRManager.startAutoRefresh();
            }
        }
    });

    // Add loading complete class to body and fix visibility
    window.addEventListener('load', () => {
        document.body.classList.add('loaded');
        
        // Force all potentially hidden elements to be visible
        setTimeout(() => {
            const elementsToFix = document.querySelectorAll('.quantum-grid, .quantum-card, .hero-card, .stats-panel, .animate-in, .stagger-animation, .slide-reveal, .text-reveal');
            elementsToFix.forEach(el => {
                el.style.opacity = '1';
                el.style.transform = 'none';
                el.style.visibility = 'visible';
                el.classList.add('revealed'); // For reveal animations
            });
            console.log('✅ All elements forced visible');
        }, 100);
        
        console.log('✅ QuantumAttend fully loaded');
    });

    console.log('✅ QuantumAttend initialization complete');
}

// ============================================================================
// 🎯 AUTO-INITIALIZATION
// ============================================================================

// Initialize theme management as early as possible
window.initializeTheme = function() {
    ThemeManager.init();
};

// Fix visibility immediately and then initialize
function fixVisibilityIssues() {
    // Immediate fix for animation classes that hide content
    const problematicElements = document.querySelectorAll('.animate-in, .stagger-animation, .hero-card, .stats-panel, .grid-hero');
    problematicElements.forEach(el => {
        el.style.opacity = '1';
        el.style.transform = 'translateY(0)';
        el.style.visibility = 'visible';
    });
    console.log('🔧 Fixed visibility for', problematicElements.length, 'elements');
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        fixVisibilityIssues();
        initializeQuantumAttend();
    });
} else {
    fixVisibilityIssues();
    initializeQuantumAttend();
}

// Add CSS for notifications and animations
const dynamicStyles = `
/* Notification System */
.notification-container {
    position: fixed;
    top: 20px;
    right: 20px;
    z-index: 10000;
    display: flex;
    flex-direction: column;
    gap: 10px;
    max-width: 400px;
}

.quantum-notification {
    background: var(--glass-bg);
    backdrop-filter: blur(20px);
    border: 1px solid var(--glass-border);
    border-radius: 16px;
    padding: 1rem;
    color: var(--text-primary);
    box-shadow: var(--shadow-lg);
    transform: translateX(100%);
    opacity: 0;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    animation: slideInNotification 0.3s ease-out forwards;
}

@keyframes slideInNotification {
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

.notification-content {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.notification-content i {
    flex-shrink: 0;
}

.notification-close {
    background: none;
    border: none;
    color: var(--text-secondary);
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;
    margin-left: auto;
    transition: var(--transition);
}

.notification-close:hover {
    background: var(--bg-secondary);
    color: var(--text-primary);
}

.notification-success {
    border-left: 4px solid var(--success);
}

.notification-error {
    border-left: 4px solid var(--danger);
}

.notification-warning {
    border-left: 4px solid var(--warning);
}

.notification-info {
    border-left: 4px solid var(--primary);
}

/* QR Display Enhancements */
.qr-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1.5rem;
}

.qr-timer {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background: var(--glass-bg);
    border-radius: 20px;
    border: 1px solid var(--glass-border);
    font-size: 0.875rem;
    color: var(--text-secondary);
    transition: var(--transition);
}

.qr-timer.timer-warning {
    border-color: var(--warning);
    color: var(--warning);
}

.qr-timer.timer-danger {
    border-color: var(--danger);
    color: var(--danger);
    animation: pulse 1s ease-in-out infinite;
}

.qr-timer.timer-expired {
    background: rgba(239, 68, 68, 0.1);
    border-color: var(--danger);
    color: var(--danger);
}

.qr-image-container {
    position: relative;
    display: inline-block;
    margin: 0 auto 1.5rem;
}

.qr-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.8);
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    opacity: 0;
    transition: var(--transition);
    border-radius: 20px;
}

.qr-image-container:hover .qr-overlay {
    opacity: 1;
}

.qr-action-btn {
    padding: 0.75rem 1rem;
    background: white;
    border: none;
    border-radius: 12px;
    color: var(--text-primary);
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
    transition: var(--transition);
}

.qr-action-btn:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-lg);
}

.qr-details {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.detail-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-size: 0.875rem;
    color: var(--text-secondary);
}

.detail-item i {
    color: var(--primary);
    flex-shrink: 0;
}

.detail-item code {
    background: var(--glass-bg);
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    font-family: 'JetBrains Mono', monospace;
    font-size: 0.8rem;
    word-break: break-all;
}

/* Modal System */
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.5);
    backdrop-filter: blur(10px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    animation: fadeIn 0.3s ease-out;
}

@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}

.modal-content {
    background: var(--bg-primary);
    border: 1px solid var(--glass-border);
    border-radius: 24px;
    max-width: 500px;
    width: 90%;
    max-height: 90vh;
    overflow-y: auto;
    box-shadow: var(--shadow-2xl);
    animation: slideInUp 0.3s ease-out;
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 2rem 2rem 0;
}

.modal-header h3 {
    margin: 0;
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-primary);
}

.modal-close {
    background: none;
    border: none;
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0.5rem;
    color: var(--text-secondary);
    border-radius: 8px;
    transition: var(--transition);
}

.modal-close:hover {
    background: var(--bg-secondary);
    color: var(--text-primary);
}

.modal-body {
    padding: 2rem;
}

/* Animation enhancements */
.animate-in {
    animation: slideInUp 0.6s ease-out;
}

@keyframes slideInUp {
    from {
        opacity: 0;
        transform: translateY(30px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Loading states */
.quantum-btn.loading {
    pointer-events: none;
    opacity: 0.7;
}

.quantum-btn.loading::after {
    content: '';
    width: 16px;
    height: 16px;
    border: 2px solid transparent;
    border-top: 2px solid currentColor;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-left: 0.5rem;
}

/* Theme transition */
.theme-transition,
.theme-transition * {
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease !important;
}

/* Mobile optimizations */
@media (max-width: 768px) {
    .notification-container {
        left: 20px;
        right: 20px;
        max-width: none;
    }
    
    .qr-overlay {
        position: static;
        opacity: 1;
        background: transparent;
        margin-top: 1rem;
    }
    
    .qr-action-btn {
        flex: 1;
    }

    .modal-content {
        margin: 1rem;
        width: calc(100% - 2rem);
    }

    .modal-header, .modal-body {
        padding: 1rem;
    }
}
`;

// Inject dynamic styles
const styleElement = document.createElement('style');
styleElement.textContent = dynamicStyles;
document.head.appendChild(styleElement);

console.log('🎨 QuantumAttend dynamic styles injected');

// Export for global access
window.QuantumAttend.Utils = Utils;
window.QuantumAttend.ThemeManager = ThemeManager;
window.QuantumAttend.QRManager = QRManager;
window.QuantumAttend.StatsManager = StatsManager;