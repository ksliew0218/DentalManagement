// treatment-details.js
document.addEventListener('DOMContentLoaded', () => {
    // Get all Learn More links
    const treatmentCards = document.querySelectorAll('.learn-more-link');
    const treatmentDetailsOverlay = document.getElementById('treatment-details-overlay');
    const treatmentDetailsContent = document.getElementById('treatment-details-content');

    // Check if elements exist
    if (!treatmentDetailsOverlay || !treatmentDetailsContent) {
        console.error('Treatment details overlay or content not found in the DOM');
        return;
    }

    console.log('Initializing treatment details overlay...');

    // Close the treatment details overlay
    function closeTreatmentDetails() {
        console.log('Closing treatment details overlay...');
        treatmentDetailsOverlay.classList.remove('is-visible');
        document.body.classList.remove('no-scroll');
    }

    // Show treatment details
    function showTreatmentDetails() {
        console.log('Showing treatment details overlay...');
        treatmentDetailsOverlay.classList.add('is-visible');
        document.body.classList.add('no-scroll');
    }

    // Fetch treatment details with timeout
    async function fetchTreatmentDetails(treatmentName) {
        console.log('Fetching treatment details for:', treatmentName);
        // Create a timeout promise
        const timeoutPromise = new Promise((_, reject) => 
            setTimeout(() => reject(new Error('Request timed out')), 10000)
        );

        try {
            // Race between fetch and timeout
            const response = await Promise.race([
                fetch(`/Patient/Treatments/GetTreatmentDetails?treatmentName=${encodeURIComponent(treatmentName)}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                }),
                timeoutPromise
            ]);

            // Check if response is ok
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to fetch treatment details');
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching treatment details:', error);
            throw error;
        }
    }

    // Show error state
    function showErrorState(message) {
        console.log('Showing error state:', message);
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
        const closeBottomBtn = document.getElementById('close-treatment-details-bottom');
        if (closeTopBtn) closeTopBtn.addEventListener('click', closeTreatmentDetails);
        if (closeBottomBtn) closeBottomBtn.addEventListener('click', closeTreatmentDetails);
    }

    // Render treatment details - Adaptive Layout based on doctor count
    function renderTreatmentDetails(treatmentDetails, treatmentName) {
        console.log('Rendering treatment details:', treatmentDetails);
        
        // Hide loading animation
        if (typeof hideLoadingAnimation === 'function') {
            hideLoadingAnimation();
        }

        // Show the overlay
        showTreatmentDetails();

        // Count the number of doctors
        const doctorCount = treatmentDetails.doctors ? treatmentDetails.doctors.length : 0;
        const hasFewDoctors = doctorCount <= 2;
        
        // Prepare doctors list HTML
        const doctorsHtml = treatmentDetails.doctors && treatmentDetails.doctors.length > 0 
            ? treatmentDetails.doctors.map(doctor => `
                <div class="doctor-card">
                    <div class="doctor-avatar">
                        <i class="bi bi-person-badge"></i>
                    </div>
                    <div class="doctor-info">
                        <span class="doctor-name">${doctor.name}</span>
                        <span class="doctor-specialty">${doctor.specialty || 'General Dentistry'}</span>
                    </div>
                </div>
            `).join('')
            : '<p class="no-doctors">No doctors currently available for this treatment.</p>';

        // Adapt the layout based on doctor count
        const layoutClass = hasFewDoctors ? 'few-doctors' : '';

        // Prepare the full HTML for the treatment details with horizontal layout
        treatmentDetailsContent.innerHTML = `
            <button id="close-treatment-details">&times;</button>
            
            <div class="treatment-details-header">
                <div class="treatment-details-icon">
                    <i class="${getTreatmentIcon(treatmentName)}"></i>
                </div>
                <div class="treatment-details-title">
                    <h2>${treatmentDetails.name}</h2>
                    <p class="treatment-price">RM ${treatmentDetails.price.toFixed(2)}</p>
                </div>
            </div>
            
            <div class="treatment-details-container ${layoutClass}">
                <div class="treatment-details-left">
                    <div class="treatment-description">
                        <h3>Treatment Description</h3>
                        <p>${treatmentDetails.description || 'No description available.'}</p>
                    </div>
                    
                    <div class="treatment-info">
                        <div class="info-item">
                            <i class="bi bi-clock"></i>
                            <div class="info-content">
                                <span class="info-label">Duration</span>
                                <span class="info-value">${treatmentDetails.duration} minutes</span>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="treatment-details-right">
                    <div class="available-doctors">
                        <h3>Available Doctors</h3>
                        <div class="doctors-list">
                            ${doctorsHtml}
                        </div>
                    </div>
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
        const closeBottomBtn = document.getElementById('close-treatment-details-bottom');
        const makeAppointmentBtn = document.getElementById('make-appointment-btn');

        // Close button event listeners
        if (closeTopBtn) closeTopBtn.addEventListener('click', closeTreatmentDetails);
        if (closeBottomBtn) closeBottomBtn.addEventListener('click', closeTreatmentDetails);

        // Make appointment button (placeholder)
        if (makeAppointmentBtn) {
            makeAppointmentBtn.addEventListener('click', () => {
                alert('Appointment booking functionality coming soon!');
            });
        }
    }

    // Helper function to get treatment icon
    function getTreatmentIcon(treatmentName) {
        const treatmentName_lower = treatmentName.toLowerCase();
        
        // Cleaning related treatments
        if (treatmentName_lower.includes('cleaning') || 
            treatmentName_lower.includes('scaling') || 
            treatmentName_lower.includes('polish')) {
            return 'bi bi-brush';
        }
        
        // Whitening related treatments
        if (treatmentName_lower.includes('whitening') || 
            treatmentName_lower.includes('bleach')) {
            return 'bi bi-brightness-high';
        }
        
        // Extraction related treatments
        if (treatmentName_lower.includes('extraction') || 
            treatmentName_lower.includes('removal') || 
            treatmentName_lower.includes('wisdom')) {
            return 'bi bi-scissors';
        }
        
        // Root canal related treatments
        if (treatmentName_lower.includes('root canal') || 
            treatmentName_lower.includes('endodontic') || 
            treatmentName_lower.includes('pulp')) {
            return 'bi bi-tools';
        }
        
        // Crown related treatments
        if (treatmentName_lower.includes('crown') || 
            treatmentName_lower.includes('cap')) {
            return 'bi bi-gem';
        }
        
        // Filling related treatments
        if (treatmentName_lower.includes('filling') || 
            treatmentName_lower.includes('restoration') || 
            treatmentName_lower.includes('composite') || 
            treatmentName_lower.includes('amalgam')) {
            return 'bi bi-pencil-fill';
        }
        
        // Implant related treatments
        if (treatmentName_lower.includes('implant')) {
            return 'bi bi-nut';
        }
        
        // Orthodontic treatments
        if (treatmentName_lower.includes('braces') || 
            treatmentName_lower.includes('aligner') || 
            treatmentName_lower.includes('invisalign') || 
            treatmentName_lower.includes('orthodontic')) {
            return 'bi bi-layout-three-columns';
        }
        
        // Dentures and prosthetics
        if (treatmentName_lower.includes('denture') || 
            treatmentName_lower.includes('prosthetic')) {
            return 'bi bi-emoji-smile';
        }
        
        // X-ray and diagnostics
        if (treatmentName_lower.includes('x-ray') || 
            treatmentName_lower.includes('radiograph') || 
            treatmentName_lower.includes('scan') || 
            treatmentName_lower.includes('diagnostic')) {
            return 'bi bi-camera';
        }
        
        // Surgical procedures
        if (treatmentName_lower.includes('surgery') || 
            treatmentName_lower.includes('surgical')) {
            return 'bi bi-scissors';
        }
        
        // Periodontal treatments
        if (treatmentName_lower.includes('gum') || 
            treatmentName_lower.includes('periodontal')) {
            return 'bi bi-heart-pulse';
        }
        
        // Tooth related general
        if (treatmentName_lower.includes('tooth') || 
            treatmentName_lower.includes('dental')) {
            return 'bi bi-square';
        }
        
        // Default icon for any other treatment
        return 'bi bi-plus-circle';
    }

    // Open treatment details
    treatmentCards.forEach(card => {
        console.log('Adding click event listener to treatment card:', card);
        card.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Treatment card clicked');
            
            // Find the parent treatment-type-card element
            const treatmentCard = this.closest('.treatment-type-card');
            if (!treatmentCard) {
                console.error('Could not find parent treatment card');
                return;
            }
            
            const treatmentName = treatmentCard.getAttribute('data-treatment-name');
            console.log('Treatment name:', treatmentName);
            
            if (!treatmentName) {
                console.error('Treatment name not found');
                return;
            }
            
            // Show loading animation using global function
            if (typeof showLoadingAnimation === 'function') {
                showLoadingAnimation();
            }
            
            // For testing purposes, we can display mock data
            const useMockData = false; // Set to false to use real API
            
            if (useMockData) {
                // Mock treatment details for testing
                setTimeout(() => {
                    const mockData = {
                        name: treatmentName,
                        description: "This is a detailed description of the treatment. It explains the procedure, benefits, and what patients can expect during and after the treatment.",
                        price: 250.00,
                        duration: 60,
                        doctors: [
                            { name: "Dr. John Smith", specialty: "General Dentistry" },
                            { name: "Dr. Sarah Wilson", specialty: "Orthodontics" }
                        ]
                    };
                    renderTreatmentDetails(mockData, treatmentName);
                }, 1000);
            } else {
                // Fetch real treatment details
                fetchTreatmentDetails(treatmentName)
                    .then(treatmentDetails => {
                        renderTreatmentDetails(treatmentDetails, treatmentName);
                    })
                    .catch(error => {
                        showErrorState(error.message || 'An unexpected error occurred');
                    });
            }
        });
    });

    // Close overlay when clicking outside the content
    treatmentDetailsOverlay.addEventListener('click', (e) => {
        if (e.target === treatmentDetailsOverlay) {
            closeTreatmentDetails();
        }
    });

    // For testing, add a manual trigger with options for doctor count
    window.openTreatmentDetails = function(treatmentName, doctorCount = 5) {
        if (typeof showLoadingAnimation === 'function') {
            showLoadingAnimation();
        }
        
        // Create sample doctors based on count
        const sampleDoctors = [];
        for (let i = 1; i <= doctorCount; i++) {
            sampleDoctors.push({
                name: `Dr. Sample Doctor ${i}`,
                specialty: i % 2 === 0 ? "Orthodontics" : "General Dentistry"
            });
        }
        
        setTimeout(() => {
            const mockData = {
                name: treatmentName || "Test Treatment",
                description: "This is a test description for debugging purposes. It provides details about what the treatment involves, how it's performed, and the expected outcomes.",
                price: 150.00,
                duration: 45,
                doctors: sampleDoctors
            };
            renderTreatmentDetails(mockData, treatmentName || "Test Treatment");
        }, 1000);
    };

    // Log initialization complete
    console.log('Treatment details overlay initialization complete');
});