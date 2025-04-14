function createLoadingAnimation() {
  const loadingAnimation = document.querySelector('.loading-animation');
  if (!loadingAnimation) return;
  
  if (loadingAnimation.children.length === 0) {
    console.log("Creating loading animation elements");
    for (let i = 0; i < 10; i++) {
      let parentEl = document.createElement('div');
      parentEl.classList.add('strand');
      parentEl.style.opacity = '0'; 
      parentEl.innerHTML = `
              <div class="top" style="animation-delay:${i * -0.2}s"></div>
              <div class="bottom" style="animation-delay:${
                -1.5 - i * 0.2
              }s"></div>
          `;
      loadingAnimation.appendChild(parentEl);
      void parentEl.offsetWidth;
      parentEl.style.opacity = ''; 
    }
    console.log("Loading animation created with 10 strands");
  }
}

function showLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (!loader) return;
  
  loader.style.display = 'flex';
  void loader.offsetWidth;
  loader.classList.add('show');
  console.log("Loading animation shown");
}

function hideLoadingAnimation() {
  const loader = document.querySelector('.loading-container');
  if (!loader) return;
  
  loader.classList.remove('show');
  setTimeout(() => {
    if (!loader.classList.contains('show')) {
      loader.style.display = 'none';
      console.log("Loading animation hidden");
    }
  }, 500);
}

function initializeLoadingAnimation() {
  console.log("Initializing loading animation");
  createLoadingAnimation();

  if (!window.loadingInitialized) {
    showLoadingAnimation();

    setTimeout(hideLoadingAnimation, 800);
    
    window.loadingInitialized = true;
    console.log("Loading animation initialized");
  }
}

document.addEventListener('DOMContentLoaded', initializeLoadingAnimation);

window.showLoadingAnimation = showLoadingAnimation;
window.hideLoadingAnimation = hideLoadingAnimation;
window.createLoadingAnimation = createLoadingAnimation;

$(document)
  .ajaxStart(function () {
    console.log("AJAX request started - showing loading animation");
    showLoadingAnimation();
  })
  .ajaxStop(function () {
    console.log("AJAX request completed - hiding loading animation");
    hideLoadingAnimation();
  });