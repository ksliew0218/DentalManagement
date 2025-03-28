document.addEventListener('DOMContentLoaded', function() {
    // Initialize variables
    let selectedDoctor = null;
    let selectedTreatment = null;
    
    // Add debugging
    console.log('Doctor selection page loaded');
    
    // Retrieve the selected treatment from session storage or TempData
    try {
        // Check for treatment data in TempData (added by controller)
        const treatmentDataElement = document.getElementById('treatment-data');
        if (treatmentDataElement) {
            selectedTreatment = JSON.parse(treatmentDataElement.value);
            console.log('Treatment data found in element:', selectedTreatment);
        } else {
            // Fallback to session storage
            const storedTreatment = sessionStorage.getItem('selectedTreatment');
            if (storedTreatment) {
                selectedTreatment = JSON.parse(storedTreatment);
                console.log('Treatment data found in session storage:', selectedTreatment);
            } else {
                console.log('No treatment data found');
            }
        }
        
        // Update the treatment summary in the UI based on ViewData
        const treatmentName = document.getElementById('treatment-name');
        if (treatmentName) {
            treatmentName.textContent = treatmentName.textContent || selectedTreatment?.name || '';
        }
        
        const selectedTreatmentName = document.getElementById('selected-treatment-name');
        if (selectedTreatmentName) {
            selectedTreatmentName.textContent = selectedTreatmentName.textContent || selectedTreatment?.name || '';
        }
    } catch (error) {
        console.error('Error getting treatment data:', error);
    }
    
    // Handle doctor filtering
    const specializationFilter = document.getElementById('specialization-filter');
    const doctorCards = document.querySelectorAll('.doctor-card:not(.any-available)');
    
    if (specializationFilter) {
        specializationFilter.addEventListener('change', function() {
            const specialization = this.value;
            
            doctorCards.forEach(card => {
                if (specialization === 'all' || card.getAttribute('data-specialization') === specialization) {
                    card.style.display = 'flex';
                } else {
                    card.style.display = 'none';
                }
            });
        });
    }
    
    // Handle doctor selection
    const selectButtons = document.querySelectorAll('.select-doctor-btn');
    const nextButton = document.querySelector('.next-btn');
    
    selectButtons.forEach(button => {
        button.addEventListener('click', function() {
            console.log('Doctor selected');
            
            // Get the parent doctor card
            const doctorCard = this.closest('.doctor-card');
            
            // Remove selection from all cards
            document.querySelectorAll('.doctor-card').forEach(card => {
                card.classList.remove('selected');
                const selectBtn = card.querySelector('.select-doctor-btn');
                if (selectBtn) {
                    selectBtn.textContent = 'Select';
                }
            });
            
            // Select this card
            doctorCard.classList.add('selected');
            this.textContent = 'Selected';
            
            // Store the selected doctor
            if (doctorCard.classList.contains('any-available')) {
                selectedDoctor = {
                    id: 'any',
                    name: 'Any Available Doctor',
                    specialization: 'Any'
                };
            } else {
                selectedDoctor = {
                    id: doctorCard.getAttribute('data-id'),
                    name: doctorCard.querySelector('h3').textContent,
                    specialization: doctorCard.querySelector('.specialty-badge').textContent,
                    img: doctorCard.querySelector('img') ? doctorCard.querySelector('img').src : null
                };
            }
            
            console.log('Selected doctor:', selectedDoctor);
            
            // Enable the next button
            if (nextButton) {
                nextButton.disabled = false;
            }
            
            // Smooth scroll to the navigation buttons
            const navButtons = document.querySelector('.navigation-buttons');
            if (navButtons) {
                navButtons.scrollIntoView({
                    behavior: 'smooth'
                });
            }
        });
    });
    
    // Handle next button click - use a form submission to post data to the server
    if (nextButton) {
        console.log('Next button found');
        
        nextButton.addEventListener('click', function(event) {
            console.log('Next button clicked');
            
            // Prevent default button behavior
            event.preventDefault();
            
            if (selectedDoctor) {
                console.log('Submitting doctor selection, ID:', selectedDoctor.id);
                
                // Store the selected doctor in session storage
                sessionStorage.setItem('selectedDoctor', JSON.stringify(selectedDoctor));
                
                try {
                    // Create a form to submit doctor ID to server
                    const form = document.createElement('form');
                    form.method = 'POST';
                    form.action = '/Patient/Appointments/SaveDoctorSelection';
                    form.style.display = 'none';
                    
                    // Add CSRF token
                    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
                    console.log('Anti-forgery token found:', !!antiForgeryToken);
                    
                    if (antiForgeryToken) {
                        const tokenInput = document.createElement('input');
                        tokenInput.type = 'hidden';
                        tokenInput.name = '__RequestVerificationToken';
                        tokenInput.value = antiForgeryToken.value;
                        form.appendChild(tokenInput);
                    }
                    
                    // Get the actual doctor ID to submit
                    let doctorIdToSubmit;
                    if (typeof selectedDoctor.id === 'string' && selectedDoctor.id === 'any') {
                        // Use the first doctor as fallback for 'any' selection
                        const firstDoctorCard = document.querySelector('.doctor-card:not(.any-available)');
                        if (firstDoctorCard) {
                            doctorIdToSubmit = firstDoctorCard.getAttribute('data-id');
                            console.log('Using first doctor as fallback, ID:', doctorIdToSubmit);
                        } else {
                            console.error('No doctor found for fallback');
                            return; // Exit if no doctor is found
                        }
                    } else {
                        doctorIdToSubmit = selectedDoctor.id;
                    }
                    
                    // Add doctor ID
                    const doctorIdInput = document.createElement('input');
                    doctorIdInput.type = 'hidden';
                    doctorIdInput.name = 'doctorId';
                    doctorIdInput.value = doctorIdToSubmit;
                    form.appendChild(doctorIdInput);
                    
                    // Add redirect URL
                    const redirectInput = document.createElement('input');
                    redirectInput.type = 'hidden';
                    redirectInput.name = 'redirectUrl';
                    redirectInput.value = '/Patient/Appointments/SelectDateTime';
                    form.appendChild(redirectInput);
                    
                    console.log('Form created, action:', form.action);
                    console.log('Doctor ID input value:', doctorIdInput.value);
                    console.log('Redirect URL:', redirectInput.value);
                    
                    // Append form to document and submit
                    document.body.appendChild(form);
                    
                    // Add a small delay to ensure form is properly added to DOM
                    setTimeout(() => {
                        console.log('Submitting form...');
                        form.submit();
                    }, 100);
                } catch (error) {
                    console.error('Error during form submission:', error);
                    // Fallback direct navigation
                    window.location.href = '/Patient/Appointments/SelectDateTime';
                }
            } else {
                console.warn('No doctor selected');
                alert('Please select a doctor before proceeding.');
            }
        });
    } else {
        console.warn('Next button not found');
    }
    
    // Add animation to the cards
    doctorCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            card.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 100 + (index * 50));
    });
    
    // Also animate the any-available doctor card
    const anyDoctorCard = document.querySelector('.doctor-card.any-available');
    if (anyDoctorCard) {
        anyDoctorCard.style.opacity = '0';
        anyDoctorCard.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            anyDoctorCard.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            anyDoctorCard.style.opacity = '1';
            anyDoctorCard.style.transform = 'translateY(0)';
        }, 50);
    }
});