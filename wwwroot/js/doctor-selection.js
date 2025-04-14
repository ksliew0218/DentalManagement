document.addEventListener('DOMContentLoaded', function() {
    let selectedDoctor = null;
    let selectedTreatment = null;
    
    console.log('Doctor selection page loaded');
    
    try {
        const treatmentDataElement = document.getElementById('treatment-data');
        if (treatmentDataElement) {
            selectedTreatment = JSON.parse(treatmentDataElement.value);
            console.log('Treatment data found in element:', selectedTreatment);
        } else {
            const storedTreatment = sessionStorage.getItem('selectedTreatment');
            if (storedTreatment) {
                selectedTreatment = JSON.parse(storedTreatment);
                console.log('Treatment data found in session storage:', selectedTreatment);
            } else {
                console.log('No treatment data found');
            }
        }
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
    
    const selectButtons = document.querySelectorAll('.select-doctor-btn');
    const nextButton = document.querySelector('.next-btn');
    
    selectButtons.forEach(button => {
        button.addEventListener('click', function() {
            console.log('Doctor selected');
            
            const doctorCard = this.closest('.doctor-card');
            
            document.querySelectorAll('.doctor-card').forEach(card => {
                card.classList.remove('selected');
                const selectBtn = card.querySelector('.select-doctor-btn');
                if (selectBtn) {
                    selectBtn.textContent = 'Select';
                }
            });
            
            doctorCard.classList.add('selected');
            this.textContent = 'Selected';
            
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
            
            if (nextButton) {
                nextButton.disabled = false;
            }
            
            const navButtons = document.querySelector('.navigation-buttons');
            if (navButtons) {
                navButtons.scrollIntoView({
                    behavior: 'smooth'
                });
            }
        });
    });
    
    if (nextButton) {
        console.log('Next button found');
        
        nextButton.addEventListener('click', function(event) {
            console.log('Next button clicked');
            
            event.preventDefault();
            
            if (selectedDoctor) {
                console.log('Submitting doctor selection, ID:', selectedDoctor.id);
                
                sessionStorage.setItem('selectedDoctor', JSON.stringify(selectedDoctor));
                
                try {
                    const form = document.createElement('form');
                    form.method = 'POST';
                    form.action = '/Patient/Appointments/SaveDoctorSelection';
                    form.style.display = 'none';
                    
                    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
                    console.log('Anti-forgery token found:', !!antiForgeryToken);
                    
                    if (antiForgeryToken) {
                        const tokenInput = document.createElement('input');
                        tokenInput.type = 'hidden';
                        tokenInput.name = '__RequestVerificationToken';
                        tokenInput.value = antiForgeryToken.value;
                        form.appendChild(tokenInput);
                    }
                    
                    let doctorIdToSubmit;
                    if (typeof selectedDoctor.id === 'string' && selectedDoctor.id === 'any') {
                        const firstDoctorCard = document.querySelector('.doctor-card:not(.any-available)');
                        if (firstDoctorCard) {
                            doctorIdToSubmit = firstDoctorCard.getAttribute('data-id');
                            console.log('Using first doctor as fallback, ID:', doctorIdToSubmit);
                        } else {
                            console.error('No doctor found for fallback');
                            return; 
                        }
                    } else {
                        doctorIdToSubmit = selectedDoctor.id;
                    }
                    
                    const doctorIdInput = document.createElement('input');
                    doctorIdInput.type = 'hidden';
                    doctorIdInput.name = 'doctorId';
                    doctorIdInput.value = doctorIdToSubmit;
                    form.appendChild(doctorIdInput);
                    
                    const redirectInput = document.createElement('input');
                    redirectInput.type = 'hidden';
                    redirectInput.name = 'redirectUrl';
                    redirectInput.value = '/Patient/Appointments/SelectDateTime';
                    form.appendChild(redirectInput);
                    
                    console.log('Form created, action:', form.action);
                    console.log('Doctor ID input value:', doctorIdInput.value);
                    console.log('Redirect URL:', redirectInput.value);
                    
                    document.body.appendChild(form);
                    
                    setTimeout(() => {
                        console.log('Submitting form...');
                        form.submit();
                    }, 100);
                } catch (error) {
                    console.error('Error during form submission:', error);
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
    
    doctorCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            card.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 100 + (index * 50));
    });
    
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