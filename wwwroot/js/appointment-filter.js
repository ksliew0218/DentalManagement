function initializeAppointmentFilters() {
  const tabButtons = document.querySelectorAll('.filter-tab');
  const tabContents = document.querySelectorAll('.tab-content');

  if (!tabButtons.length || !tabContents.length) {
    return;
  }

  tabButtons.forEach((button) => {
    button.addEventListener('click', function () {
      tabButtons.forEach((btn) => btn.classList.remove('active'));
      tabContents.forEach((content) => content.classList.remove('active'));

      this.classList.add('active');
      const tabId = this.getAttribute('data-tab');
      document.getElementById(tabId + '-tab').classList.add('active');

      applyAllFilters();
    });
  });

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

  window.addEventListener('click', function (event) {
    const modal = document.getElementById('cancel-confirmation-modal');
    if (event.target === modal) {
      closeModal();
    }
  });

  const appointmentCards = document.querySelectorAll('.appointment-card');
  appointmentCards.forEach((card, index) => {
    setTimeout(() => {
      card.classList.add('visible');
    }, 100 * (index + 1));
  });

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

  if (!dateFilterBtn || !datePickerContainer) {
    return;
  }

  if (!window.appointmentFilters) {
    window.appointmentFilters = {
      search: '',
      dateRange: null,
    };
  }
  
  const currentFilters = window.appointmentFilters;

  dateFilterBtn.addEventListener('click', function () {
    datePickerContainer.classList.toggle('active');
    this.classList.toggle('active');

    if (currentFilters.dateRange) {
      startDateInput.value = currentFilters.dateRange.start;
      endDateInput.value = currentFilters.dateRange.end;

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

  if (closeDatePicker) {
    closeDatePicker.addEventListener('click', function () {
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    });
  }
  document.addEventListener('click', function (event) {
    if (
      dateFilterBtn && !dateFilterBtn.contains(event.target) &&
      datePickerContainer && !datePickerContainer.contains(event.target)
    ) {
      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    }
  });

  if (dateShortcuts.length) {
    dateShortcuts.forEach((shortcut) => {
      shortcut.addEventListener('click', function () {
        const range = this.getAttribute('data-range');

        dateShortcuts.forEach((s) => s.classList.remove('active'));

        this.classList.add('active');

        const today = new Date();
        let startDate = new Date(today);
        let endDate = new Date(today);

        switch (range) {
          case 'today':
            break;
          case 'week':
            startDate.setDate(today.getDate() - today.getDay());
            endDate.setDate(startDate.getDate() + 6);
            break;
          case 'month':
            startDate.setDate(1);
            endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
            break;
          case 'all':
            startDateInput.value = '';
            endDateInput.value = '';
            return;
        }

        startDateInput.value = formatDateForInput(startDate);
        endDateInput.value = formatDateForInput(endDate);
      });
    });
  }

  function formatDateForInput(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  function formatDateForDisplay(dateString) {
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
  }

  if (applyDateFilter) {
    applyDateFilter.addEventListener('click', function () {
      const startDate = startDateInput.value;
      const endDate = endDateInput.value;

      if (startDate || endDate) {
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

        updateFilterTags();
        applyAllFilters();

        datePickerContainer.classList.remove('active');
        dateFilterBtn.classList.add('active');
      } else {
        clearDateFilterAndUI();
      }
    });
  }

  if (clearDateFilter) {
    clearDateFilter.addEventListener('click', function () {
      startDateInput.value = '';
      endDateInput.value = '';
      dateShortcuts.forEach((s) => s.classList.remove('active'));

      currentFilters.dateRange = null;
      updateFilterTags();
      applyAllFilters();

      datePickerContainer.classList.remove('active');
      dateFilterBtn.classList.remove('active');
    });
  }

  function clearDateFilterAndUI() {
    if (startDateInput) startDateInput.value = '';
    if (endDateInput) endDateInput.value = '';
    
    dateShortcuts.forEach((s) => s.classList.remove('active'));
    currentFilters.dateRange = null;
    
    if (dateFilterBtn) dateFilterBtn.classList.remove('active');
    
    updateFilterTags();
    applyAllFilters();
  }

  if (clearAllFilters) {
    clearAllFilters.addEventListener('click', function () {
      currentFilters.search = '';
      currentFilters.dateRange = null;

      const searchElement = document.getElementById('appointmentSearch');
      if (searchElement) searchElement.value = '';
      
      if (startDateInput) startDateInput.value = '';
      if (endDateInput) endDateInput.value = '';
      
      dateShortcuts.forEach((s) => s.classList.remove('active'));
      
      if (dateFilterBtn) dateFilterBtn.classList.remove('active');

      if (activeFilters) activeFilters.style.display = 'none';

      const visibleTab = document.querySelector('.tab-content.active');
      if (visibleTab) {
        const cards = visibleTab.querySelectorAll('.appointment-card');
        cards.forEach((card) => {
          card.style.display = 'flex';
        });

        const emptyState = visibleTab.querySelector('.empty-category-card');
        if (emptyState) {
          emptyState.style.display = 'none';
        }
      }
    });
  }

  function updateFilterTags() {
    if (!activeFilterTags) return;
    
    activeFilterTags.innerHTML = '';

    let hasActiveFilters = false;

    if (currentFilters.search) {
      hasActiveFilters = true;
      addFilterTag('Search', `"${currentFilters.search}"`, 'search');
    }

    if (currentFilters.dateRange) {
      hasActiveFilters = true;

      let dateLabel;

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

    if (activeFilters) {
      activeFilters.style.display = hasActiveFilters ? 'flex' : 'none';
    }
  }

  function addFilterTag(type, value, filterKey) {
    const tagElement = document.createElement('div');
    tagElement.className = 'filter-tag';
    tagElement.innerHTML = `
            <span>${type}: ${value}</span>
            <button class="remove-filter" data-filter="${filterKey}">
                <i class="bi bi-x"></i>
            </button>
        `;

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

      updateFilterTags();
      applyAllFilters();
    });

    activeFilterTags.appendChild(tagElement);
  }
  
  function isAppointmentInPast(dateElement) {
    if (!dateElement) return false;
    
    const monthElement = dateElement.querySelector('.month');
    const dayElement = dateElement.querySelector('.day');
    const timeElement = dateElement.closest('.appointment-card').querySelector('.time');
    
    if (!monthElement || !dayElement) return false;
    
    const monthText = monthElement.textContent.trim();
    const dayText = dayElement.textContent.trim();
    let timeText = timeElement ? timeElement.textContent.trim() : '23:59'; 
    
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const monthIndex = monthNames.findIndex(m => monthText.includes(m));
    
    if (monthIndex === -1) return false;
    
    const now = new Date();
    
    if (timeText) {
      const timeMatch = timeText.match(/(\d{1,2}):(\d{2})(?:\s*(AM|PM))?/i);
      
      if (timeMatch) {
        let hours = parseInt(timeMatch[1]);
        const minutes = parseInt(timeMatch[2]);
        const ampm = timeMatch[3] ? timeMatch[3].toUpperCase() : null;
        
        if (ampm === 'PM' && hours < 12) {
          hours += 12; 
        } else if (ampm === 'AM' && hours === 12) {
          hours = 0; 
        }
        
        const appointmentDate = new Date(
          now.getFullYear(),
          monthIndex,
          parseInt(dayText),
          hours,
          minutes
        );
        
        return appointmentDate < now;
      }
    }
    
    const appointmentDate = new Date(
      now.getFullYear(),
      monthIndex,
      parseInt(dayText),
      23, 
      59
    );
    
    return appointmentDate < now;
  }

  function applyAllFilters() {
    const visibleTab = document.querySelector('.tab-content.active');

    if (visibleTab) {
      const cards = visibleTab.querySelectorAll('.appointment-card');
      let visibleCount = 0;

      cards.forEach((card) => {
        let visible = true;

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

        if (visible && currentFilters.dateRange) {
          const dateBox = card.querySelector('.date-box');
          if (dateBox) {
            const monthText = dateBox.querySelector('.month').textContent;
            const dayText = dateBox.querySelector('.day').textContent;

            const currentYear = new Date().getFullYear();

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

            const appointmentDate = new Date(
              currentYear,
              monthIndex,
              parseInt(dayText)
            );

            if (currentFilters.dateRange.start) {
              const startDate = new Date(currentFilters.dateRange.start);
              startDate.setHours(0, 0, 0, 0); 

              if (appointmentDate < startDate) {
                visible = false;
              }
            }

            if (visible && currentFilters.dateRange.end) {
              const endDate = new Date(currentFilters.dateRange.end);
              endDate.setHours(23, 59, 59, 999); 

              if (appointmentDate > endDate) {
                visible = false;
              }
            }
          }
        }

        const tabId = visibleTab.id;
        const statusElement = card.querySelector('.appointment-status');
        const status = statusElement ? statusElement.textContent.trim() : '';
        
        if (tabId !== 'cancelled-tab' && status === 'Cancelled') {
          visible = false;
        }

        card.style.display = visible ? 'flex' : 'none';

        if (visible) {
          visibleCount++;
        }
      });

      const emptyState = visibleTab.querySelector('.empty-category-card');
      if (emptyState) {
        emptyState.style.display = visibleCount === 0 ? 'flex' : 'none';
      }
    }
  }

  const searchInput = document.getElementById('appointmentSearch');
  if (searchInput) {
    if (currentFilters.search) {
      searchInput.value = currentFilters.search;
    }
    
    searchInput.addEventListener('input', function () {
      currentFilters.search = this.value.trim();

      updateFilterTags();
      applyAllFilters();
    });
  }
  
  function organizeAppointmentsByStatus() {
    const appointments = document.querySelectorAll('.appointment-card');
    const upcomingTab = document.getElementById('upcoming-tab');
    const pastTab = document.getElementById('past-tab');
    const cancelledTab = document.getElementById('cancelled-tab');
    
    if (!upcomingTab || !pastTab || !cancelledTab) return;
    
    const upcomingAppointments = [];
    const pastAppointments = [];
    const cancelledAppointments = [];
    
    appointments.forEach(card => {
      const statusElement = card.querySelector('.appointment-status');
      const status = statusElement ? statusElement.textContent.trim() : '';
      const dateBox = card.querySelector('.date-box');
      
      const cardClone = card.cloneNode(true);
      
      if (status === 'Cancelled') {
        cancelledAppointments.push(cardClone);
      } 
      else if (status === 'Completed') {
        pastAppointments.push(cardClone);
      }
      else if (isAppointmentInPast(dateBox)) {
        pastAppointments.push(cardClone);
      }
      else {
        upcomingAppointments.push(cardClone);
      }
    });
    
    function updateTabWithAppointments(tab, appointments) {
      const existingGrid = tab.querySelector('.appointments-grid');
      const existingEmpty = tab.querySelector('.empty-category-card');
      
      if (existingGrid) existingGrid.remove();
      if (existingEmpty) existingEmpty.remove();
      
      if (appointments.length > 0) {
        const grid = document.createElement('div');
        grid.className = 'appointments-grid';
        
        appointments.forEach(card => {
          grid.appendChild(card);
        });
        
        tab.appendChild(grid);
      } else {
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
    
    updateTabWithAppointments(upcomingTab, upcomingAppointments);
    updateTabWithAppointments(pastTab, pastAppointments);
    updateTabWithAppointments(cancelledTab, cancelledAppointments);
    
    appointments.forEach(card => card.remove());
    
    const newCards = document.querySelectorAll('.appointment-card');
    newCards.forEach((card, index) => {
      setTimeout(() => {
        card.classList.add('visible');
      }, 100 * (index + 1));
    });
  }
  
  organizeAppointmentsByStatus();
  
  updateFilterTags();
  applyAllFilters();
}

function confirmCancelAppointment(appointmentId) {
  const appointmentIdInput = document.getElementById('appointmentIdInput');
  if (appointmentIdInput) {
    appointmentIdInput.value = appointmentId;
  }

  const modal = document.getElementById('cancel-confirmation-modal');
  if (modal) {
    modal.classList.add('show');
    modal.style.display = 'flex'; 
  }
}

window.confirmCancelAppointment = confirmCancelAppointment;

document.addEventListener('DOMContentLoaded', initializeAppointmentFilters);

document.addEventListener('contentLoaded', initializeAppointmentFilters);