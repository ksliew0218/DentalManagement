// treatment-details.js - With Profile Picture Support
document.addEventListener('DOMContentLoaded', () => {
  // Get all Learn More links
  const treatmentCards = document.querySelectorAll('.learn-more-link');
  const treatmentDetailsOverlay = document.getElementById(
    'treatment-details-overlay'
  );
  const treatmentDetailsContent = document.getElementById(
    'treatment-details-content'
  );

  // Check if elements exist
  if (!treatmentDetailsOverlay || !treatmentDetailsContent) {
    console.error('Treatment details overlay or content not found in the DOM');
    return;
  }

  // Close the treatment details overlay
  function closeTreatmentDetails() {
    treatmentDetailsOverlay.classList.remove('is-visible');
    document.body.classList.remove('no-scroll');
  }

  // Show treatment details
  function showTreatmentDetails() {
    treatmentDetailsOverlay.classList.add('is-visible');
    document.body.classList.add('no-scroll');
  }

  // Fetch treatment details with timeout
  async function fetchTreatmentDetails(treatmentName) {
    // Create a timeout promise
    const timeoutPromise = new Promise((_, reject) =>
      setTimeout(() => reject(new Error('Request timed out')), 10000)
    );

    try {
      // Race between fetch and timeout
      const response = await Promise.race([
        fetch(
          `/Patient/Treatments/GetTreatmentDetails?treatmentName=${encodeURIComponent(
            treatmentName
          )}`,
          {
            method: 'GET',
            headers: {
              Accept: 'application/json',
            },
          }
        ),
        timeoutPromise,
      ]);

      // Check if response is ok
      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(
          errorData.message || 'Failed to fetch treatment details'
        );
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching treatment details:', error);
      throw error;
    }
  }

  // Show error state
  function showErrorState(message) {
    // Hide loading animation
    if (typeof hideLoadingAnimation === 'function') {
      hideLoadingAnimation();
    }

    // Show the overlay with error content
    showTreatmentDetails();

    treatmentDetailsContent.innerHTML = `
            <button id="close-treatment-details">&times;</button>
            <div class="error-container">
                <i class="bi bi-exclamation-triangle-fill"></i>
                <h3>Unable to Fetch Treatment Details</h3>
                <p>${message}</p>
                <button class="action-btn close-btn" id="close-treatment-details-bottom">Close</button>
            </div>
        `;

    // Add close button event listeners
    const closeTopBtn = document.getElementById('close-treatment-details');
    const closeBottomBtn = document.getElementById(
      'close-treatment-details-bottom'
    );
    if (closeTopBtn)
      closeTopBtn.addEventListener('click', closeTreatmentDetails);
    if (closeBottomBtn)
      closeBottomBtn.addEventListener('click', closeTreatmentDetails);
  }

  // Get treatment image URL
  function getTreatmentImageUrl(treatmentDetails) {
    return treatmentDetails.imageUrl || null;
  }

  // Helper function to get treatment icon based on name
  function getTreatmentIcon(treatmentName) {
    const treatmentName_lower = treatmentName.toLowerCase();

    // Cleaning related treatments
    if (
      treatmentName_lower.includes('cleaning') ||
      treatmentName_lower.includes('scaling') ||
      treatmentName_lower.includes('polish')
    ) {
      return 'bi bi-brush';
    }

    // Whitening related treatments
    if (
      treatmentName_lower.includes('whitening') ||
      treatmentName_lower.includes('bleach')
    ) {
      return 'bi bi-brightness-high';
    }

    // Extraction related treatments
    if (
      treatmentName_lower.includes('extraction') ||
      treatmentName_lower.includes('removal') ||
      treatmentName_lower.includes('wisdom')
    ) {
      return 'bi bi-scissors';
    }

    // Root canal related treatments
    if (
      treatmentName_lower.includes('root canal') ||
      treatmentName_lower.includes('endodontic') ||
      treatmentName_lower.includes('pulp')
    ) {
      return 'bi bi-tools';
    }

    // Crown related treatments
    if (
      treatmentName_lower.includes('crown') ||
      treatmentName_lower.includes('cap')
    ) {
      return 'bi bi-gem';
    }

    // Filling related treatments
    if (
      treatmentName_lower.includes('filling') ||
      treatmentName_lower.includes('restoration') ||
      treatmentName_lower.includes('composite') ||
      treatmentName_lower.includes('amalgam')
    ) {
      return 'bi bi-pencil-fill';
    }

    // Implant related treatments
    if (treatmentName_lower.includes('implant')) {
      return 'bi bi-nut';
    }

    // Orthodontic treatments
    if (
      treatmentName_lower.includes('braces') ||
      treatmentName_lower.includes('aligner') ||
      treatmentName_lower.includes('invisalign') ||
      treatmentName_lower.includes('orthodontic')
    ) {
      return 'bi bi-layout-three-columns';
    }

    // Dentures and prosthetics
    if (
      treatmentName_lower.includes('denture') ||
      treatmentName_lower.includes('prosthetic')
    ) {
      return 'bi bi-emoji-smile';
    }

    // X-ray and diagnostics
    if (
      treatmentName_lower.includes('x-ray') ||
      treatmentName_lower.includes('radiograph') ||
      treatmentName_lower.includes('scan') ||
      treatmentName_lower.includes('diagnostic')
    ) {
      return 'bi bi-camera';
    }

    // Surgical procedures
    if (
      treatmentName_lower.includes('surgery') ||
      treatmentName_lower.includes('surgical')
    ) {
      return 'bi bi-scissors';
    }

    // Periodontal treatments
    if (
      treatmentName_lower.includes('gum') ||
      treatmentName_lower.includes('periodontal')
    ) {
      return 'bi bi-heart-pulse';
    }

    // Tooth related general
    if (
      treatmentName_lower.includes('tooth') ||
      treatmentName_lower.includes('dental')
    ) {
      return 'bi bi-square';
    }

    // Default icon for any other treatment
    return 'bi bi-plus-circle';
  }

  // Render treatment details with image on left side
  function renderTreatmentDetails(treatmentDetails, treatmentName) {
    // Hide loading animation
    if (typeof hideLoadingAnimation === 'function') {
      hideLoadingAnimation();
    }

    // Show the overlay
    showTreatmentDetails();

    // Get treatment image URL or use icon placeholder
    const treatmentImageUrl = getTreatmentImageUrl(treatmentDetails);

    // Prepare doctors list HTML with profile picture support
    const doctorsHtml =
      treatmentDetails.doctors && treatmentDetails.doctors.length > 0
        ? treatmentDetails.doctors
            .map((doctor) => {
              // Check if doctor has profile picture
              const hasProfilePic =
                doctor.profilePictureUrl &&
                doctor.profilePictureUrl.trim().length > 0;
              const avatarHtml = hasProfilePic
                ? `<img src="${doctor.profilePictureUrl}" alt="${doctor.name}" />`
                : `<i class="bi bi-person-fill"></i>`;

              // Make sure the doctor name has the Dr. prefix
              const doctorName = doctor.name.startsWith('Dr.')
                ? doctor.name
                : `Dr. ${doctor.name}`;

              return `
                <div class="doctor-card">
                    <div class="doctor-avatar">
                        ${avatarHtml}
                    </div>
                    <div class="doctor-info">
                        <span class="doctor-name">${doctorName}</span>
                        <span class="doctor-specialty">
                            <i class="bi bi-award"></i>
                            ${doctor.specialty || 'General Dentistry'}
                        </span>
                    </div>
                </div>
                `;
            })
            .join('')
        : '<p class="no-doctors">No doctors currently available for this treatment.</p>';

    // Prepare image or icon placeholder HTML
    const imageHtml = treatmentImageUrl
      ? `<img src="${treatmentImageUrl}" alt="${treatmentDetails.name}" class="treatment-image">`
      : `<div class="treatment-icon-placeholder"><i class="${getTreatmentIcon(
          treatmentName
        )}"></i></div>`;

    // Format price to include RM prefix and 2 decimal places
    const formattedPrice = `RM ${treatmentDetails.price.toFixed(2)}`;

    // Prepare the full HTML for the treatment details with new layout
    treatmentDetailsContent.innerHTML = `
            <button id="close-treatment-details">&times;</button>
            
            <div class="treatment-details-header">
                <h2>${treatmentDetails.name}</h2>
                <p class="treatment-price">${formattedPrice}</p>
            </div>
            
            <div class="treatment-details-container">
                <div class="treatment-details-left">
                    <div class="treatment-image-container">
                        ${imageHtml}
                    </div>
                </div>
                
                <div class="treatment-details-right">
                    <div class="treatment-description">
                        <h3>Treatment Description</h3>
                        <p>${
                          treatmentDetails.description ||
                          'No description available.'
                        }</p>
                    </div>
                    
                    <div class="treatment-info">
                        <div class="info-item">
                            <i class="bi bi-clock"></i>
                            <div class="info-content">
                                <span class="info-label">Duration</span>
                                <span class="info-value">${
                                  treatmentDetails.duration
                                } minutes</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="available-doctors">
                <h3>Available Doctors</h3>
                <div class="doctors-list">
                    ${doctorsHtml}
                </div>
            </div>
            
            <div class="treatment-details-actions">
                <button class="action-btn close-btn" id="close-treatment-details-bottom">Close</button>
                <button class="action-btn make-appointment-btn" id="make-appointment-btn">
                    <i class="bi bi-calendar-plus"></i> Make Appointment
                </button>
            </div>
        `;

    // Add event listeners for close buttons
    const closeTopBtn = document.getElementById('close-treatment-details');
    const closeBottomBtn = document.getElementById(
      'close-treatment-details-bottom'
    );
    const makeAppointmentBtn = document.getElementById('make-appointment-btn');

    // Close button event listeners
    if (closeTopBtn)
      closeTopBtn.addEventListener('click', closeTreatmentDetails);
    if (closeBottomBtn)
      closeBottomBtn.addEventListener('click', closeTreatmentDetails);

    // Make appointment button
    if (makeAppointmentBtn) {
      makeAppointmentBtn.addEventListener('click', () => {
        // Create a form and submit it instead of using window.location
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Patient/Appointments/SaveTreatmentSelection';
        form.style.display = 'none';

        // Add treatment ID
        const treatmentIdInput = document.createElement('input');
        treatmentIdInput.type = 'hidden';
        treatmentIdInput.name = 'treatmentId';
        treatmentIdInput.value = treatmentDetails.id;
        form.appendChild(treatmentIdInput);

        // Add redirect URL
        const redirectInput = document.createElement('input');
        redirectInput.type = 'hidden';
        redirectInput.name = 'redirectUrl';
        redirectInput.value = '/Patient/Appointments/Doctor';
        form.appendChild(redirectInput);

        // Add anti-forgery token if it exists
        const antiForgeryToken = document.querySelector(
          'input[name="__RequestVerificationToken"]'
        );
        if (antiForgeryToken) {
          const tokenInput = document.createElement('input');
          tokenInput.type = 'hidden';
          tokenInput.name = '__RequestVerificationToken';
          tokenInput.value = antiForgeryToken.value;
          form.appendChild(tokenInput);
        }

        document.body.appendChild(form);
        form.submit();
      });
    }
  }

  // Add click events to all treatment cards
  treatmentCards.forEach((card) => {
    card.addEventListener('click', function (e) {
      e.preventDefault();

      // Find the parent treatment-type-card element
      const treatmentCard = this.closest('.treatment-type-card');
      if (!treatmentCard) {
        console.error('Could not find parent treatment card');
        return;
      }

      const treatmentName = treatmentCard.getAttribute('data-treatment-name');
      if (!treatmentName) {
        console.error('Treatment name not found');
        return;
      }

      // Show loading animation if available
      if (typeof showLoadingAnimation === 'function') {
        showLoadingAnimation();
      }

      // Fetch treatment details
      fetchTreatmentDetails(treatmentName)
        .then((treatmentDetails) => {
          renderTreatmentDetails(treatmentDetails, treatmentName);
        })
        .catch((error) => {
          showErrorState(error.message || 'An unexpected error occurred');
        });
    });
  });

  // Close overlay when clicking outside the content
  treatmentDetailsOverlay.addEventListener('click', (e) => {
    if (e.target === treatmentDetailsOverlay) {
      closeTreatmentDetails();
    }
  });

  // Close on escape key
  document.addEventListener('keydown', (e) => {
    if (
      e.key === 'Escape' &&
      treatmentDetailsOverlay.classList.contains('is-visible')
    ) {
      closeTreatmentDetails();
    }
  });
});
