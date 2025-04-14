document.addEventListener('DOMContentLoaded', function() {
    console.log('Confirmation script loaded');
    
    const treatmentIdField = document.getElementById('treatmentId');
    const doctorIdField = document.getElementById('doctorId');
    const appointmentDateField = document.getElementById('appointmentDate');
    const appointmentTimeField = document.getElementById('appointmentTime');
    
    if (treatmentIdField) console.log('Treatment ID field value:', treatmentIdField.value);
    if (doctorIdField) console.log('Doctor ID field value:', doctorIdField.value);
    if (appointmentDateField) console.log('Appointment Date field value:', appointmentDateField.value);
    if (appointmentTimeField) console.log('Appointment Time field value:', appointmentTimeField.value);
    
    const policyCheckbox = document.getElementById('policy-agreement');
    const confirmButton = document.getElementById('confirm-btn');
    
    if (policyCheckbox && confirmButton) {
        console.log('Found policy checkbox and confirm button');
        
        confirmButton.disabled = !policyCheckbox.checked;
        
        policyCheckbox.addEventListener('change', function() {
            confirmButton.disabled = !this.checked;
            console.log('Checkbox state changed, button disabled:', confirmButton.disabled);
        });
    } else {
        console.error('Policy checkbox or confirm button not found');
    }
    
    const form = document.getElementById('confirmBookingForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            console.log('Form is being submitted');
            const formData = new FormData(form);
            console.log('Form data:');
            for (let pair of formData.entries()) {
                console.log(pair[0] + ': ' + pair[1]);
            }
        });
    }
    
    function animatePageElements() {
        const elements = document.querySelectorAll('.summary-card, .notes-section, .payment-section, .policy-section');
        
        elements.forEach((element, index) => {
            element.style.opacity = '0';
            element.style.transform = 'translateY(20px)';
            
            setTimeout(() => {
                element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }, 100 + (index * 100));
        });
    }
    
    animatePageElements();
});