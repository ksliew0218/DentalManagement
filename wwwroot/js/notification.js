/**
 * Notifications functionality
 * Following AJAX Navigation Guidelines for consistent behavior
 */

// Main initialization function that will be called on both page load and after AJAX navigation
function initializeNotifications() {
    // Initialize Bootstrap tabs
    initializeBootstrapTabs();
    
    // Initialize checkbox handling
    initializeCheckboxes();
    
    // Initialize AJAX mark as read
    initializeMarkAsRead();
}

// Initialize Bootstrap tabs
function initializeBootstrapTabs() {
    try {
        const tabElements = document.querySelectorAll('#notificationTabs .nav-link');
        
        if (tabElements.length > 0) {
            tabElements.forEach(function(tabElement) {
                // Clean up any existing listeners to prevent duplicates
                tabElement.removeEventListener('click', handleTabClick);
                tabElement.addEventListener('click', handleTabClick);
            });
            
            console.log('Notification tabs initialized');
        }
    } catch (error) {
        console.error('Error initializing notification tabs:', error);
    }
}

// Handle tab clicks
function handleTabClick(event) {
    event.preventDefault();
    
    // Get the target tab
    const targetId = this.getAttribute('data-bs-target');
    if (!targetId) return;
    
    // Remove active class from all tabs and panes
    document.querySelectorAll('#notificationTabs .nav-link').forEach(function(tab) {
        tab.classList.remove('active');
    });
    
    document.querySelectorAll('.tab-pane').forEach(function(pane) {
        pane.classList.remove('show', 'active');
    });
    
    // Add active class to clicked tab
    this.classList.add('active');
    
    // Show the target pane
    const targetPane = document.querySelector(targetId);
    if (targetPane) {
        targetPane.classList.add('show', 'active');
    }
}

// Initialize checkboxes with hidden field handling
function initializeCheckboxes() {
    try {
        const checkboxes = document.querySelectorAll('input[type="checkbox"]');
        
        if (checkboxes.length > 0) {
            checkboxes.forEach(function(checkbox) {
                // Clean up any existing listeners to prevent duplicates
                checkbox.removeEventListener('change', handleCheckboxChange);
                checkbox.addEventListener('change', handleCheckboxChange);
                
                // Set initial state
                handleCheckboxChange.call(checkbox);
            });
            
            console.log('Notification checkboxes initialized');
        }
    } catch (error) {
        console.error('Error initializing checkboxes:', error);
    }
}

// Handle checkbox changes (for hidden field management)
function handleCheckboxChange() {
    // Find the corresponding hidden field with the same name
    const hiddenFields = Array.from(document.querySelectorAll('input[type="hidden"]'));
    const hiddenField = hiddenFields.find(input => input.name === this.name);
    
    if (hiddenField) {
        // If checkbox is checked, we want the hidden field to be ignored
        if (this.checked) {
            hiddenField.setAttribute('data-orig-name', hiddenField.name);
            hiddenField.removeAttribute('name');
        } else {
            // Restore the name attribute when unchecked
            const origName = hiddenField.getAttribute('data-orig-name');
            if (origName) {
                hiddenField.setAttribute('name', origName);
            }
        }
    }
}

// Function to toggle timing option
function toggleTimingOption(element, checkboxId) {
    // Get the checkbox
    const checkbox = document.getElementById(checkboxId);
    if (!checkbox) return;
    
    // Toggle the checkbox state
    checkbox.checked = !checkbox.checked;
    
    // Toggle the selected class
    if (checkbox.checked) {
        element.classList.add('selected');
    } else {
        element.classList.remove('selected');
    }
    
    // Find the hidden field
    const hiddenField = Array.from(document.querySelectorAll('input[type="hidden"]'))
        .find(input => input.name === checkbox.name);
        
    // Toggle the hidden field
    if (hiddenField) {
        if (checkbox.checked) {
            hiddenField.setAttribute('data-orig-name', hiddenField.name);
            hiddenField.removeAttribute('name');
        } else {
            const origName = hiddenField.getAttribute('data-orig-name');
            if (origName) {
                hiddenField.setAttribute('name', origName);
            }
        }
    }
}

// Initialize mark as read functionality
function initializeMarkAsRead() {
    try {
        const markReadForms = document.querySelectorAll('.mark-read-form');
        
        if (markReadForms.length > 0) {
            markReadForms.forEach(function(form) {
                // Clean up any existing listeners to prevent duplicates
                form.removeEventListener('submit', handleMarkAsReadSubmit);
                form.addEventListener('submit', handleMarkAsReadSubmit);
            });
            
            console.log('Mark as read forms initialized');
        }
    } catch (error) {
        console.error('Error initializing mark as read forms:', error);
    }
}

// Handle mark as read form submissions
function handleMarkAsReadSubmit(event) {
    // Only intercept for forms that aren't already read
    const button = this.querySelector('button');
    if (!button || button.classList.contains('read')) return;
    
    event.preventDefault();
    
    try {
        const form = this;
        const formData = new FormData(form);
        const url = form.getAttribute('action');
        
        fetch(url, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update the button appearance
                button.classList.add('read');
                button.innerHTML = '<i class="bi bi-check-circle-fill"></i> Already Read';
                
                // Update the notification count
                updateNotificationCount();
            }
        })
        .catch(error => {
            console.error('Error marking notification as read:', error);
        });
    } catch (error) {
        console.error('Error handling mark as read submission:', error);
    }
}

// Update the notification count badge
function updateNotificationCount() {
    const countBadge = document.querySelector('.notification-count');
    if (!countBadge) return;
    
    const currentCount = parseInt(countBadge.textContent);
    if (isNaN(currentCount)) return;
    
    if (currentCount > 1) {
        countBadge.textContent = currentCount - 1;
    } else {
        countBadge.remove();
    }
}

// Make functions globally available for HTML onclick handlers
window.toggleTimingOption = toggleTimingOption;
window.handleMarkAsReadSubmit = handleMarkAsReadSubmit;

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeNotifications);

// Re-initialize after AJAX content loads (for consistent behavior with ajax-loader.js)
document.addEventListener('contentLoaded', initializeNotifications);