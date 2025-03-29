// Use direct event delegation for toggle buttons
document.addEventListener('click', function(e) {
  // More precise check to handle icon clicks
  const isExpandButton = e.target.matches('.menu-toggle, .menu-toggle *') || 
                         e.target.closest('.menu-toggle');
                         
  const isCollapseButton = e.target.matches('.menu-toggle-collapsed, .menu-toggle-collapsed *') || 
                           e.target.closest('.menu-toggle-collapsed');
  
  if (isExpandButton) {
    console.log("Expand button clicked");
    toggleSidebar('collapse');
  } else if (isCollapseButton) {
    console.log("Collapse button clicked");
    toggleSidebar('expand');
  }
});

// More direct approach for toggle functionality
function toggleSidebar(action) {
  const sidebar = document.getElementById('sidebar');
  
  if (!sidebar) {
    console.error("Sidebar element not found");
    return;
  }
  
  if (action === 'collapse') {
    console.log("Collapsing sidebar");
    sidebar.classList.add('collapsed');
    localStorage.setItem('sidebarCollapsed', 'true');
    
    // Ensure main content margin is updated
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
      mainContent.style.marginLeft = '80px';
      mainContent.style.width = 'calc(100% - 80px)';
    }
  } else if (action === 'expand') {
    console.log("Expanding sidebar");
    sidebar.classList.remove('collapsed');
    localStorage.setItem('sidebarCollapsed', 'false');
    
    // Ensure main content margin is updated
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
      mainContent.style.marginLeft = '280px';
      mainContent.style.width = 'calc(100% - 280px)';
    }
  }
}

// Also add direct event listeners as a fallback
function attachDirectToggleEvents() {
  console.log("Attaching direct toggle events");
  
  const expandButton = document.querySelector('.menu-toggle');
  if (expandButton) {
    expandButton.addEventListener('click', function() {
      console.log("Direct expand button clicked");
      toggleSidebar('collapse');
    });
  } else {
    console.warn("Expand button not found");
  }
  
  const collapseButton = document.querySelector('.menu-toggle-collapsed');
  if (collapseButton) {
    collapseButton.addEventListener('click', function() {
      console.log("Direct collapse button clicked");
      toggleSidebar('expand');
    });
  } else {
    console.warn("Collapse button not found");
  }
}

// Initialize sidebar state
function initializeSidebar() {
  console.log("Initializing sidebar");
  
  const sidebar = document.getElementById('sidebar');
  if (!sidebar) {
    console.error("Sidebar not found during initialization");
    return;
  }
  
  // Check localStorage for sidebar state
  const isSidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
  console.log("Sidebar collapsed state:", isSidebarCollapsed);
  
  if (isSidebarCollapsed) {
    sidebar.classList.add('collapsed');
    // Update main content margins
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
      mainContent.style.marginLeft = '80px';
      mainContent.style.width = 'calc(100% - 80px)';
    }
  } else {
    sidebar.classList.remove('collapsed');
    // Update main content margins
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
      mainContent.style.marginLeft = '280px';
      mainContent.style.width = 'calc(100% - 280px)';
    }
  }
  
  // Attach direct event listeners
  attachDirectToggleEvents();
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
  console.log("DOM Content Loaded - Initializing sidebar");
  initializeSidebar();
});

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', function() {
  console.log("Content Loaded - Reinitializing sidebar");
  initializeSidebar();
});

// Run immediately (self-executing function)
(function() {
  setTimeout(function() {
    console.log("Self-executing init - Initializing sidebar");
    initializeSidebar();
  }, 200);
})();

// Make toggle function globally available
window.toggleSidebar = toggleSidebar;