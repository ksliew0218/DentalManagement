// Wrap all functionality in a function that can be called on page load and after AJAX content loads
function initializeDashboard() {
  // Function to animate progress bar
  function animateProgressBar() {
    const progressBar = document.querySelector('.progress-fill');
    if (!progressBar) return;

    // Get the target width from the inline style or data attribute
    const targetWidth = progressBar.style.width;

    // Reset the width to 0
    progressBar.style.width = '0%';

    // Force a reflow to ensure the animation works
    progressBar.offsetHeight;

    // Set up an intersection observer to start animation when visible
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            // Add a small delay before starting the animation
            setTimeout(() => {
              progressBar.style.width = targetWidth;
            }, 200);

            // Disconnect the observer after animation starts
            observer.disconnect();
          }
        });
      },
      {
        threshold: 0.1,
      }
    );

    // Start observing the progress bar
    observer.observe(progressBar);
  }

  // Function to animate percentage counter
  function animatePercentage() {
    const percentageElement = document.querySelector(
      '.completion-status .percentage'
    );
    if (!percentageElement) return;

    // Get the target percentage from the text content
    const targetPercentage = parseInt(percentageElement.textContent);
    let currentPercentage = 0;

    // Reset the percentage to 0
    percentageElement.textContent = '0%';

    // Animate the percentage counter
    const duration = 1500; // Match this with the CSS transition duration
    const steps = 60; // Number of steps in the animation
    const increment = targetPercentage / steps;
    const stepDuration = duration / steps;

    const animation = setInterval(() => {
      currentPercentage = Math.min(
        currentPercentage + increment,
        targetPercentage
      );
      percentageElement.textContent = `${Math.round(currentPercentage)}%`;

      if (currentPercentage >= targetPercentage) {
        clearInterval(animation);
      }
    }, stepDuration);
  }

  // Initialize animations
  function initializeAnimations() {
    animateProgressBar();
    animatePercentage();
  }

  // Add smooth reveal animation for the entire card
  function addCardRevealAnimation() {
    const treatmentCard = document.querySelector('.ongoing-treatment');
    if (!treatmentCard) return;

    treatmentCard.style.opacity = '0';
    treatmentCard.style.transform = 'translateY(20px)';

    setTimeout(() => {
      treatmentCard.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
      treatmentCard.style.opacity = '1';
      treatmentCard.style.transform = 'translateY(0)';
    }, 100);
  }

  // Initialize all animations
  initializeAnimations();
  addCardRevealAnimation();
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeDashboard);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeDashboard);