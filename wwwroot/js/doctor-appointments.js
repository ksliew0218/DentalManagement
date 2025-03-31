// Doctor Appointments Page JavaScript

$(document).ready(function() {
    // Initialize DataTable with advanced options
    var appointmentsTable = $('#appointmentsTable').DataTable({
        responsive: true,
        language: {
            search: "_INPUT_",
            searchPlaceholder: "Search appointments...",
            lengthMenu: "Show _MENU_ appointments",
            info: "Showing _START_ to _END_ of _TOTAL_ appointments",
            infoEmpty: "Showing 0 to 0 of 0 appointments",
            infoFiltered: "(filtered from _MAX_ total appointments)"
        },
        dom: "<'row'<'col-sm-6'l><'col-sm-6'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row'<'col-sm-5'i><'col-sm-7'p>>",
        order: [[0, 'desc'], [1, 'asc']],
        pageLength: 10,
        initComplete: function() {
            // Apply custom styling to the search box
            $('.dataTables_filter input')
                .addClass('form-control-sm shadow-sm')
                .css('width', 'auto');
            
            $('.dataTables_length select').addClass('form-control-sm shadow-sm');
            
            // Add search icon to the search box
            $('.dataTables_filter label').prepend('<i class="fas fa-search text-primary mr-2"></i>');
            
            // Add animation to the table rows
            animateTableRows();
        }
    });

    // Function to animate table rows on load
    function animateTableRows() {
        $('#appointmentsTable tbody tr').each(function(index) {
            $(this).css({
                'opacity': 0,
                'transform': 'translateY(20px)'
            });
            
            setTimeout(function() {
                $('#appointmentsTable tbody tr').eq(index).css({
                    'opacity': 1,
                    'transform': 'translateY(0)',
                    'transition': 'all 0.3s ease'
                });
            }, 100 * (index + 1));
        });
    }

    // Add animation to stats cards
    $('.col-xl-3').each(function(index) {
        $(this).css({
            'opacity': 0,
            'transform': 'translateY(20px)'
        });
        
        setTimeout(function() {
            $('.col-xl-3').eq(index).css({
                'opacity': 1,
                'transform': 'translateY(0)',
                'transition': 'all 0.4s ease'
            });
        }, 200 * (index + 1));
    });

    // Enable tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // Add card transition effects
    $('.card').hover(
        function() {
            $(this).find('.card-body').css('background-color', '#f8f9fc');
        },
        function() {
            $(this).find('.card-body').css('background-color', '#ffffff');
        }
    );

    // Filter appointments by status
    $('#statusFilter li a').on('click', function() {
        var status = $(this).attr('data-status');
        $('#currentStatusFilter').text($(this).text());
        
        if (status === 'all') {
            appointmentsTable.column(5).search('').draw();
        } else {
            appointmentsTable.column(5).search(status).draw();
        }
    });

    // Filter appointments by date range
    $('#dateFilter li a').on('click', function() {
        var range = $(this).attr('data-range');
        $('#currentDateFilter').text($(this).text());
        
        var today = new Date();
        var startDate = new Date(today);
        var endDate = new Date(today);
        
        switch(range) {
            case 'today':
                // Already set to today
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
                // Reset custom filtering
                appointmentsTable.draw();
                return;
        }
        
        // Format dates for comparison (YYYY-MM-DD)
        var formattedStartDate = formatDate(startDate);
        var formattedEndDate = formatDate(endDate);
        
        // Custom filtering function for date range
        $.fn.dataTable.ext.search.push(
            function(settings, data, dataIndex) {
                var appointmentDate = data[0]; // The date is in the first column
                
                // Extract the date value (format: "MMM dd, yyyy")
                var dateParts = appointmentDate.replace(/<[^>]*>/g, '').trim().split(',');
                if (dateParts.length < 2) return true; // If can't parse, include the row
                
                var monthDay = dateParts[0].trim().split(' ');
                var month = getMonthNumber(monthDay[0]);
                var day = parseInt(monthDay[1]);
                var year = parseInt(dateParts[1].trim());
                
                var date = new Date(year, month, day);
                
                // Return whether the date is in range
                return (
                    (formattedStartDate === '' || date >= startDate) && 
                    (formattedEndDate === '' || date <= endDate)
                );
            }
        );
        
        appointmentsTable.draw();
    });

    // Helper function to get month number from string
    function getMonthNumber(monthStr) {
        var months = {
            'Jan': 0, 'Feb': 1, 'Mar': 2, 'Apr': 3,
            'May': 4, 'Jun': 5, 'Jul': 6, 'Aug': 7,
            'Sep': 8, 'Oct': 9, 'Nov': 10, 'Dec': 11
        };
        return months[monthStr];
    }

    // Helper function to format date as YYYY-MM-DD
    function formatDate(date) {
        var d = new Date(date),
            month = '' + (d.getMonth() + 1),
            day = '' + d.getDate(),
            year = d.getFullYear();

        if (month.length < 2) 
            month = '0' + month;
        if (day.length < 2) 
            day = '0' + day;

        return [year, month, day].join('-');
    }

    // Add active class to sidebar item
    $(".nav-item a").each(function() {
        if ($(this).attr("href") === window.location.pathname) {
            $(this).addClass("active");
            $(this).closest(".nav-item").addClass("active");
            
            // If inside a collapse, open it
            if ($(this).closest(".collapse").length) {
                var collapseId = $(this).closest(".collapse").attr("id");
                $(`[data-toggle="collapse"][data-target="#${collapseId}"]`).removeClass("collapsed");
                $(this).closest(".collapse").addClass("show");
            }
        }
    });

    // Confirmation for appointment actions
    $('.confirm-action').on('click', function(e) {
        e.preventDefault();
        var actionUrl = $(this).attr('href');
        var actionType = $(this).data('action-type');
        var appointmentId = $(this).data('appointment-id');
        
        // Set modal content based on action type
        var modalTitle, modalBody, confirmBtnClass, confirmBtnText;
        
        switch(actionType) {
            case 'cancel':
                modalTitle = 'Cancel Appointment';
                modalBody = 'Are you sure you want to cancel this appointment? This action cannot be undone.';
                confirmBtnClass = 'btn-danger';
                confirmBtnText = 'Cancel Appointment';
                break;
            case 'complete':
                modalTitle = 'Complete Appointment';
                modalBody = 'Do you want to mark this appointment as completed?';
                confirmBtnClass = 'btn-success';
                confirmBtnText = 'Mark as Completed';
                break;
            case 'reschedule':
                modalTitle = 'Reschedule Appointment';
                modalBody = 'Do you want to reschedule this appointment?';
                confirmBtnClass = 'btn-warning';
                confirmBtnText = 'Reschedule';
                break;
            default:
                modalTitle = 'Confirm Action';
                modalBody = 'Are you sure you want to proceed with this action?';
                confirmBtnClass = 'btn-primary';
                confirmBtnText = 'Confirm';
        }
        
        // Populate modal
        $('#actionConfirmationModal .modal-title').text(modalTitle);
        $('#actionConfirmationModal .modal-body p').text(modalBody);
        $('#confirmActionBtn')
            .removeClass('btn-primary btn-danger btn-success btn-warning')
            .addClass(confirmBtnClass)
            .text(confirmBtnText)
            .attr('data-url', actionUrl);
        
        // Show modal
        $('#actionConfirmationModal').modal('show');
    });
    
    // Handle confirm button click
    $('#confirmActionBtn').on('click', function() {
        var actionUrl = $(this).attr('data-url');
        window.location.href = actionUrl;
    });
});

// Time countdown function for upcoming appointments
function updateCountdowns() {
    $('.countdown').each(function() {
        var appointmentTime = new Date($(this).data('appointment-time')).getTime();
        var now = new Date().getTime();
        var distance = appointmentTime - now;
        
        if (distance < 0) {
            $(this).html('<span class="text-danger">Time passed</span>');
            return;
        }
        
        var days = Math.floor(distance / (1000 * 60 * 60 * 24));
        var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        
        var countdownText = '';
        
        if (days > 0) {
            countdownText += days + 'd ';
        }
        
        countdownText += hours + 'h ' + minutes + 'm';
        $(this).text(countdownText);
        
        // Add urgency classes
        if (distance < (24 * 60 * 60 * 1000)) { // Less than 24 hours
            $(this).removeClass('text-primary text-success text-danger').addClass('text-warning');
        } else if (distance < (3 * 24 * 60 * 60 * 1000)) { // Less than 3 days
            $(this).removeClass('text-primary text-danger text-warning').addClass('text-success');
        } else {
            $(this).removeClass('text-success text-danger text-warning').addClass('text-primary');
        }
    });
}

// Update countdowns on page load and every minute
$(document).ready(function() {
    updateCountdowns();
    setInterval(updateCountdowns, 60000);
}); 