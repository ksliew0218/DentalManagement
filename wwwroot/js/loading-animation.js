// Create DNA loading animation with proper delays
function createLoadingAnimation() {
  const loadingAnimation = document.querySelector('.loading-animation');
  for (let i = 0; i < 10; i++) {
    let parentEl = document.createElement('div');
    parentEl.classList.add('strand');
    parentEl.style.opacity = '0'; // Start invisible
    parentEl.innerHTML = `
            <div class="top" style="animation-delay:${i * -0.2}s"></div>
            <div class="bottom" style="animation-delay:${
              -1.5 - i * 0.2
            }s"></div>
        `;
    loadingAnimation.appendChild(parentEl);
    // Trigger reflow to ensure smooth animation
    void parentEl.offsetWidth;
    parentEl.style.opacity = ''; // Remove opacity to allow animation
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

// Initialize loading animation
document.addEventListener('DOMContentLoaded', function () {
  createLoadingAnimation();

  // Show loading when page starts loading
  showLoadingAnimation();

  // Hide loading when page is fully loaded
  window.addEventListener('load', function () {
    // Longer delay for smoother transition
    setTimeout(hideLoadingAnimation, 800);
  });

  // Show loading on navigation
  document.addEventListener('click', function (e) {
    if (e.target.tagName === 'A' && !e.target.hasAttribute('data-no-loading')) {
      showLoadingAnimation();
    }
  });
});

// For AJAX requests (if using)
$(document)
  .ajaxStart(function () {
    showLoadingAnimation();
  })
  .ajaxStop(function () {
    hideLoadingAnimation();
  });
