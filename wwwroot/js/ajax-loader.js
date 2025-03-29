// Use event delegation for navigation links to ensure they work after AJAX loads
function updateActiveMenu(url = window.location.pathname) {
  // Normalize URL for better matching
  url = url.toLowerCase();
  
  console.log("Updating active menu for URL:", url);
  
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    // Get base URL from data-url attribute
    const dataUrl = link.getAttribute('data-url');
    if (!dataUrl) return;
    
    const linkUrl = new URL(dataUrl, window.location.origin).pathname.toLowerCase();
    
    // Extract parts to match controller
    const linkParts = linkUrl.split('/').filter(Boolean);
    const urlParts = url.split('/').filter(Boolean);
    
    // Check if controller matches (usually the second part of the URL)
    const isMatch = linkParts.length >= 2 && urlParts.length >= 2 && 
                    linkParts[0] === urlParts[0] && // Area matches (Patient)
                    linkParts[1] === urlParts[1];   // Controller matches (Appointments, Dashboard, etc.)
    
    if (isMatch) {
      console.log("Match found for:", linkUrl);
      link.classList.add('active');
      link.parentElement.classList.add('active');
      
      // Store in localStorage for persistence
      localStorage.setItem('activeNavUrl', linkUrl);
    } else {
      link.classList.remove('active');
      link.parentElement.classList.remove('active');
    }
  });
  
  // If no match was found, try to restore from localStorage
  const activeItems = document.querySelectorAll('.nav-menu .nav-link.active');
  if (activeItems.length === 0) {
    const storedActiveUrl = localStorage.getItem('activeNavUrl');
    if (storedActiveUrl) {
      document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
        const dataUrl = link.getAttribute('data-url');
        if (!dataUrl) return;
        
        const linkUrl = new URL(dataUrl, window.location.origin).pathname.toLowerCase();
        if (linkUrl === storedActiveUrl) {
          link.classList.add('active');
          link.parentElement.classList.add('active');
        }
      });
    }
  }
}

function showLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (loader) {
    loader.style.display = 'flex';
    // Trigger reflow
    void loader.offsetWidth;
    loader.classList.add('show');
  }
}

function hideLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (loader) {
    loader.classList.remove('show');
    // Wait for transition to complete before hiding
    setTimeout(() => {
      if (!loader.classList.contains('show')) {
        loader.style.display = 'none';
      }
    }, 500);
  }
}

function loadContent(event, element, pushState = true) {
  if (event) event.preventDefault();
  
  // Show loading animation
  showLoadingAnimation();

  const url = element
    ? element.getAttribute('data-url')
    : window.location.pathname;

  $.ajax({
    url: url,
    type: 'GET',
    success: function (response) {
      // Update content
      document.getElementById('contentContainer').innerHTML = `<main class="main-content">${response}</main>`;
      
      // Update active menu
      updateActiveMenu(url);

      if (pushState) {
        window.history.pushState({ url }, '', url);
      }

      // Trigger content loaded event
      const contentLoadedEvent = new Event('contentLoaded');
      document.dispatchEvent(contentLoadedEvent);
    },
    error: function (xhr, status, error) {
      console.error('Error loading content:', error);
      document.getElementById('contentContainer').innerHTML = 
        '<main class="main-content"><div class="alert alert-danger">Error loading content. Please try again.</div></main>';
    },
    complete: function () {
      // Hide loading animation
      hideLoadingAnimation();
    },
  });
}

// Use event delegation to handle navigation clicks
document.addEventListener('click', function(e) {
  // Find closest .nav-link to handle clicks on icons inside the links
  const navLink = e.target.closest('.nav-menu .nav-link');
  
  if (navLink) {
    // Skip AJAX loading for links with 'no-ajax' class
    if (!navLink.classList.contains('no-ajax')) {
      loadContent(e, navLink);
    }
  }
});

// Handle browser back/forward navigation
window.addEventListener('popstate', function (event) {
  if (event.state && event.state.url) {
    loadContent(null, event.state.url, false);
  } else {
    updateActiveMenu();
  }
});

// Initialize on page first load
document.addEventListener('DOMContentLoaded', function() {
  console.log("DOM Content Loaded - Initializing menu");
  updateActiveMenu();
});

// Run immediately (self-executing function)
(function() {
  // Delay just slightly to ensure DOM is ready
  setTimeout(function() {
    console.log("Self-executing init - Updating menu");
    updateActiveMenu();
  }, 100);
})();

// Also run when window is fully loaded
window.addEventListener('load', function() {
  console.log("Window loaded - Updating menu");
  updateActiveMenu();
});

// Make functions globally available
window.loadContent = loadContent;
window.updateActiveMenu = updateActiveMenu;
window.showLoadingAnimation = showLoadingAnimation;
window.hideLoadingAnimation = hideLoadingAnimation;