document.addEventListener('DOMContentLoaded', function () {
  const menuItems = document.querySelectorAll('.nav-menu li a');
  const sidebar = document.getElementById('sidebar');
  const menuToggle = document.querySelector('.menu-toggle');
  const menuToggleCollapsed = document.querySelector('.menu-toggle-collapsed');

  menuToggle.addEventListener('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    sidebar.classList.add('collapsed');
  });

  menuToggleCollapsed.addEventListener('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    sidebar.classList.remove('collapsed');
  });

  const currentPath = window.location.pathname;

  menuItems.forEach((item) => {
    item.addEventListener('click', function (e) {
      if (e.target === menuToggle || e.target === menuToggle.querySelector('i')) {
        return;
      }
      menuItems.forEach((i) => i.parentElement.classList.remove('active'));
      this.parentElement.classList.add('active');
    });

    if (item.getAttribute('href') === currentPath) {
      menuItems.forEach((i) => i.parentElement.classList.remove('active'));
      item.parentElement.classList.add('active');
    }
  });
});
