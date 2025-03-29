function updateActiveMenu(url = window.location.pathname) {
  // Store the current path in localStorage for persistence across page refreshes
  localStorage.setItem('currentActivePath', url);
  
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    const linkUrl = new URL(
      link.getAttribute('data-url'),
      window.location.origin
    ).pathname;

    // Check if the current URL starts with the menu item URL (for subpages)
    // Or if it matches exactly
    if (url === linkUrl || url.startsWith(linkUrl + '/')) {
      link.classList.add('active');
      link.parentElement.classList.add('active'); 
    } else {
      link.classList.remove('active');
      link.parentElement.classList.remove('active');
    }
  });
}

function loadContent(event, element, pushState = true) {
  if (event) event.preventDefault();
  showLoadingAnimation();

  const url = element
    ? element.getAttribute('data-url')
    : window.location.pathname;

  $.ajax({
    url: url,
    type: 'GET',
    success: function (response) {
      $('#contentContainer').html(response);
      updateActiveMenu(url);

      if (pushState) {
        window.history.pushState({ url }, '', url);
      }

      // Initialize layout after content is loaded
      if (typeof initializeLayout === 'function') {
        initializeLayout();
      }
      
      if (typeof handleResponsiveLayout === 'function') {
        handleResponsiveLayout();
      }

      // Trigger content loaded event
      const contentLoadedEvent = new Event('contentLoaded');
      document.dispatchEvent(contentLoadedEvent);
    },
    error: function (xhr, status, error) {
      console.error('Error loading content:', error);
      $('#contentContainer').html(
        '<div class="alert alert-danger">Error loading content. Please try again.</div>'
      );
    },
    complete: function () {
      hideLoadingAnimation();
    },
  });
}

function initializeNavigation() {
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    // Remove existing click handlers first
    link.removeEventListener('click', handleNavClick);
    
    // Add new click handler
    link.addEventListener('click', handleNavClick);
  });
}

function handleNavClick(e) {
  // Skip AJAX loading for links with 'no-ajax' class
  if (this.classList.contains('no-ajax')) {
    return true;
  }
  loadContent(e, this);
}

// Restore active menu highlighting from localStorage
function restoreActiveMenuHighlight() {
  const currentPath = window.location.pathname;
  const savedActivePath = localStorage.getItem('currentActivePath');
  
  // First check if current URL matches any menu item directly
  let matchFound = highlightMatchingMenuItem(currentPath);
  
  // If no match with current path, try with saved path
  if (!matchFound && savedActivePath && savedActivePath !== currentPath) {
    highlightMatchingMenuItem(savedActivePath);
  }
}

// Helper function to find and highlight the matching menu item
function highlightMatchingMenuItem(path) {
  let matchFound = false;
  
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    const linkUrl = new URL(
      link.getAttribute('data-url'),
      window.location.origin
    ).pathname;
    
    // Check if the path matches the menu item URL or if it's a subpage
    if (path === linkUrl || path.startsWith(linkUrl + '/')) {
      link.classList.add('active');
      link.parentElement.classList.add('active');
      matchFound = true;
    } else {
      link.classList.remove('active');
      link.parentElement.classList.remove('active');
    }
  });
  
  return matchFound;
}

// Handle browser back/forward navigation
window.addEventListener('popstate', function(event) {
  const currentPath = window.location.pathname;
  
  // Update the active menu based on the current path
  updateActiveMenu(currentPath);
  
  if (event.state && event.state.url) {
    loadContent(null, null, false);
  }
});

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
  initializeNavigation();
  restoreActiveMenuHighlight();
});

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', function() {
  initializeNavigation();
});

// Make functions available globally
window.loadContent = loadContent;
window.updateActiveMenu = updateActiveMenu;
window.initializeNavigation = initializeNavigation;
window.restoreActiveMenuHighlight = restoreActiveMenuHighlight;