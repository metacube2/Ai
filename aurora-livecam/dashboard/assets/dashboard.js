/**
 * Dashboard JavaScript
 */

document.addEventListener('DOMContentLoaded', function() {
    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.3s';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });

    // Mobile sidebar toggle
    const sidebar = document.querySelector('.sidebar');
    const mainContent = document.querySelector('.main-content');

    if (window.innerWidth <= 768) {
        // Add menu button
        const menuBtn = document.createElement('button');
        menuBtn.className = 'btn btn-secondary';
        menuBtn.style.cssText = 'position: fixed; top: 10px; left: 10px; z-index: 200; padding: 0.5rem;';
        menuBtn.innerHTML = '☰';
        menuBtn.onclick = () => sidebar.classList.toggle('open');
        document.body.appendChild(menuBtn);

        // Close sidebar on content click
        mainContent.addEventListener('click', () => {
            sidebar.classList.remove('open');
        });
    }

    // Color picker live preview
    document.querySelectorAll('.color-picker').forEach(picker => {
        picker.addEventListener('input', function() {
            const wrapper = this.closest('.color-picker-wrapper');
            if (wrapper) {
                const valueDisplay = wrapper.querySelector('.color-value');
                if (valueDisplay) {
                    valueDisplay.textContent = this.value;
                }
            }
        });
    });

    // Form unsaved changes warning
    const forms = document.querySelectorAll('form');
    let formChanged = false;

    forms.forEach(form => {
        form.addEventListener('change', () => {
            formChanged = true;
        });

        form.addEventListener('submit', () => {
            formChanged = false;
        });
    });

    window.addEventListener('beforeunload', (e) => {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = '';
        }
    });

    // Stats refresh (every 30 seconds on overview page)
    if (document.querySelector('.stats-grid')) {
        setInterval(refreshStats, 30000);
    }
});

/**
 * Refresh stats via AJAX
 */
function refreshStats() {
    fetch('/dashboard/api/stats.php')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateStatCard('viewers_current', data.stats.viewers_current);
                updateStatCard('viewers_today', data.stats.viewers_today);
                updateStatCard('viewers_peak', data.stats.viewers_peak);
            }
        })
        .catch(err => console.log('Stats refresh failed:', err));
}

/**
 * Update a stat card value
 */
function updateStatCard(id, value) {
    const cards = document.querySelectorAll('.stat-card');
    cards.forEach(card => {
        const label = card.querySelector('.stat-label');
        if (label) {
            // Match by label text (simplified)
            const valueEl = card.querySelector('.stat-value');
            if (valueEl && typeof value !== 'undefined') {
                valueEl.textContent = value;
            }
        }
    });
}

/**
 * Show notification toast
 */
function showNotification(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type}`;
    toast.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 1000; min-width: 300px;';
    toast.textContent = message;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.transition = 'opacity 0.3s';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

/**
 * Confirm dangerous actions
 */
function confirmAction(message) {
    return confirm(message || 'Sind Sie sicher?');
}
