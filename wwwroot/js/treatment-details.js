function initializeTreatmentDetails() {
  const treatmentCards = document.querySelectorAll('.learn-more-link');
  const treatmentDetailsOverlay = document.getElementById(
    'treatment-details-overlay'
  );
  const treatmentDetailsContent = document.getElementById(
    'treatment-details-content'
  );

  if (!treatmentDetailsOverlay || !treatmentDetailsContent) {
    console.log('Treatment details overlay or content not found in the DOM');
    return;
  }

  function closeTreatmentDetails() {
    treatmentDetailsOverlay.classList.remove('is-visible');
    document.body.classList.remove('no-scroll');
  }
  function showTreatmentDetails() {
    treatmentDetailsOverlay.classList.add('is-visible');
    document.body.classList.add('no-scroll');
  }
  async function fetchTreatmentDetails(treatmentName) {
    const timeoutPromise = new Promise((_, reject) =>
      setTimeout(() => reject(new Error('Request timed out')), 10000)
    );

    try {
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

  function showErrorState(message) {
    if (typeof hideLoadingAnimation === 'function') {
      hideLoadingAnimation();
    }

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

    const closeTopBtn = document.getElementById('close-treatment-details');
    const closeBottomBtn = document.getElementById(
      'close-treatment-details-bottom'
    );
    if (closeTopBtn)
      closeTopBtn.addEventListener('click', closeTreatmentDetails);
    if (closeBottomBtn)
      closeBottomBtn.addEventListener('click', closeTreatmentDetails);
  }

  function getTreatmentImageUrl(treatmentDetails) {
    return treatmentDetails.imageUrl || null;
  }

  function getTreatmentIcon(treatmentName) {
    const treatmentName_lower = treatmentName.toLowerCase();

    if (
      treatmentName_lower.includes('cleaning') ||
      treatmentName_lower.includes('scaling') ||
      treatmentName_lower.includes('polish')
    ) {
      return 'bi bi-brush';
    }

    if (
      treatmentName_lower.includes('whitening') ||
      treatmentName_lower.includes('bleach')
    ) {
      return 'bi bi-brightness-high';
    }

    if (
      treatmentName_lower.includes('extraction') ||
      treatmentName_lower.includes('removal') ||
      treatmentName_lower.includes('wisdom')
    ) {
      return 'bi bi-scissors';
    }

    if (
      treatmentName_lower.includes('root canal') ||
      treatmentName_lower.includes('endodontic') ||
      treatmentName_lower.includes('pulp')
    ) {
      return 'bi bi-tools';
    }

    if (
      treatmentName_lower.includes('crown') ||
      treatmentName_lower.includes('cap')
    ) {
      return 'bi bi-gem';
    }

    if (
      treatmentName_lower.includes('filling') ||
      treatmentName_lower.includes('restoration') ||
      treatmentName_lower.includes('composite') ||
      treatmentName_lower.includes('amalgam')
    ) {
      return 'bi bi-pencil-fill';
    }

    if (treatmentName_lower.includes('implant')) {
      return 'bi bi-nut';
    }

    if (
      treatmentName_lower.includes('braces') ||
      treatmentName_lower.includes('aligner') ||
      treatmentName_lower.includes('invisalign') ||
      treatmentName_lower.includes('orthodontic')
    ) {
      return 'bi bi-layout-three-columns';
    }

    if (
      treatmentName_lower.includes('denture') ||
      treatmentName_lower.includes('prosthetic')
    ) {
      return 'bi bi-emoji-smile';
    }

    if (
      treatmentName_lower.includes('x-ray') ||
      treatmentName_lower.includes('radiograph') ||
      treatmentName_lower.includes('scan') ||
      treatmentName_lower.includes('diagnostic')
    ) {
      return 'bi bi-camera';
    }

    if (
      treatmentName_lower.includes('surgery') ||
      treatmentName_lower.includes('surgical')
    ) {
      return 'bi bi-scissors';
    }

    if (
      treatmentName_lower.includes('gum') ||
      treatmentName_lower.includes('periodontal')
    ) {
      return 'bi bi-heart-pulse';
    }

    if (
      treatmentName_lower.includes('tooth') ||
      treatmentName_lower.includes('dental')
    ) {
      return 'bi bi-square';
    }

    return 'bi bi-plus-circle';
  }

  function renderTreatmentDetails(treatmentDetails, treatmentName) {
    if (typeof hideLoadingAnimation === 'function') {
      hideLoadingAnimation();
    }

    showTreatmentDetails();

    const treatmentImageUrl = getTreatmentImageUrl(treatmentDetails);

    const doctorsHtml =
      treatmentDetails.doctors && treatmentDetails.doctors.length > 0
        ? treatmentDetails.doctors
            .map((doctor) => {
              const hasProfilePic =
                doctor.profilePictureUrl &&
                doctor.profilePictureUrl.trim().length > 0;
              const avatarHtml = hasProfilePic
                ? `<img src="${doctor.profilePictureUrl}" alt="${doctor.name}" />`
                : `<i class="bi bi-person-fill"></i>`;

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

    const imageHtml = treatmentImageUrl
      ? `<img src="${treatmentImageUrl}" alt="${treatmentDetails.name}" class="treatment-image">`
      : `<div class="treatment-icon-placeholder"><i class="${getTreatmentIcon(
          treatmentName
        )}"></i></div>`;

    const formattedPrice = `RM ${treatmentDetails.price.toFixed(2)}`;

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

    const closeTopBtn = document.getElementById('close-treatment-details');
    const closeBottomBtn = document.getElementById(
      'close-treatment-details-bottom'
    );
    const makeAppointmentBtn = document.getElementById('make-appointment-btn');

    if (closeTopBtn)
      closeTopBtn.addEventListener('click', closeTreatmentDetails);
    if (closeBottomBtn)
      closeBottomBtn.addEventListener('click', closeTreatmentDetails);

    if (makeAppointmentBtn) {
      makeAppointmentBtn.addEventListener('click', () => {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Patient/Appointments/SaveTreatmentSelection';
        form.style.display = 'none';

        const treatmentIdInput = document.createElement('input');
        treatmentIdInput.type = 'hidden';
        treatmentIdInput.name = 'treatmentId';
        treatmentIdInput.value = treatmentDetails.id;
        form.appendChild(treatmentIdInput);

        const redirectInput = document.createElement('input');
        redirectInput.type = 'hidden';
        redirectInput.name = 'redirectUrl';
        redirectInput.value = '/Patient/Appointments/Doctor';
        form.appendChild(redirectInput);

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

  treatmentCards.forEach((card) => {
    card.addEventListener('click', function (e) {
      e.preventDefault();

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

      if (typeof showLoadingAnimation === 'function') {
        showLoadingAnimation();
      }

      fetchTreatmentDetails(treatmentName)
        .then((treatmentDetails) => {
          renderTreatmentDetails(treatmentDetails, treatmentName);
        })
        .catch((error) => {
          showErrorState(error.message || 'An unexpected error occurred');
        });
    });
  });

  treatmentDetailsOverlay.addEventListener('click', (e) => {
    if (e.target === treatmentDetailsOverlay) {
      closeTreatmentDetails();
    }
  });

  document.addEventListener('keydown', (e) => {
    if (
      e.key === 'Escape' &&
      treatmentDetailsOverlay.classList.contains('is-visible')
    ) {
      closeTreatmentDetails();
    }
  });
}
document.addEventListener('DOMContentLoaded', initializeTreatmentDetails);

document.addEventListener('contentLoaded', initializeTreatmentDetails);