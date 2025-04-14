function initializeBillingPage() {
  setupAlerts();

  setupTabs();

  animateCards();

  animateCounters();

  highlightMenuItem('billing');
}

function setupAlerts() {
  const alerts = document.querySelectorAll('.alert-message');

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

  const savedTab =
    localStorage.getItem('billingActiveTab') || 'pending-deposits';

  setActiveTabById(savedTab);

  tabs.forEach((tab) => {
    tab.addEventListener('click', function () {
      const tabId = this.getAttribute('data-tab');

      tabs.forEach((t) => t.classList.remove('active'));

      this.classList.add('active');

      showTabContent(tabId);

      localStorage.setItem('billingActiveTab', tabId);
    });
  });
}

function setActiveTabById(tabId) {
  const tab = document.querySelector(`.filter-tab[data-tab="${tabId}"]`);
  const tabs = document.querySelectorAll('.filter-tab');

  if (tab) {
    tabs.forEach((t) => t.classList.remove('active'));

    tab.classList.add('active');

    showTabContent(tabId);
  }
}

function showTabContent(tabId) {
  const tabContents = document.querySelectorAll('.tab-content');
  tabContents.forEach((content) => content.classList.remove('active'));

  const selectedContent = document.getElementById(`${tabId}-tab`);
  if (selectedContent) {
    selectedContent.classList.add('active');

    animateCards();
  }
}

function animateCards() {
  const activeTab = document.querySelector('.tab-content.active');
  if (!activeTab) return;

  const cards = activeTab.querySelectorAll('.payment-card:not(.visible)');

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
    const duration = 1500; 
    const frameDuration = 1000 / 60; 
    const totalFrames = Math.round(duration / frameDuration);

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

function highlightMenuItem(menuItemId) {
  const menuItems = document.querySelectorAll('.sidebar-nav .nav-link');
  menuItems.forEach((item) => item.classList.remove('active'));

  const currentItem = document.getElementById(menuItemId);
  if (currentItem) {
    currentItem.classList.add('active');

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

  localStorage.setItem('activeMenuItem', menuItemId);
}
document.addEventListener('DOMContentLoaded', initializeBillingPage);

document.addEventListener('contentLoaded', initializeBillingPage);

window.highlightMenuItem = highlightMenuItem;
