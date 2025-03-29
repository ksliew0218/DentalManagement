// Improved dashboard.js with more robust initialization
// Wrap all functionality in a function that can be called on page load and after AJAX content loads
function initializeDashboard() {
  console.log("Initializing dashboard animations...");
  
  // Function to animate progress bar
  function animateProgressBar() {
    const progressBar = document.querySelector('.progress-fill');
    if (!progressBar) {
      console.log("No progress bar found");
      return;
    }

    console.log("Animating progress bar");
    // Get the target width from the inline style or data attribute
    const targetWidth = progressBar.style.width || progressBar.dataset.progress + '%';

    // Reset the width to 0
    progressBar.style.width = '0%';

    // Force a reflow to ensure the animation works
    void progressBar.offsetHeight;

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
    if (!percentageElement) {
      console.log("No percentage element found");
      return;
    }

    console.log("Animating percentage counter");
    // Get the target percentage from the text content
    const text = percentageElement.textContent;
    const targetPercentage = parseInt(text);
    
    if (isNaN(targetPercentage)) {
      console.log("Invalid percentage value:", text);
      return;
    }
    
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

  // Add smooth reveal animation for the entire card
  function addCardRevealAnimation() {
    const treatmentCard = document.querySelector('.ongoing-treatment');
    if (!treatmentCard) {
      console.log("No treatment card found");
      return;
    }

    console.log("Adding card reveal animation");
    treatmentCard.style.opacity = '0';
    treatmentCard.style.transform = 'translateY(20px)';

    setTimeout(() => {
      treatmentCard.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
      treatmentCard.style.opacity = '1';
      treatmentCard.style.transform = 'translateY(0)';
    }, 100);
  }

  // Initialize treatment card animations
  function initializeTreatmentCardAnimations() {
    console.log("Initializing treatment card animations");
    animateProgressBar();
    animatePercentage();
    addCardRevealAnimation();
  }
  
  // Initialize treatment details overlay click handlers
  function initializeTreatmentDetails() {
    console.log("Initializing treatment details");
    const treatmentCards = document.querySelectorAll('.treatment-type-card');
    const overlay = document.getElementById('treatment-details-overlay');
    
    if (!treatmentCards.length || !overlay) {
      console.log("No treatment cards or overlay found");
      return;
    }
    
    // Remove any existing event listeners (to prevent duplicates)
    treatmentCards.forEach(card => {
      const clone = card.cloneNode(true);
      card.parentNode.replaceChild(clone, card);
    });
    
    // Add event listeners to the new cards
    document.querySelectorAll('.treatment-type-card').forEach(card => {
      card.addEventListener('click', function() {
        const treatmentName = this.getAttribute('data-treatment-name');
        console.log("Treatment card clicked:", treatmentName);
        // Add your treatment details logic here
      });
    });
    
    // Add click handler for learn more links
    document.querySelectorAll('.learn-more-link').forEach(link => {
      link.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        const card = this.closest('.treatment-type-card');
        const treatmentName = card.getAttribute('data-treatment-name');
        console.log("Learn more clicked for:", treatmentName);
        // Add your learn more logic here
      });
    });
  }

  // Call all initialization functions
  initializeTreatmentCardAnimations();
  initializeTreatmentDetails();
  
  console.log("Dashboard initialization complete");
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
  console.log("DOM Content Loaded - initializing dashboard");
  initializeDashboard();
});

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', function() {
  console.log("Content Loaded event received - reinitializing dashboard");
  initializeDashboard();
});

// Make function globally available
window.initializeDashboard = initializeDashboard;