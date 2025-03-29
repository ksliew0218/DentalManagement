// Improved loading animation with better initialization
// Create DNA loading animation with proper delays
function createLoadingAnimation() {
  const loadingAnimation = document.querySelector('.loading-animation');
  if (!loadingAnimation) return;
  
  // Only create the animation if it doesn't already exist
  if (loadingAnimation.children.length === 0) {
    console.log("Creating loading animation elements");
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
    console.log("Loading animation created with 10 strands");
  }
}

function showLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (!loader) return;
  
  loader.style.display = 'flex';
  // Trigger reflow
  void loader.offsetWidth;
  loader.classList.add('show');
  console.log("Loading animation shown");
}

function hideLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (!loader) return;
  
  loader.classList.remove('show');
  // Wait for transition to complete before hiding
  setTimeout(() => {
    if (!loader.classList.contains('show')) {
      loader.style.display = 'none';
      console.log("Loading animation hidden");
    }
  }, 500);
}

// Initialize loading animation
function initializeLoadingAnimation() {
  console.log("Initializing loading animation");
  createLoadingAnimation();

  // Only show on first load, not after AJAX navigation
  if (!window.loadingInitialized) {
    // Show loading when page starts loading
    showLoadingAnimation();

    // Hide loading when page is fully loaded
    setTimeout(hideLoadingAnimation, 800);
    
    // Mark as initialized
    window.loadingInitialized = true;
    console.log("Loading animation initialized");
  }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeLoadingAnimation);

// Make functions globally available
window.showLoadingAnimation = showLoadingAnimation;
window.hideLoadingAnimation = hideLoadingAnimation;
window.createLoadingAnimation = createLoadingAnimation;

// For AJAX requests (jQuery)
$(document)
  .ajaxStart(function () {
    console.log("AJAX request started - showing loading animation");
    showLoadingAnimation();
  })
  .ajaxStop(function () {
    console.log("AJAX request completed - hiding loading animation");
    hideLoadingAnimation();
  });