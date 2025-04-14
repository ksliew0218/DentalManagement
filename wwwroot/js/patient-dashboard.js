function initializeLayout() {
  const contentContainer = document.getElementById('contentContainer');
  
  if (contentContainer) {
    contentContainer.style.marginLeft = '280px';
    contentContainer.style.width = 'calc(100% - 280px)';
  }
}

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

document.addEventListener('DOMContentLoaded', function() {
  initializeLayout();
  handleResponsiveLayout();
  window.addEventListener('resize', handleResponsiveLayout);
});

document.addEventListener('contentLoaded', function() {
  initializeLayout();
  handleResponsiveLayout();
});

window.initializeLayout = initializeLayout;
window.handleResponsiveLayout = handleResponsiveLayout;