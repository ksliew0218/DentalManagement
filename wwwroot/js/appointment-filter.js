// Wrap all functionality in a function that can be called on page load and after AJAX navigation
function initializeAppointmentFilters() {
  // Tab switching functionality
  const tabButtons = document.querySelectorAll('.filter-tab');
  const tabContents = document.querySelectorAll('.tab-content');

  if (!tabButtons.length || !tabContents.length) {
    // If elements don't exist, exit early
    return;
  }

  tabButtons.forEach((button) => {
    button.addEventListener('click', function () {
      // Remove active class from all buttons and contents
      tabButtons.forEach((btn) => btn.classList.remove('active'));
      tabContents.forEach((content) => content.classList.remove('active'));

      // Add active class to current button and corresponding content
      this.classList.add('active');
      const tabId = this.getAttribute('data-tab');
      document.getElementById(tabId + '-tab').classList.add('active');

      // Apply active filters to the new tab content
      applyAllFilters();
    });
  });

  // Modal functionality
  const modal = document.getElementById('cancel-confirmation-modal');
  const closeButtons = document.querySelectorAll('.close-modal-btn');
  const keepAppointmentBtn = document.getElementById('keep-appointment-btn');

  function closeModal() {
    const modal = document.getElementById('cancel-confirmation-modal');
    if (modal) {
      modal.classList.remove('show');
      modal.style.display = 'none';
    }
  }

  if (closeButtons.length) {
    closeButtons.forEach((button) => {
      button.addEventListener('click', closeModal);
    });
  }

  if (keepAppointmentBtn) {
    keepAppointmentBtn.addEventListener('click', closeModal);
  }

  // Close modal if clicked outside
  window.addEventListener('click', function (event) {
    const modal = document.getElementById('cancel-confirmation-modal');
    if (event.target === modal) {
      closeModal();
    }
  });

  // Handle card animations
  const appointmentCards = document.querySelectorAll('.appointment-card');
  appointmentCards.forEach((card, index) => {
    setTimeout(() => {
      card.classList.add('visible');
    }, 100 * (index + 1));
  });

  // Date Filter Functionality
  const dateFilterBtn = document.getElementById('dateFilterBtn');
  const datePickerContainer = document.getElementById('datePickerContainer');
  const closeDatePicker = document.querySelector('.close-date-picker');
  const dateShortcuts = document.querySelectorAll('.date-shortcut');
  const applyDateFilter = document.querySelector('.apply-date-filter');
  const clearDateFilter = document.querySelector('.clear-date-filter');
  const startDateInput = document.getElementById('startDate');
  const endDateInput = document.getElementById('endDate');
  const activeFilters = document.getElementById('activeFilters');
  const activeFilterTags = document.getElementById('activeFilterTags');
  const clearAllFilters = document.getElementById('clearAllFilters');

  // If required elements don't exist, exit early
  if (!dateFilterBtn || !datePickerContainer) {
    return;
  }

  // Current filters state - initialize as a window property to persist across AJAX loads
  if (!window.appointmentFilters) {
    window.appointmentFilters = {
      search: '',
      dateRange: null,
    };
  }
  
  // Reference the current filters for easier reading
  const currentFilters = window.appointmentFilters;

  // Toggle date picker
  dateFilterBtn.addEventListener('click', function () {
    datePickerContainer.classList.toggle('active');
    this.classList.toggle('active');

    // Set initial values if date range is already selected
    if (currentFilters.dateRange) {
      startDateInput.value = currentFilters.dateRange.start;
      endDateInput.value = currentFilters.dateRange.end;

      // Highlight the corresponding shortcut if applicable
      dateShortcuts.forEach((shortcut) => {
        shortcut.classList.remove('active');
        if (
          shortcut.getAttribute('data-range') ===
          currentFilters.dateRange.shortcut
        ) {
          shortcut.classList.add('active');
        }
      });
    }
  });

  // Close date picker
  if (closeDatePicker) {
    closeDatePicker.addEventListener('click', function () {
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    });
  }

  // Click outside to close date picker
  document.addEventListener('click', function (event) {
    if (
      dateFilterBtn && !dateFilterBtn.contains(event.target) &&
      datePickerContainer && !datePickerContainer.contains(event.target)
    ) {
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    }
  });

  // Date shortcuts
  if (dateShortcuts.length) {
    dateShortcuts.forEach((shortcut) => {
      shortcut.addEventListener('click', function () {
        const range = this.getAttribute('data-range');

        // Remove active class from all shortcuts
        dateShortcuts.forEach((s) => s.classList.remove('active'));

        // Add active class to clicked shortcut
        this.classList.add('active');

        // Set date inputs based on shortcut
        const today = new Date();
        let startDate = new Date(today);
        let endDate = new Date(today);

        switch (range) {
          case 'today':
            // Start and end date are both today
            break;
          case 'week':
            // Start date is beginning of week (Sunday)
            startDate.setDate(today.getDate() - today.getDay());
            // End date is end of week (Saturday)
            endDate.setDate(startDate.getDate() + 6);
            break;
          case 'month':
            // Start date is 1st of current month
            startDate.setDate(1);
            // End date is last day of current month
            endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
            break;
          case 'all':
            // Clear the inputs for all time
            startDateInput.value = '';
            endDateInput.value = '';
            return;
        }

        // Format dates for inputs (YYYY-MM-DD)
        startDateInput.value = formatDateForInput(startDate);
        endDateInput.value = formatDateForInput(endDate);
      });
    });
  }

  // Helper function to format date for input
  function formatDateForInput(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // Helper function to format date for display
  function formatDateForDisplay(dateString) {
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
  }

  // Apply date filter
  if (applyDateFilter) {
    applyDateFilter.addEventListener('click', function () {
      const startDate = startDateInput.value;
      const endDate = endDateInput.value;

      if (startDate || endDate) {
        // Determine which shortcut was used (if any)
        let shortcut = null;
        dateShortcuts.forEach((s) => {
          if (s.classList.contains('active')) {
            shortcut = s.getAttribute('data-range');
          }
        });

        currentFilters.dateRange = {
          start: startDate,
          end: endDate,
          shortcut: shortcut,
        };

        // Update UI
        updateFilterTags();
        applyAllFilters();

        // Close date picker
        datePickerContainer.classList.remove('active');
        dateFilterBtn.classList.add('active');
      } else {
        // If no dates selected, clear the filter
        clearDateFilterAndUI();
      }
    });
  }

  // Clear date filter button inside date picker
  if (clearDateFilter) {
    clearDateFilter.addEventListener('click', function () {
      // Clear the date inputs and shortcuts
      startDateInput.value = '';
      endDateInput.value = '';
      dateShortcuts.forEach((s) => s.classList.remove('active'));

      // Clear the filter and update UI
      currentFilters.dateRange = null;
      updateFilterTags();
      applyAllFilters();

      // Close date picker
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    });
  }

  // Helper function to clear date filter and update UI
  function clearDateFilterAndUI() {
    if (startDateInput) startDateInput.value = '';
    if (endDateInput) endDateInput.value = '';
    
    dateShortcuts.forEach((s) => s.classList.remove('active'));
    currentFilters.dateRange = null;
    
    if (dateFilterBtn) dateFilterBtn.classList.remove('active');
    
    updateFilterTags();
    applyAllFilters();
  }

  // Clear all filters
  if (clearAllFilters) {
    clearAllFilters.addEventListener('click', function () {
      // Reset filter state
      currentFilters.search = '';
      currentFilters.dateRange = null;

      // Reset UI
      const searchElement = document.getElementById('appointmentSearch');
      if (searchElement) searchElement.value = '';
      
      if (startDateInput) startDateInput.value = '';
      if (endDateInput) endDateInput.value = '';
      
      dateShortcuts.forEach((s) => s.classList.remove('active'));
      
      if (dateFilterBtn) dateFilterBtn.classList.remove('active');

      // Hide active filters display
      if (activeFilters) activeFilters.style.display = 'none';

      // Show all appointments in the current tab
      const visibleTab = document.querySelector('.tab-content.active');
      if (visibleTab) {
        const cards = visibleTab.querySelectorAll('.appointment-card');
        cards.forEach((card) => {
          card.style.display = 'flex';
        });

        // Hide empty state
        const emptyState = visibleTab.querySelector('.empty-category-card');
        if (emptyState) {
          emptyState.style.display = 'none';
        }
      }
    });
  }

  // Update filter tags display
  function updateFilterTags() {
    if (!activeFilterTags) return;
    
    // Clear existing tags
    activeFilterTags.innerHTML = '';

    let hasActiveFilters = false;

    // Add search filter tag
    if (currentFilters.search) {
      hasActiveFilters = true;
      addFilterTag('Search', `"${currentFilters.search}"`, 'search');
    }

    // Add date range filter tag
    if (currentFilters.dateRange) {
      hasActiveFilters = true;

      let dateLabel;

      // Use friendly label for shortcuts
      if (currentFilters.dateRange.shortcut) {
        switch (currentFilters.dateRange.shortcut) {
          case 'today':
            dateLabel = 'Today';
            break;
          case 'week':
            dateLabel = 'This Week';
            break;
          case 'month':
            dateLabel = 'This Month';
            break;
          case 'all':
            dateLabel = 'All Time';
            break;
        }
      } else {
        // Custom date range
        if (currentFilters.dateRange.start && currentFilters.dateRange.end) {
          dateLabel = `${formatDateForDisplay(
            currentFilters.dateRange.start
          )} - ${formatDateForDisplay(currentFilters.dateRange.end)}`;
        } else if (currentFilters.dateRange.start) {
          dateLabel = `From ${formatDateForDisplay(
            currentFilters.dateRange.start
          )}`;
        } else if (currentFilters.dateRange.end) {
          dateLabel = `Until ${formatDateForDisplay(
            currentFilters.dateRange.end
          )}`;
        }
      }

      addFilterTag('Date', dateLabel, 'date');
    }

    // Show/hide active filters section
    if (activeFilters) {
      activeFilters.style.display = hasActiveFilters ? 'flex' : 'none';
    }
  }

  // Add a filter tag to the display
  function addFilterTag(type, value, filterKey) {
    const tagElement = document.createElement('div');
    tagElement.className = 'filter-tag';
    tagElement.innerHTML = `
            <span>${type}: ${value}</span>
            <button class="remove-filter" data-filter="${filterKey}">
                <i class="bi bi-x"></i>
            </button>
        `;

    // Add click event to remove button
    const removeButton = tagElement.querySelector('.remove-filter');
    removeButton.addEventListener('click', function () {
      const filterKey = this.getAttribute('data-filter');

      if (filterKey === 'search') {
        currentFilters.search = '';
        const searchElement = document.getElementById('appointmentSearch');
        if (searchElement) searchElement.value = '';
      } else if (filterKey === 'date') {
        clearDateFilterAndUI();
      }

      // Update UI and apply filters
      updateFilterTags();
      applyAllFilters();
    });

    activeFilterTags.appendChild(tagElement);
  }
  
  // Helper function to check if an appointment is in the past based on date and time
  function isAppointmentInPast(dateElement) {
    if (!dateElement) return false;
    
    const monthElement = dateElement.querySelector('.month');
    const dayElement = dateElement.querySelector('.day');
    const timeElement = dateElement.closest('.appointment-card').querySelector('.time');
    
    if (!monthElement || !dayElement) return false;
    
    const monthText = monthElement.textContent.trim();
    const dayText = dayElement.textContent.trim();
    let timeText = timeElement ? timeElement.textContent.trim() : '23:59'; // Default to end of day if no time
    
    // Extract just the time part if it has format or additional text
    if (timeText.includes(':')) {
      const match = timeText.match(/\d{1,2}:\d{2}/);
      timeText = match ? match[0] : '23:59';
    } else {
      timeText = '23:59'; // Default if no valid time format
    }
    
    // Parse month name to month index (0-11)
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const monthIndex = monthNames.findIndex(m => monthText.includes(m));
    
    if (monthIndex === -1) return false;
    
    // Create date object for the appointment (use current year)
    const now = new Date();
    const appointmentDate = new Date(
      now.getFullYear(),
      monthIndex,
      parseInt(dayText),
      parseInt(timeText.split(':')[0]),
      parseInt(timeText.split(':')[1])
    );
    
    // Compare with current date/time
    return appointmentDate < now;
  }

  // Apply all active filters
  function applyAllFilters() {
    const visibleTab = document.querySelector('.tab-content.active');

    if (visibleTab) {
      const cards = visibleTab.querySelectorAll('.appointment-card');
      let visibleCount = 0;

      cards.forEach((card) => {
        let visible = true;

        // Apply search filter
        if (currentFilters.search) {
          const treatmentName = card
            .querySelector('.treatment-name')
            .textContent.toLowerCase();
          const doctorName = card
            .querySelector('.doctor-info span')
            .textContent.toLowerCase();

          if (
            !treatmentName.includes(currentFilters.search.toLowerCase()) &&
            !doctorName.includes(currentFilters.search.toLowerCase())
          ) {
            visible = false;
          }
        }

        // Apply date filter
        if (visible && currentFilters.dateRange) {
          const dateBox = card.querySelector('.date-box');
          if (dateBox) {
            const monthText = dateBox.querySelector('.month').textContent;
            const dayText = dateBox.querySelector('.day').textContent;

            // Get year from current date (since the appointment card might not show year)
            const currentYear = new Date().getFullYear();

            // Parse month (convert from "Apr" to month number)
            const monthNames = [
              'Jan',
              'Feb',
              'Mar',
              'Apr',
              'May',
              'Jun',
              'Jul',
              'Aug',
              'Sep',
              'Oct',
              'Nov',
              'Dec',
            ];
            const monthIndex = monthNames.findIndex((m) =>
              monthText.includes(m)
            );

            // Create date object for appointment
            const appointmentDate = new Date(
              currentYear,
              monthIndex,
              parseInt(dayText)
            );

            // Check if date is within range
            if (currentFilters.dateRange.start) {
              const startDate = new Date(currentFilters.dateRange.start);
              startDate.setHours(0, 0, 0, 0); // Start of day

              if (appointmentDate < startDate) {
                visible = false;
              }
            }

            if (visible && currentFilters.dateRange.end) {
              const endDate = new Date(currentFilters.dateRange.end);
              endDate.setHours(23, 59, 59, 999); // End of day

              if (appointmentDate > endDate) {
                visible = false;
              }
            }
          }
        }

        // Special handling for tabs
        // Note: This is to ensure we maintain tab separation for cancelled appointments
        const tabId = visibleTab.id;
        const statusElement = card.querySelector('.appointment-status');
        const status = statusElement ? statusElement.textContent.trim() : '';
        
        // Make sure cancelled appointments only show in cancelled tab
        if (tabId !== 'cancelled-tab' && status === 'Cancelled') {
          visible = false;
        }

        // Update card visibility
        card.style.display = visible ? 'flex' : 'none';

        if (visible) {
          visibleCount++;
        }
      });

      // Show/hide empty state
      const emptyState = visibleTab.querySelector('.empty-category-card');
      if (emptyState) {
        emptyState.style.display = visibleCount === 0 ? 'flex' : 'none';
      }
    }
  }

  // Search functionality
  const searchInput = document.getElementById('appointmentSearch');
  if (searchInput) {
    // Set initial value if search filter exists
    if (currentFilters.search) {
      searchInput.value = currentFilters.search;
    }
    
    searchInput.addEventListener('input', function () {
      // Update current filter state
      currentFilters.search = this.value.trim();

      // Update UI and apply filters
      updateFilterTags();
      applyAllFilters();
    });
  }
  
  // Check each appointment and move it to the correct tab based on status and date
  function organizeAppointmentsByStatus() {
    const appointments = document.querySelectorAll('.appointment-card');
    const upcomingTab = document.getElementById('upcoming-tab');
    const pastTab = document.getElementById('past-tab');
    const cancelledTab = document.getElementById('cancelled-tab');
    
    if (!upcomingTab || !pastTab || !cancelledTab) return;
    
    // Process each appointment card
    appointments.forEach(card => {
      const statusElement = card.querySelector('.appointment-status');
      const status = statusElement ? statusElement.textContent.trim() : '';
      const dateBox = card.querySelector('.date-box');
      
      // Create a clone of the card to move to another tab if needed
      const cardClone = card.cloneNode(true);
      
      // Handle cancelled appointments - they always go to Cancelled tab
      if (status === 'Cancelled') {
        // If the card isn't already in the cancelled tab, move it there
        if (!card.parentElement.closest('#cancelled-tab')) {
          if (cancelledTab.querySelector('.appointments-grid')) {
            cancelledTab.querySelector('.appointments-grid').appendChild(cardClone);
          } else {
            // Create grid if it doesn't exist
            const grid = document.createElement('div');
            grid.className = 'appointments-grid';
            grid.appendChild(cardClone);
            
            // Replace empty state with grid if it exists
            const emptyState = cancelledTab.querySelector('.empty-category-card');
            if (emptyState) {
              cancelledTab.replaceChild(grid, emptyState);
            } else {
              cancelledTab.appendChild(grid);
            }
          }
          card.remove();
        }
      } 
      // Handle completed appointments - they go to Past tab
      else if (status === 'Completed') {
        // If the card isn't already in the past tab, move it there
        if (!card.parentElement.closest('#past-tab')) {
          if (pastTab.querySelector('.appointments-grid')) {
            pastTab.querySelector('.appointments-grid').appendChild(cardClone);
          } else {
            // Create grid if it doesn't exist
            const grid = document.createElement('div');
            grid.className = 'appointments-grid';
            grid.appendChild(cardClone);
            
            // Replace empty state with grid if it exists
            const emptyState = pastTab.querySelector('.empty-category-card');
            if (emptyState) {
              pastTab.replaceChild(grid, emptyState);
            } else {
              pastTab.appendChild(grid);
            }
          }
          card.remove();
        }
      }
      // Check if appointment is in the past based on date/time
      else if (isAppointmentInPast(dateBox)) {
        // If it's a past appointment and not already in past tab, move it there
        if (!card.parentElement.closest('#past-tab')) {
          if (pastTab.querySelector('.appointments-grid')) {
            pastTab.querySelector('.appointments-grid').appendChild(cardClone);
          } else {
            // Create grid if it doesn't exist
            const grid = document.createElement('div');
            grid.className = 'appointments-grid';
            grid.appendChild(cardClone);
            
            // Replace empty state with grid if it exists
            const emptyState = pastTab.querySelector('.empty-category-card');
            if (emptyState) {
              pastTab.replaceChild(grid, emptyState);
            } else {
              pastTab.appendChild(grid);
            }
          }
          card.remove();
        }
      }
    });
    
    // Check if any tab is empty and show empty state if needed
    [upcomingTab, pastTab, cancelledTab].forEach(tab => {
      const grid = tab.querySelector('.appointments-grid');
      if (grid && grid.children.length === 0) {
        grid.remove();
        
        // Only add empty state if it doesn't already exist
        if (!tab.querySelector('.empty-category-card')) {
          const emptyState = document.createElement('div');
          emptyState.className = 'empty-category-card';
          
          let iconClass, title, message;
          if (tab.id === 'upcoming-tab') {
            iconClass = 'bi-calendar-plus';
            title = 'No Upcoming Appointments';
            message = 'You don\'t have any scheduled appointments. Book a new appointment to get started.';
          } else if (tab.id === 'past-tab') {
            iconClass = 'bi-calendar-check';
            title = 'No Past Appointments';
            message = 'You don\'t have any past appointment records.';
          } else {
            iconClass = 'bi-calendar-x';
            title = 'No Cancelled Appointments';
            message = 'You don\'t have any cancelled appointments.';
          }
          
          emptyState.innerHTML = `
            <div class="empty-icon">
              <i class="bi ${iconClass}"></i>
            </div>
            <div class="empty-message">
              <h3>${title}</h3>
              <p>${message}</p>
              ${tab.id === 'upcoming-tab' ? `
                <a href="/Patient/Appointments/Book" class="action-btn schedule-btn">
                  <i class="bi bi-plus-circle"></i>
                  Book an Appointment
                </a>
              ` : ''}
            </div>
          `;
          
          tab.appendChild(emptyState);
        }
      }
    });
  }
  
  // Run the organization logic on page load
  organizeAppointmentsByStatus();
  
  // Apply existing filters on initialization
  updateFilterTags();
  applyAllFilters();
}

// Function for cancel appointment confirmation
function confirmCancelAppointment(appointmentId) {
  // Set the appointment ID in the hidden form field
  const appointmentIdInput = document.getElementById('appointmentIdInput');
  if (appointmentIdInput) {
    appointmentIdInput.value = appointmentId;
  }

  // Show the modal
  const modal = document.getElementById('cancel-confirmation-modal');
  if (modal) {
    modal.classList.add('show');
    modal.style.display = 'flex'; // Explicitly set display to flex
  }
}

// Make the function globally available
window.confirmCancelAppointment = confirmCancelAppointment;

// Initialize on page load
document.addEventListener('DOMContentLoaded', initializeAppointmentFilters);

// Re-initialize after AJAX content loads
document.addEventListener('contentLoaded', initializeAppointmentFilters);