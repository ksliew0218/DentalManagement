// Appointment Filter Functionality
document.addEventListener('DOMContentLoaded', function () {
  // Tab switching functionality
  const tabButtons = document.querySelectorAll('.filter-tab');
  const tabContents = document.querySelectorAll('.tab-content');

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
    modal.classList.remove('show');
    modal.style.display = 'none';
  }

  closeButtons.forEach((button) => {
    button.addEventListener('click', closeModal);
  });

  keepAppointmentBtn.addEventListener('click', closeModal);

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

  // Current filters state
  let currentFilters = {
    search: '',
    dateRange: null,
  };

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
  closeDatePicker.addEventListener('click', function () {
    datePickerContainer.classList.remove('active');
    dateFilterBtn.classList.remove('active');
  });

  // Click outside to close date picker
  document.addEventListener('click', function (event) {
    if (
      !dateFilterBtn.contains(event.target) &&
      !datePickerContainer.contains(event.target)
    ) {
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    }
  });

  // Date shortcuts
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

  // Clear date filter button inside date picker
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

  // Helper function to clear date filter and update UI
  function clearDateFilterAndUI() {
    startDateInput.value = '';
    endDateInput.value = '';
    dateShortcuts.forEach((s) => s.classList.remove('active'));
    currentFilters.dateRange = null;
    dateFilterBtn.classList.remove('active');
    updateFilterTags();
    applyAllFilters();
  }

  // Clear all filters
  clearAllFilters.addEventListener('click', function () {
    // Reset filter state
    currentFilters = {
      search: '',
      dateRange: null,
    };

    // Reset UI
    document.getElementById('appointmentSearch').value = '';
    startDateInput.value = '';
    endDateInput.value = '';
    dateShortcuts.forEach((s) => s.classList.remove('active'));
    dateFilterBtn.classList.remove('active');

    // Hide active filters display
    activeFilters.style.display = 'none';

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

  // Update filter tags display
  function updateFilterTags() {
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
    activeFilters.style.display = hasActiveFilters ? 'flex' : 'none';
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
        document.getElementById('appointmentSearch').value = '';
      } else if (filterKey === 'date') {
        clearDateFilterAndUI();
      }

      // Update UI and apply filters
      updateFilterTags();
      applyAllFilters();
    });

    activeFilterTags.appendChild(tagElement);
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
    searchInput.addEventListener('input', function () {
      // Update current filter state
      currentFilters.search = this.value.trim();

      // Update UI and apply filters
      updateFilterTags();
      applyAllFilters();
    });
  }
});

// Function for cancel appointment confirmation
function confirmCancelAppointment(appointmentId) {
  // Set the appointment ID in the hidden form field
  document.getElementById('appointmentIdInput').value = appointmentId;

  // Show the modal
  const modal = document.getElementById('cancel-confirmation-modal');
  modal.classList.add('show');
  modal.style.display = 'flex'; // Explicitly set display to flex
}
