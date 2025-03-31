/**
 * Doctor Dashboard JavaScript
 * Handles sidebar functionality, notifications, and dashboard interactions
 */

// Initialize sidebar functionality
function initializeSidebar() {
  const sidebar = document.getElementById('sidebar');
  const menuToggle = document.querySelector('.menu-toggle');
  const menuToggleCollapsed = document.querySelector('.menu-toggle-collapsed');
  const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');

  // Remove existing event listeners if any
  menuToggle?.removeEventListener('click', toggleSidebar);
  menuToggleCollapsed?.removeEventListener('click', toggleSidebar);
  mobileMenuToggle?.removeEventListener('click', toggleMobileSidebar);

  // Add new event listeners
  menuToggle?.addEventListener('click', toggleSidebar);
  menuToggleCollapsed?.addEventListener('click', toggleSidebar);
  mobileMenuToggle?.addEventListener('click', toggleMobileSidebar);

  // Check localStorage for sidebar state
  const isSidebarCollapsed = localStorage.getItem('doctorSidebarCollapsed') === 'true';
  if (isSidebarCollapsed) {
    sidebar.classList.add('collapsed');
  }

  // On mobile, always start with sidebar collapsed
  if (window.innerWidth <= 768) {
    sidebar.classList.remove('open');
  }
}

// Toggle sidebar between expanded and collapsed states
function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  
  if (this.classList.contains('menu-toggle')) {
    // Header toggle button clicked (collapse sidebar)
    sidebar.classList.add('collapsed');
    localStorage.setItem('doctorSidebarCollapsed', 'true');
  } else {
    // Collapsed toggle button clicked (expand sidebar)
    sidebar.classList.remove('collapsed');
    localStorage.setItem('doctorSidebarCollapsed', 'false');
  }
}

// Toggle mobile sidebar visibility
function toggleMobileSidebar() {
  const sidebar = document.getElementById('sidebar');
  sidebar.classList.toggle('open');
}

// Initialize dashboard counters with animation
function initializeCounters() {
  const counters = document.querySelectorAll('.count');
  
  counters.forEach(counter => {
    const target = parseInt(counter.innerText);
    const duration = 1000; // Animation duration in milliseconds
    const step = Math.ceil(target / (duration / 50)); // Update every 50ms
    let current = 0;
    
    // Reset counter text before starting animation
    counter.innerText = '0';
    
    const updateCounter = () => {
      current += step;
      if (current >= target) {
        counter.innerText = target;
        clearInterval(interval);
      } else {
        counter.innerText = current;
      }
    };
    
    const interval = setInterval(updateCounter, 50);
  });
}

// Highlight today in the calendar if present
function highlightToday() {
  const todayDate = new Date().getDate();
  const calendarDays = document.querySelectorAll('.calendar-day');
  
  calendarDays.forEach(day => {
    if (parseInt(day.innerText) === todayDate && !day.classList.contains('other-month')) {
      day.classList.add('today');
    }
  });
}

// Handle notification badges
function setupNotifications() {
  const notificationsBadge = document.querySelector('.bell-btn .badge');
  
  if (notificationsBadge) {
    // Check if there are notifications
    const notificationCount = parseInt(notificationsBadge.innerText);
    
    if (notificationCount > 0) {
      // Make the notification badge visible
      notificationsBadge.style.display = 'flex';
    } else {
      // Hide the badge if no notifications
      notificationsBadge.style.display = 'none';
    }
  }
}

// Toggle between light and dark theme (placeholder for future functionality)
function toggleTheme() {
  // This is a placeholder for future dark mode implementation
  document.body.classList.toggle('dark-theme');
  
  // Store user preference
  const isDarkMode = document.body.classList.contains('dark-theme');
  localStorage.setItem('doctorDarkTheme', isDarkMode);
}

// Initialize tabbed content if present
function initializeTabs() {
  const tabButtons = document.querySelectorAll('.tab-button');
  const tabContents = document.querySelectorAll('.tab-content');
  
  if (tabButtons.length > 0) {
    // Set first tab as active by default
    tabButtons[0].classList.add('active');
    tabContents[0].classList.add('active');
    
    // Add click handlers to tab buttons
    tabButtons.forEach((button, index) => {
      button.addEventListener('click', () => {
        // Remove active class from all buttons and contents
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabContents.forEach(content => content.classList.remove('active'));
        
        // Add active class to current button and content
        button.classList.add('active');
        tabContents[index].classList.add('active');
      });
    });
  }
}

// Update appointment status indicators
function updateStatusIndicators() {
  const statusBadges = document.querySelectorAll('.status-badge');
  
  statusBadges.forEach(badge => {
    const status = badge.innerText.toLowerCase();
    
    if (status.includes('pending')) {
      badge.classList.add('status-pending');
    } else if (status.includes('confirmed')) {
      badge.classList.add('status-confirmed');
    } else if (status.includes('cancelled')) {
      badge.classList.add('status-cancelled');
    } else if (status.includes('completed')) {
      badge.classList.add('status-completed');
    }
  });
}

// Handle responsive behavior
function handleResponsive() {
  const sidebar = document.getElementById('sidebar');
  
  if (window.innerWidth <= 768) {
    // On mobile, remove collapsed class and add necessary mobile classes
    sidebar.classList.remove('collapsed');
    
    if (localStorage.getItem('doctorSidebarCollapsed') === 'true') {
      sidebar.classList.remove('open');
    } else {
      sidebar.classList.add('open');
    }
  } else {
    // On desktop, restore saved collapsed state
    sidebar.classList.remove('open');
    
    if (localStorage.getItem('doctorSidebarCollapsed') === 'true') {
      sidebar.classList.add('collapsed');
    } else {
      sidebar.classList.remove('collapsed');
    }
  }
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  initializeSidebar();
  initializeCounters();
  highlightToday();
  setupNotifications();
  initializeTabs();
  updateStatusIndicators();
  handleResponsive();
  
  // Check for saved theme preference
  if (localStorage.getItem('doctorDarkTheme') === 'true') {
    document.body.classList.add('dark-theme');
  }
  
  // Handle window resize
  window.addEventListener('resize', handleResponsive);
});

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', () => {
  initializeSidebar();
  initializeCounters();
  highlightToday();
  updateStatusIndicators();
  initializeTabs();
}); 