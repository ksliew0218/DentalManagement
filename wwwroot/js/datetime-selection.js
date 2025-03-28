document.addEventListener('DOMContentLoaded', function () {
  // Initialize variables
  let selectedDate = null;
  let selectedTimeSlot = null;
  let selectedSlotIds = [];

  // Current date for reference
  const currentDate = new Date();
  // Current month and year that is displayed
  let currentMonth = currentDate.getMonth();
  let currentYear = currentDate.getFullYear();

  // Get dates with available slots from the data provided by the controller
  const availableDates = Object.keys(timeSlotData || {});

  console.log('Available dates from server:', availableDates);
  console.log('Treatment duration:', treatmentDuration, 'minutes');

  // Generate the calendar
  generateCalendar(currentMonth, currentYear);
  updateCurrentMonthDisplay();

  // Handle month navigation
  const prevMonthBtn = document.querySelector('.month-nav.prev');
  if (prevMonthBtn) {
    prevMonthBtn.addEventListener('click', function () {
      navigateMonth(-1);
    });
  }

  const nextMonthBtn = document.querySelector('.month-nav.next');
  if (nextMonthBtn) {
    nextMonthBtn.addEventListener('click', function () {
      navigateMonth(1);
    });
  }

  // Form submission
  const dateTimeForm = document.getElementById('dateTimeForm');
  if (dateTimeForm) {
    dateTimeForm.addEventListener('submit', function (event) {
      console.log('Form submission started');

      if (!selectedDate || !selectedTimeSlot || selectedSlotIds.length === 0) {
        console.error('Missing date, time, or slot ids selection');
        event.preventDefault();
        alert('Please select both a date and time slot');
        return false;
      }

      console.log('Selected date:', selectedDate);
      console.log('Selected time:', selectedTimeSlot);
      console.log('Selected slot IDs:', selectedSlotIds);

      // Update hidden inputs
      document.getElementById('selectedDate').value = selectedDate;
      document.getElementById('selectedTime').value = selectedTimeSlot;
      document.getElementById('selectedSlotIds').value =
        selectedSlotIds.join(',');

      // Store in session storage
      const dateTimeSelection = {
        fullDate: selectedDate,
        formattedDate: formatDateForDisplay(new Date(selectedDate)),
        timeSlot: selectedTimeSlot,
        slotIds: selectedSlotIds,
      };
      sessionStorage.setItem(
        'selectedDateTime',
        JSON.stringify(dateTimeSelection)
      );

      console.log('Form data prepared, continuing with submission');
      // Form will submit normally
    });
  }

  function updateCurrentMonthDisplay() {
    const monthNames = [
      'January',
      'February',
      'March',
      'April',
      'May',
      'June',
      'July',
      'August',
      'September',
      'October',
      'November',
      'December',
    ];
    const currentMonthElement = document.getElementById('current-month');
    if (currentMonthElement) {
      currentMonthElement.textContent = `${monthNames[currentMonth]} ${currentYear}`;
    }
  }

  function navigateMonth(direction) {
    currentMonth += direction;

    if (currentMonth < 0) {
      currentMonth = 11;
      currentYear--;
    } else if (currentMonth > 11) {
      currentMonth = 0;
      currentYear++;
    }

    // Regenerate calendar with new month
    generateCalendar(currentMonth, currentYear);
    updateCurrentMonthDisplay();
  }

  function generateCalendar(month, year) {
    const calendarDays = document.getElementById('calendar-days');
    if (!calendarDays) return;

    calendarDays.innerHTML = ''; // Clear previous calendar

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    // Add days from previous month
    for (let i = 0; i < firstDay; i++) {
      const day = daysInPrevMonth - firstDay + i + 1;
      addCalendarDay(day, 'inactive', null, calendarDays);
    }

    // Add days of current month
    for (let day = 1; day <= daysInMonth; day++) {
      const dateString = `${year}-${String(month + 1).padStart(
        2,
        '0'
      )}-${String(day).padStart(2, '0')}`;
      const dateObj = new Date(year, month, day);

      // Check if date is past
      const isPast = dateObj < new Date().setHours(0, 0, 0, 0);

      // Check if there are available slots for this date
      const isAvailable = !isPast && availableDates.includes(dateString);

      const isToday =
        day === currentDate.getDate() &&
        month === currentDate.getMonth() &&
        year === currentDate.getFullYear();

      const isSelected = selectedDate === dateString;

      let className = isAvailable ? 'available' : 'unavailable';
      if (isPast) className += ' past';
      if (isToday) className += ' today';
      if (isSelected) className += ' selected';

      addCalendarDay(day, className, dateString, calendarDays);
    }

    // Add days from next month to fill the grid (to make a complete grid of 6 rows)
    const totalDaysDisplayed = firstDay + daysInMonth;
    const rowsNeeded = Math.ceil(totalDaysDisplayed / 7);
    const cellsNeeded = rowsNeeded * 7;
    const nextMonthDays = cellsNeeded - totalDaysDisplayed;

    for (let day = 1; day <= nextMonthDays; day++) {
      addCalendarDay(day, 'inactive', null, calendarDays);
    }
  }

  function addCalendarDay(day, className, dateString, container) {
    const dayElement = document.createElement('div');
    dayElement.classList.add('calendar-day');
    if (className) {
      className.split(' ').forEach((cls) => dayElement.classList.add(cls));
    }
    dayElement.textContent = day;

    if (dateString) {
      dayElement.setAttribute('data-date', dateString);

      // Add click event only for available dates
      if (className.includes('available')) {
        dayElement.addEventListener('click', function () {
          selectDate(dateString);
        });
      }
    }

    container.appendChild(dayElement);
  }

  function selectDate(dateString) {
    // Clear previous selection
    const prevSelected = document.querySelector('.calendar-day.selected');
    if (prevSelected) {
      prevSelected.classList.remove('selected');
    }

    // Mark new selection
    const selectedDay = document.querySelector(
      `.calendar-day[data-date="${dateString}"]`
    );
    if (selectedDay) {
      selectedDay.classList.add('selected');
    }

    // Update selected date
    selectedDate = dateString;

    // Reset selected time slot
    selectedTimeSlot = null;
    selectedSlotIds = [];
    document.getElementById('next-btn').disabled = true;

    // Update displayed selected date
    const displayDate = formatDateForDisplay(new Date(dateString));
    const selectedDateElement = document.getElementById('selected-date');
    if (selectedDateElement) {
      selectedDateElement.textContent = displayDate;
    }

    // Generate time slots for this date using the real data
    generateTimeSlots(dateString);

    // Hide the calendar selection note
    const timeslotsNote = document.querySelector('.timeslots-note');
    if (timeslotsNote) {
      timeslotsNote.style.display = 'none';
    }
  }

  function formatDateForDisplay(date) {
    const options = {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    };
    return date.toLocaleDateString('en-US', options);
  }

  function isTimeSlotAvailable(dateString, timeString) {
    // Check if this is today
    const today = new Date();
    const selectedDate = new Date(dateString);

    // If not today, all slots are available
    if (
      selectedDate.getDate() !== today.getDate() ||
      selectedDate.getMonth() !== today.getMonth() ||
      selectedDate.getFullYear() !== today.getFullYear()
    ) {
      return true;
    }

    // For today, check if the time is at least 1 hour from now
    const now = new Date();
    const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);

    // Parse the time string (assuming format like "10:00 AM", "2:00 PM")
    const timeParts = timeString.match(/(\d+):(\d+)\s*(AM|PM)/i);
    if (!timeParts) return false;

    let hours = parseInt(timeParts[1]);
    const minutes = parseInt(timeParts[2]);
    const period = timeParts[3].toUpperCase();

    // Convert to 24-hour format
    if (period === 'PM' && hours < 12) hours += 12;
    if (period === 'AM' && hours === 12) hours = 0;

    // Create a date object for the slot time
    const slotTime = new Date(selectedDate);
    slotTime.setHours(hours, minutes, 0, 0);

    // Return true if slot time is at least 1 hour from now
    return slotTime >= oneHourFromNow;
  }

  function generateTimeSlots(dateString) {
    // Clear previous time slots
    const morningSlots = document.getElementById('morning-slots');
    const afternoonSlots = document.getElementById('afternoon-slots');
    const eveningSlots = document.getElementById('evening-slots');

    if (morningSlots) morningSlots.innerHTML = '';
    if (afternoonSlots) afternoonSlots.innerHTML = '';
    if (eveningSlots) eveningSlots.innerHTML = '';

    // Get slots for this date from the data provided by controller
    const slotsForDate = timeSlotData[dateString] || [];

    console.log(
      `Generating time slots for ${dateString}. Available slots:`,
      slotsForDate
    );

    // Group slots by period
    const morningSlotsList = slotsForDate.filter(
      (slot) => slot.Period === 'Morning'
    );
    const afternoonSlotsList = slotsForDate.filter(
      (slot) => slot.Period === 'Afternoon'
    );
    const eveningSlotsList = slotsForDate.filter(
      (slot) => slot.Period === 'Evening'
    );

    const hasSlots = slotsForDate.length > 0;

    // Show appropriate message if no slots available
    const noSlotsMessage = document.getElementById('no-slots-message');
    const timeslotsContainer = document.querySelector('.timeslots-container');

    if (noSlotsMessage) {
      noSlotsMessage.style.display = hasSlots ? 'none' : 'block';
    }

    if (timeslotsContainer) {
      timeslotsContainer.style.display = hasSlots ? 'flex' : 'none';
    }

    // Display time slots
    if (morningSlots) populateTimeSlots('morning-slots', morningSlotsList);
    if (afternoonSlots)
      populateTimeSlots('afternoon-slots', afternoonSlotsList);
    if (eveningSlots) populateTimeSlots('evening-slots', eveningSlotsList);
  }

  function populateTimeSlots(containerId, slots) {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Filter out slots that are within 1 hour from now for today
    const today = new Date().toISOString().split('T')[0]; // YYYY-MM-DD format

    // Filter slots if it's today
    let availableSlots = slots;
    if (selectedDate === today) {
      console.log(
        "Filtering today's slots to remove times within 1 hour from now"
      );
      availableSlots = slots.filter((slot) =>
        isTimeSlotAvailable(selectedDate, slot.FormattedStartTime)
      );
      console.log(
        `Original slots: ${slots.length}, After filtering: ${availableSlots.length}`
      );
    }

    if (availableSlots.length === 0) {
      const emptyMessage = document.createElement('div');
      emptyMessage.classList.add('empty-slots-message');
      emptyMessage.textContent = 'No available slots';
      container.appendChild(emptyMessage);
      return;
    }

    // Continue with the existing logic using availableSlots instead of slots
    availableSlots.forEach((slot) => {
      const slotButton = document.createElement('button');
      slotButton.classList.add('time-slot');

      // Check if slot requires multiple hours
      if (slot.RequiredSlots > 1) {
        slotButton.classList.add('multi-hour');
      }

      slotButton.textContent = slot.FormattedStartTime;
      slotButton.setAttribute('data-slot', slot.StartTime.substring(11, 16)); // HH:MM format
      slotButton.setAttribute('data-slot-id', slot.Id);

      // Store consecutive slot IDs as a data attribute
      if (slot.ConsecutiveSlotIds && slot.ConsecutiveSlotIds.length > 0) {
        slotButton.setAttribute(
          'data-consecutive-slots',
          JSON.stringify(slot.ConsecutiveSlotIds)
        );
      }

      // Add duration information for multi-hour slots
      if (slot.RequiredSlots > 1) {
        const durationSpan = document.createElement('span');
        durationSpan.classList.add('slot-duration');
        durationSpan.textContent = `(${slot.FormattedDuration})`;
        slotButton.appendChild(durationSpan);
      }

      slotButton.addEventListener('click', function () {
        // Clear previous selection
        document.querySelectorAll('.time-slot.selected').forEach((el) => {
          el.classList.remove('selected');
        });

        // Mark as selected
        this.classList.add('selected');

        // Update selected time
        selectedTimeSlot = this.getAttribute('data-slot');

        // Get consecutive slots if available
        const consecutiveSlotsAttr = this.getAttribute(
          'data-consecutive-slots'
        );
        if (consecutiveSlotsAttr) {
          try {
            selectedSlotIds = JSON.parse(consecutiveSlotsAttr);
            console.log('Selected consecutive slots:', selectedSlotIds);
          } catch (e) {
            console.error('Error parsing consecutive slots:', e);
            selectedSlotIds = [parseInt(this.getAttribute('data-slot-id'))];
          }
        } else {
          selectedSlotIds = [parseInt(this.getAttribute('data-slot-id'))];
        }

        // Enable the next button
        document.getElementById('next-btn').disabled = false;
      });

      container.appendChild(slotButton);
    });
  }

  // Check for stored date/time selection (for navigating back from the confirmation page)
  const storedDateTime = sessionStorage.getItem('selectedDateTime');
  if (storedDateTime) {
    try {
      const dateTime = JSON.parse(storedDateTime);
      if (dateTime.fullDate) {
        // Get date components
        const dateParts = dateTime.fullDate.split('-');
        if (dateParts.length === 3) {
          const year = parseInt(dateParts[0]);
          const month = parseInt(dateParts[1]) - 1; // JS months are 0-based

          // If the stored date is not in the current displayed month, navigate to it
          if (year !== currentYear || month !== currentMonth) {
            currentYear = year;
            currentMonth = month;
            generateCalendar(currentMonth, currentYear);
            updateCurrentMonthDisplay();
          }

          // Select the date
          selectDate(dateTime.fullDate);

          // If there's also a time selection and it's available,
          // find and select the matching time slot button
          if (dateTime.timeSlot) {
            setTimeout(() => {
              const timeSlotBtn = document.querySelector(
                `.time-slot[data-slot="${dateTime.timeSlot}"]`
              );
              if (timeSlotBtn) {
                timeSlotBtn.click();
              }
            }, 500); // Small delay to ensure time slots are generated
          }
        }
      }
    } catch (error) {
      console.error('Error parsing stored date/time:', error);
    }
  }
});
