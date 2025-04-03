/**
 * Billing page initialization script
 * Following the AJAX Navigation Guidelines
 */

function initializeBillingPage() {
  // Set up alert dismissal with fade out animation
  setupAlerts();

  // Set up tab navigation
  setupTabs();

  // Animate payment cards
  animateCards();

  // Animate counter numbers
  animateCounters();

  // Highlight the Billing menu item in the sidebar
  highlightMenuItem('billing');
}

function setupAlerts() {
  const alerts = document.querySelectorAll('.alert-message');

  // Set up manual dismissal
  alerts.forEach((alert) => {
    const closeBtn = alert.querySelector('.close-alert');
    if (closeBtn) {
      closeBtn.addEventListener('click', function () {
        alert.style.opacity = '0';
        setTimeout(() => {
          alert.style.display = 'none';
        }, 300);
      });
    }
  });

  // Auto-hide alerts after 5 seconds
  if (alerts.length > 0) {
    setTimeout(() => {
      alerts.forEach((alert) => {
        alert.style.opacity = '0';
        setTimeout(() => {
          alert.style.display = 'none';
        }, 300);
      });
    }, 5000);
  }
}

function setupTabs() {
  const tabs = document.querySelectorAll('.filter-tab');

  // Get saved tab from localStorage or use default
  const savedTab =
    localStorage.getItem('billingActiveTab') || 'pending-deposits';

  // Set initial active tab
  setActiveTabById(savedTab);

  // Add click event listeners to tabs
  tabs.forEach((tab) => {
    tab.addEventListener('click', function () {
      const tabId = this.getAttribute('data-tab');

      // Remove active class from all tabs
      tabs.forEach((t) => t.classList.remove('active'));

      // Add active class to current tab
      this.classList.add('active');

      // Show selected tab content
      showTabContent(tabId);

      // Save current tab to localStorage
      localStorage.setItem('billingActiveTab', tabId);
    });
  });
}

function setActiveTabById(tabId) {
  // Find and activate the tab with the given ID
  const tab = document.querySelector(`.filter-tab[data-tab="${tabId}"]`);
  const tabs = document.querySelectorAll('.filter-tab');

  if (tab) {
    // Remove active class from all tabs
    tabs.forEach((t) => t.classList.remove('active'));

    // Add active class to specified tab
    tab.classList.add('active');

    // Show the corresponding tab content
    showTabContent(tabId);
  }
}

function showTabContent(tabId) {
  // Hide all tab contents
  const tabContents = document.querySelectorAll('.tab-content');
  tabContents.forEach((content) => content.classList.remove('active'));

  // Show the selected tab content
  const selectedContent = document.getElementById(`${tabId}-tab`);
  if (selectedContent) {
    selectedContent.classList.add('active');

    // Animate cards in the newly visible tab
    animateCards();
  }
}

function animateCards() {
  // Find cards in currently active tab
  const activeTab = document.querySelector('.tab-content.active');
  if (!activeTab) return;

  const cards = activeTab.querySelectorAll('.payment-card:not(.visible)');

  // Add animation with increasing delay based on index
  cards.forEach((card, index) => {
    card.style.setProperty('--index', index);
    setTimeout(() => {
      card.classList.add('visible');
    }, 50 * index);
  });
}

function animateCounters() {
  const counters = document.querySelectorAll('.summary-count');

  counters.forEach((counter) => {
    const target = parseInt(counter.innerText, 10);
    const duration = 1500; // animation duration in ms
    const frameDuration = 1000 / 60; // for 60fps
    const totalFrames = Math.round(duration / frameDuration);

    // Only animate if there's a positive number
    if (target > 0) {
      let count = 0;
      const increment = target / totalFrames;

      const animate = () => {
        count += increment;
        if (count < target) {
          counter.textContent = Math.floor(count);
          requestAnimationFrame(animate);
        } else {
          counter.textContent = target;
        }
      };

      animate();
    }
  });
}

// Highlight the menu item in the sidebar
function highlightMenuItem(menuItemId) {
  // Remove active class from all menu items
  const menuItems = document.querySelectorAll('.sidebar-nav .nav-link');
  menuItems.forEach((item) => item.classList.remove('active'));

  // Add active class to the current menu item
  const currentItem = document.getElementById(menuItemId);
  if (currentItem) {
    currentItem.classList.add('active');

    // If the item is in a submenu, expand the parent menu
    const parentMenu = currentItem.closest('.submenu');
    if (parentMenu) {
      const parentToggle = document.querySelector(
        `[data-bs-target="#${parentMenu.id}"]`
      );
      if (parentToggle && parentToggle.classList.contains('collapsed')) {
        parentToggle.click();
      }
    }
  }

  // Store the current active menu item in localStorage
  localStorage.setItem('activeMenuItem', menuItemId);
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeBillingPage);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeBillingPage);

// Make functions global for potential HTML event handlers
window.highlightMenuItem = highlightMenuItem;
