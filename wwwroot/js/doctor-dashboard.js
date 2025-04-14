function initializeSidebar() {
  const sidebar = document.getElementById('sidebar');
  const menuToggle = document.querySelector('.menu-toggle');
  const menuToggleCollapsed = document.querySelector('.menu-toggle-collapsed');
  const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');

  menuToggle?.removeEventListener('click', toggleSidebar);
  menuToggleCollapsed?.removeEventListener('click', toggleSidebar);
  mobileMenuToggle?.removeEventListener('click', toggleMobileSidebar);

  menuToggle?.addEventListener('click', toggleSidebar);
  menuToggleCollapsed?.addEventListener('click', toggleSidebar);
  mobileMenuToggle?.addEventListener('click', toggleMobileSidebar);

  const isSidebarCollapsed = localStorage.getItem('doctorSidebarCollapsed') === 'true';
  if (isSidebarCollapsed) {
    sidebar.classList.add('collapsed');
  }

  if (window.innerWidth <= 768) {
    sidebar.classList.remove('open');
  }
}

function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  
  if (this.classList.contains('menu-toggle')) {
    sidebar.classList.add('collapsed');
    localStorage.setItem('doctorSidebarCollapsed', 'true');
  } else {
    sidebar.classList.remove('collapsed');
    localStorage.setItem('doctorSidebarCollapsed', 'false');
  }
}

function toggleMobileSidebar() {
  const sidebar = document.getElementById('sidebar');
  sidebar.classList.toggle('open');
}

function initializeCounters() {
  const counters = document.querySelectorAll('.count');
  
  counters.forEach(counter => {
    const target = parseInt(counter.innerText);
    const duration = 1000; 
    const step = Math.ceil(target / (duration / 50)); 
    let current = 0;
    
    counter.innerText = '0';
    
    const updateCounter = () => {
      current += step;
      if (current >= target) {
        counter.innerText = target;
        clearInterval(interval);
      } else {
        counter.innerText = current;
      }
    };
    
    const interval = setInterval(updateCounter, 50);
  });
}

function highlightToday() {
  const todayDate = new Date().getDate();
  const calendarDays = document.querySelectorAll('.calendar-day');
  
  calendarDays.forEach(day => {
    if (parseInt(day.innerText) === todayDate && !day.classList.contains('other-month')) {
      day.classList.add('today');
    }
  });
}

function setupNotifications() {
  const notificationsBadge = document.querySelector('.bell-btn .badge');
  
  if (notificationsBadge) {
    const notificationCount = parseInt(notificationsBadge.innerText);
    
    if (notificationCount > 0) {
      notificationsBadge.style.display = 'flex';
    } else {
      notificationsBadge.style.display = 'none';
    }
  }
}

function toggleTheme() {
  document.body.classList.toggle('dark-theme');
  
  const isDarkMode = document.body.classList.contains('dark-theme');
  localStorage.setItem('doctorDarkTheme', isDarkMode);
}

function initializeTabs() {
  const tabButtons = document.querySelectorAll('.tab-button');
  const tabContents = document.querySelectorAll('.tab-content');
  
  if (tabButtons.length > 0) {
    tabButtons[0].classList.add('active');
    tabContents[0].classList.add('active');
    
    tabButtons.forEach((button, index) => {
      button.addEventListener('click', () => {
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabContents.forEach(content => content.classList.remove('active'));
        
        button.classList.add('active');
        tabContents[index].classList.add('active');
      });
    });
  }
}

function updateStatusIndicators() {
  const statusBadges = document.querySelectorAll('.status-badge');
  
  statusBadges.forEach(badge => {
    const status = badge.innerText.toLowerCase();
    
    if (status.includes('pending')) {
      badge.classList.add('status-pending');
    } else if (status.includes('confirmed')) {
      badge.classList.add('status-confirmed');
    } else if (status.includes('cancelled')) {
      badge.classList.add('status-cancelled');
    } else if (status.includes('completed')) {
      badge.classList.add('status-completed');
    }
  });
}

function handleResponsive() {
  const sidebar = document.getElementById('sidebar');
  
  if (window.innerWidth <= 768) {
    sidebar.classList.remove('collapsed');
    
    if (localStorage.getItem('doctorSidebarCollapsed') === 'true') {
      sidebar.classList.remove('open');
    } else {
      sidebar.classList.add('open');
    }
  } else {
    sidebar.classList.remove('open');
    
    if (localStorage.getItem('doctorSidebarCollapsed') === 'true') {
      sidebar.classList.add('collapsed');
    } else {
      sidebar.classList.remove('collapsed');
    }
  }
}

document.addEventListener('DOMContentLoaded', () => {
  initializeSidebar();
  initializeCounters();
  highlightToday();
  setupNotifications();
  initializeTabs();
  updateStatusIndicators();
  handleResponsive();
  
  if (localStorage.getItem('doctorDarkTheme') === 'true') {
    document.body.classList.add('dark-theme');
  }
  
  window.addEventListener('resize', handleResponsive);
});

document.addEventListener('contentLoaded', () => {
  initializeSidebar();
  initializeCounters();
  highlightToday();
  updateStatusIndicators();
  initializeTabs();
}); 