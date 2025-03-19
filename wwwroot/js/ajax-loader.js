document.querySelector('.nav-menu').addEventListener('click', function (event) {
  const link = event.target.closest('.nav-link');
  if (link) {
    loadContent(event, link);
  }
});

window.addEventListener('popstate', function (event) {
  if (event.state && event.state.url) {
    loadContent(null, null, false);
  }
});

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

      // Dispatch custom event after content loads
      document.dispatchEvent(new Event('contentLoaded'));
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

function updateActiveMenu(url = window.location.pathname) {
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    const linkUrl = new URL(
      link.getAttribute('data-url'),
      window.location.origin
    ).pathname;

    if (linkUrl === url) {
      link.classList.add('active');
      link.parentElement.classList.add('active'); 
    } else {
      link.classList.remove('active');
      link.parentElement.classList.remove('active');
    }
  });

  console.log('🔹 Active menu updated:', url);
}

// Update the click handler in your JavaScript
document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
  link.addEventListener('click', function (e) {
    // Skip AJAX loading for links with 'no-ajax' class
    if (this.classList.contains('no-ajax')) {
      return true; // Allow normal navigation
    }
    loadContent(e, this);
  });
});

// Initialize click handlers
function initializeNavigation() {
  document.querySelectorAll('.nav-menu .nav-link').forEach((link) => {
    // Remove existing click handlers
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

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeNavigation);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeNavigation);
