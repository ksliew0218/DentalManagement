function updateActiveMenu(url = window.location.pathname) {
  localStorage.setItem('currentActivePath', url);
  
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    const linkUrl = new URL(
      link.getAttribute('data-url'),
      window.location.origin
    ).pathname;

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

      if (typeof initializeLayout === 'function') {
        initializeLayout();
      }
      
      if (typeof handleResponsiveLayout === 'function') {
        handleResponsiveLayout();
      }

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
    link.removeEventListener('click', handleNavClick);
    
    link.addEventListener('click', handleNavClick);
  });
}

function handleNavClick(e) {
  if (this.classList.contains('no-ajax')) {
    return true;
  }
  loadContent(e, this);
}

function restoreActiveMenuHighlight() {
  const currentPath = window.location.pathname;
  const savedActivePath = localStorage.getItem('currentActivePath');
  
  let matchFound = highlightMatchingMenuItem(currentPath);
  
  if (!matchFound && savedActivePath && savedActivePath !== currentPath) {
    highlightMatchingMenuItem(savedActivePath);
  }
}

function highlightMatchingMenuItem(path) {
  let matchFound = false;
  
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    const linkUrl = new URL(
      link.getAttribute('data-url'),
      window.location.origin
    ).pathname;
    
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

window.addEventListener('popstate', function(event) {
  const currentPath = window.location.pathname;
  
  updateActiveMenu(currentPath);
  
  if (event.state && event.state.url) {
    loadContent(null, null, false);
  }
});

document.addEventListener('DOMContentLoaded', function() {
  initializeNavigation();
  restoreActiveMenuHighlight();
});

document.addEventListener('contentLoaded', function() {
  initializeNavigation();
});

window.loadContent = loadContent;
window.updateActiveMenu = updateActiveMenu;
window.initializeNavigation = initializeNavigation;
window.restoreActiveMenuHighlight = restoreActiveMenuHighlight;