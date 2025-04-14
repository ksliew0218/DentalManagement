function initializeDashboard() {
  console.log("Initializing dashboard animations...");
  
  function animateProgressBar() {
    const progressBar = document.querySelector('.progress-fill');
    if (!progressBar) {
      console.log("No progress bar found");
      return;
    }

    console.log("Animating progress bar");
    const targetWidth = progressBar.style.width || progressBar.dataset.progress + '%';

    progressBar.style.width = '0%';

    void progressBar.offsetHeight;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setTimeout(() => {
              progressBar.style.width = targetWidth;
            }, 200);

            observer.disconnect();
          }
        });
      },
      {
        threshold: 0.1,
      }
    );

    observer.observe(progressBar);
  }

  function animatePercentage() {
    const percentageElement = document.querySelector(
      '.completion-status .percentage'
    );
    if (!percentageElement) {
      console.log("No percentage element found");
      return;
    }

    console.log("Animating percentage counter");
    const text = percentageElement.textContent;
    const targetPercentage = parseInt(text);
    
    if (isNaN(targetPercentage)) {
      console.log("Invalid percentage value:", text);
      return;
    }
    
    let currentPercentage = 0;

    percentageElement.textContent = '0%';

    const duration = 1500; 
    const steps = 60; 
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

  function initializeTreatmentCardAnimations() {
    console.log("Initializing treatment card animations");
    animateProgressBar();
    animatePercentage();
    addCardRevealAnimation();
  }
  function initializeTreatmentDetails() {
    console.log("Initializing treatment details");
    const treatmentCards = document.querySelectorAll('.treatment-type-card');
    const overlay = document.getElementById('treatment-details-overlay');
    
    if (!treatmentCards.length || !overlay) {
      console.log("No treatment cards or overlay found");
      return;
    }
    
    treatmentCards.forEach(card => {
      const clone = card.cloneNode(true);
      card.parentNode.replaceChild(clone, card);
    });
    
    document.querySelectorAll('.treatment-type-card').forEach(card => {
      card.addEventListener('click', function() {
        const treatmentName = this.getAttribute('data-treatment-name');
        console.log("Treatment card clicked:", treatmentName);
      });
    });
    
    document.querySelectorAll('.learn-more-link').forEach(link => {
      link.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        const card = this.closest('.treatment-type-card');
        const treatmentName = card.getAttribute('data-treatment-name');
        console.log("Learn more clicked for:", treatmentName);
      });
    });
  }

  initializeTreatmentCardAnimations();
  initializeTreatmentDetails();
  
  console.log("Dashboard initialization complete");
}

document.addEventListener('DOMContentLoaded', function() {
  console.log("DOM Content Loaded - initializing dashboard");
  initializeDashboard();
});

document.addEventListener('contentLoaded', function() {
  console.log("Content Loaded event received - reinitializing dashboard");
  initializeDashboard();
});

window.initializeDashboard = initializeDashboard;