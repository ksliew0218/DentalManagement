function initializeSidebar() {
  const sidebar = document.getElementById('sidebar');
  const menuToggle = document.querySelector('.menu-toggle');
  const menuToggleCollapsed = document.querySelector('.menu-toggle-collapsed');

  // Remove existing event listeners if any
  menuToggle?.removeEventListener('click', toggleSidebar);
  menuToggleCollapsed?.removeEventListener('click', toggleSidebar);

  // Add new event listeners
  menuToggle?.addEventListener('click', toggleSidebar);
  menuToggleCollapsed?.addEventListener('click', toggleSidebar);

  // Check localStorage for sidebar state
  const isSidebarCollapsed =
    localStorage.getItem('sidebarCollapsed') === 'true';
  if (isSidebarCollapsed) {
    sidebar.classList.add('collapsed');
  }
}

function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  if (this.classList.contains('menu-toggle')) {
    // Header toggle button clicked
    sidebar.classList.add('collapsed');
    localStorage.setItem('sidebarCollapsed', 'true');
  } else {
    // Collapsed toggle button clicked
    sidebar.classList.remove('collapsed');
    localStorage.setItem('sidebarCollapsed', 'false');
  }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeSidebar);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeSidebar);
