// Initialize content container margins
function initializeLayout() {
  const contentContainer = document.getElementById('contentContainer');
  
  if (contentContainer) {
    contentContainer.style.marginLeft = '280px';
    contentContainer.style.width = 'calc(100% - 280px)';
  }
}

// Set responsive layout for mobile devices
function handleResponsiveLayout() {
  const contentContainer = document.getElementById('contentContainer');
  
  if (window.innerWidth <= 768) {
    if (contentContainer) {
      contentContainer.style.marginLeft = '0';
      contentContainer.style.width = '100%';
    }
  } else {
    if (contentContainer) {
      contentContainer.style.marginLeft = '280px';
      contentContainer.style.width = 'calc(100% - 280px)';
    }
  }
}

// Initialize the layout on page load
document.addEventListener('DOMContentLoaded', function() {
  initializeLayout();
  handleResponsiveLayout();
  
  // Add window resize listener for responsive layout
  window.addEventListener('resize', handleResponsiveLayout);
});

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', function() {
  initializeLayout();
  handleResponsiveLayout();
});

// Make functions available globally
window.initializeLayout = initializeLayout;
window.handleResponsiveLayout = handleResponsiveLayout;