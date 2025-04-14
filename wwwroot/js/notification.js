function initializeNotifications() {
    initializeBootstrapTabs();
    
    initializeCheckboxes();
    
    initializeMarkAsRead();
}

function initializeBootstrapTabs() {
    try {
        const tabElements = document.querySelectorAll('#notificationTabs .nav-link');
        
        if (tabElements.length > 0) {
            tabElements.forEach(function(tabElement) {
                tabElement.removeEventListener('click', handleTabClick);
                tabElement.addEventListener('click', handleTabClick);
            });
            
            console.log('Notification tabs initialized');
        }
    } catch (error) {
        console.error('Error initializing notification tabs:', error);
    }
}

function handleTabClick(event) {
    event.preventDefault();
    
    const targetId = this.getAttribute('data-bs-target');
    if (!targetId) return;
    
    document.querySelectorAll('#notificationTabs .nav-link').forEach(function(tab) {
        tab.classList.remove('active');
    });
    
    document.querySelectorAll('.tab-pane').forEach(function(pane) {
        pane.classList.remove('show', 'active');
    });
    
    this.classList.add('active');
    
    const targetPane = document.querySelector(targetId);
    if (targetPane) {
        targetPane.classList.add('show', 'active');
    }
}

function initializeCheckboxes() {
    try {
        const checkboxes = document.querySelectorAll('input[type="checkbox"]');
        
        if (checkboxes.length > 0) {
            checkboxes.forEach(function(checkbox) {
                checkbox.removeEventListener('change', handleCheckboxChange);
                checkbox.addEventListener('change', handleCheckboxChange);
                
                handleCheckboxChange.call(checkbox);
            });
            
            console.log('Notification checkboxes initialized');
        }
    } catch (error) {
        console.error('Error initializing checkboxes:', error);
    }
}

function handleCheckboxChange() {
    const hiddenFields = Array.from(document.querySelectorAll('input[type="hidden"]'));
    const hiddenField = hiddenFields.find(input => input.name === this.name);
    
    if (hiddenField) {
        if (this.checked) {
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

function toggleTimingOption(element, checkboxId) {
    const checkbox = document.getElementById(checkboxId);
    if (!checkbox) return;
    
    checkbox.checked = !checkbox.checked;
    
    if (checkbox.checked) {
        element.classList.add('selected');
    } else {
        element.classList.remove('selected');
    }
    
    const hiddenField = Array.from(document.querySelectorAll('input[type="hidden"]'))
        .find(input => input.name === checkbox.name);
        
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

function initializeMarkAsRead() {
    try {
        const markReadForms = document.querySelectorAll('.mark-read-form');
        
        if (markReadForms.length > 0) {
            markReadForms.forEach(function(form) {
                form.removeEventListener('submit', handleMarkAsReadSubmit);
                form.addEventListener('submit', handleMarkAsReadSubmit);
            });
            
            console.log('Mark as read forms initialized');
        }
    } catch (error) {
        console.error('Error initializing mark as read forms:', error);
    }
}

function handleMarkAsReadSubmit(event) {
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
                button.classList.add('read');
                button.innerHTML = '<i class="bi bi-check-circle-fill"></i> Already Read';
                
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

window.toggleTimingOption = toggleTimingOption;
window.handleMarkAsReadSubmit = handleMarkAsReadSubmit;

document.addEventListener('DOMContentLoaded', initializeNotifications);

document.addEventListener('contentLoaded', initializeNotifications);