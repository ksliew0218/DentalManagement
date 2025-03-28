document.addEventListener('DOMContentLoaded', function () {
  // Initialize variables
  let selectedTreatment = null;

  // Get essential DOM elements
  const filterTabs = document.querySelectorAll('.filter-tab');
  const treatmentCards = document.querySelectorAll('.treatment-card');
  const emptyCategoryCards = document.querySelectorAll('.empty-category-card');
  const nextButton = document.querySelector('.next-btn');
  const antiForgeryToken = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );

  // Debugging function
  function logDebug(message) {
    console.log(`[Treatment Selection Debug] ${message}`);
  }

  filterTabs.forEach((tab) => {
    tab.addEventListener('click', function () {
      // Update active tab
      filterTabs.forEach((t) => t.classList.remove('active'));
      this.classList.add('active');

      const category = this.getAttribute('data-category');

      // For 'all' category, show all treatments and hide all empty messages
      if (category === 'all') {
        treatmentCards.forEach((card) => (card.style.display = 'flex'));
        emptyCategoryCards.forEach((card) => (card.style.display = 'none'));
        return;
      }

      // For specific category, show only that category's treatments and its empty message if needed
      let hasTreatments = false;

      // First hide all treatments and empty messages
      treatmentCards.forEach((card) => (card.style.display = 'none'));
      emptyCategoryCards.forEach((card) => (card.style.display = 'none'));

      // Show treatments for selected category
      treatmentCards.forEach((card) => {
        if (card.getAttribute('data-category') === category) {
          card.style.display = 'flex';
          hasTreatments = true;
        }
      });

      // If no treatments found for this category, show its empty message
      if (!hasTreatments) {
        const emptyCard = document.querySelector(
          `.empty-category-card[data-category="${category}"]`
        );
        if (emptyCard) {
          emptyCard.style.display = 'flex';
        }
      }
    });
  });

  // Handle treatment selection
  const selectButtons = document.querySelectorAll('.select-treatment-btn');

  selectButtons.forEach((button) => {
    button.addEventListener('click', function () {
      // Get the parent treatment card
      const treatmentCard = this.closest('.treatment-card');

      // Remove selection from all cards
      treatmentCards.forEach((card) => {
        card.classList.remove('selected');
        card.querySelector('.select-treatment-btn').textContent = 'Select';
      });

      // Select this card
      treatmentCard.classList.add('selected');
      this.textContent = 'Selected';

      // Store the selected treatment
      selectedTreatment = {
        id: treatmentCard.getAttribute('data-id'),
        name: treatmentCard.querySelector('h3').textContent,
        category: treatmentCard.getAttribute('data-category'),
        duration: treatmentCard.getAttribute('data-duration') + ' min',
        price: 'RM ' + treatmentCard.getAttribute('data-price'),
      };

      // Enable the next button
      nextButton.disabled = false;

      // Smooth scroll to the navigation buttons
      document.querySelector('.navigation-buttons').scrollIntoView({
        behavior: 'smooth',
      });

      // Store in session storage for potential page refresh/back navigation
      sessionStorage.setItem(
        'selectedTreatment',
        JSON.stringify(selectedTreatment)
      );
    });
  });

  // Handle next button click - use a form submission to post data to the server
  // Modify the next button click handler
  nextButton.addEventListener('click', function (event) {
    event.preventDefault();

    console.log('Next button clicked');
    console.log('Selected Treatment:', selectedTreatment);

    // Add more detailed logging
    console.group('Form Submission Details');
    console.log('Anti-Forgery Token:', antiForgeryToken);
    console.log(
      'Anti-Forgery Token Value:',
      antiForgeryToken ? antiForgeryToken.value : 'Not Found'
    );
    console.groupEnd();

    if (selectedTreatment) {
      try {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Patient/Appointments/SaveTreatmentSelection';
        form.style.display = 'none';

        // Enhanced logging for form creation
        console.group('Form Creation');
        console.log('Form Action:', form.action);

        if (antiForgeryToken) {
          const tokenInput = document.createElement('input');
          tokenInput.type = 'hidden';
          tokenInput.name = '__RequestVerificationToken';
          tokenInput.value = antiForgeryToken.value;
          form.appendChild(tokenInput);
          console.log('CSRF Token Added');
        } else {
          console.warn('No Anti-Forgery Token Found');
        }

        const treatmentIdInput = document.createElement('input');
        treatmentIdInput.type = 'hidden';
        treatmentIdInput.name = 'treatmentId';
        treatmentIdInput.value = selectedTreatment.id;
        form.appendChild(treatmentIdInput);
        console.log('Treatment ID Input:', treatmentIdInput.value);

        const redirectInput = document.createElement('input');
        redirectInput.type = 'hidden';
        redirectInput.name = 'redirectUrl';
        redirectInput.value = '/Patient/Appointments/Doctor';
        form.appendChild(redirectInput);
        console.log('Redirect URL:', redirectInput.value);

        console.groupEnd();

        document.body.appendChild(form);

        // Add a global error handler
        window.addEventListener('error', function (event) {
          console.error('Global error caught:', event.error);
        });

        form.submit();
      } catch (error) {
        console.error('Form submission error:', error);
        alert(
          'An error occurred while selecting the treatment. Please try again.'
        );
      }
    } else {
      console.warn('No treatment selected');
      alert('Please select a treatment before proceeding.');
    }
  });

  // Check if there's a previously selected treatment (when navigating back)
  const storedTreatment = sessionStorage.getItem('selectedTreatment');
  if (storedTreatment) {
    try {
      const parsedTreatment = JSON.parse(storedTreatment);

      // Find and select the matching treatment card
      treatmentCards.forEach((card) => {
        if (card.getAttribute('data-id') === parsedTreatment.id.toString()) {
          // Trigger a click on the select button
          card.querySelector('.select-treatment-btn').click();
        }
      });
    } catch (error) {
      console.error('Error parsing stored treatment:', error);
    }
  }

  // Add animation to the progress steps
  function animateProgressSteps() {
    const steps = document.querySelectorAll('.step');
    steps.forEach((step, index) => {
      setTimeout(() => {
        if (index === 0) {
          step.classList.add('active');
        }
      }, index * 200);
    });
  }

  // Initialize animations
  animateProgressSteps();
});
