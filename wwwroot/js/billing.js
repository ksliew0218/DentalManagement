/**
 * Billing page initialization script
 * Following the AJAX Navigation Guidelines
 */

function initializeBillingPage() {
    // Set up alert dismissal
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        const closeBtn = alert.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function() {
                alert.classList.add('d-none');
            });
        }
    });

    // Auto-hide alerts after 5 seconds
    setTimeout(() => {
        alerts.forEach(alert => {
            alert.classList.add('fade');
            setTimeout(() => {
                alert.classList.add('d-none');
            }, 500);
        });
    }, 5000);

    // Set up any tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Highlight the Billing menu item
    highlightMenuItem('billing');
}

// Highlight the menu item in the sidebar
function highlightMenuItem(menuItemId) {
    // Remove active class from all menu items
    const menuItems = document.querySelectorAll('.sidebar-nav .nav-link');
    menuItems.forEach(item => item.classList.remove('active'));
    
    // Add active class to the current menu item
    const currentItem = document.getElementById(menuItemId);
    if (currentItem) {
        currentItem.classList.add('active');
    }
    
    // Store the current active menu item in localStorage
    localStorage.setItem('activeMenuItem', menuItemId);
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeBillingPage);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeBillingPage);

// Make functions global if needed
window.highlightMenuItem = highlightMenuItem;