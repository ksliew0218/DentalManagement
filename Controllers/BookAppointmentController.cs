using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class BookAppointmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public BookAppointmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: BookAppointment/ChooseTreatment
    public IActionResult ChooseTreatment()
    {
        var treatments = _context.TreatmentTypes
            .Where(t => t.IsActive && !t.IsDeleted)
            .ToList();

        if (!treatments.Any())
        {
            TempData["Error"] = "No active treatments available.";
            return RedirectToAction("Index", "Home");
        }

        return View(treatments);
    }

    // GET: BookAppointment/GetDoctorsByTreatment
    [HttpGet]
    public IActionResult GetDoctorsByTreatment(int treatmentId)
    {
        var doctors = _context.DoctorTreatments
            .Where(dt => dt.TreatmentTypeId == treatmentId && !dt.IsDeleted && dt.IsActive)
            .Select(dt => new
            {
                dt.Doctor.Id,
                Name = dt.Doctor.FirstName + " " + dt.Doctor.LastName,
                Specialty = dt.Doctor.Specialty
            }).ToList();

        if (!doctors.Any())
        {
            return Json(new { message = "No doctors available for this treatment." });
        }

        return Json(doctors);
    }
    [HttpGet]
    public IActionResult Create(int treatmentId, int doctorId)
    {
        var treatment = _context.TreatmentTypes.Find(treatmentId);
        var doctor = _context.Doctors.Find(doctorId);

        if (treatment == null || doctor == null)
            return NotFound();

        ViewBag.Patients = _context.Patients.Where(p => !p.IsDeleted).ToList();

        // ✅ Use UtcNow instead of Now
        ViewBag.TimeSlots = _context.TimeSlots
            .Where(ts => ts.DoctorId == doctorId && !ts.IsBooked && ts.StartTime > DateTime.UtcNow)
            .OrderBy(ts => ts.StartTime)
            .ToList();

        var appointment = new Appointment
        {
            TreatmentTypeId = treatmentId,
            DoctorId = doctorId,
            Duration = treatment.Duration,
            TotalAmount = treatment.Price
        };

        return View(appointment);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Appointment appointment, int TimeSlotId)
    {
        if (ModelState.IsValid)
        {
            var selectedSlot = _context.TimeSlots.FirstOrDefault(ts => ts.Id == TimeSlotId);

            if (selectedSlot == null || selectedSlot.IsBooked)
            {
                ModelState.AddModelError("", "Selected timeslot is no longer available.");
            }
            else
            {
                appointment.AppointmentDate = selectedSlot.StartTime.Date;
                appointment.AppointmentTime = selectedSlot.StartTime.TimeOfDay;
                appointment.CreatedAt = DateTime.UtcNow;
                appointment.Status = "Scheduled"; // <-- Add this line


                _context.Appointments.Add(appointment);
                _context.SaveChanges();

                selectedSlot.IsBooked = true;
                selectedSlot.AppointmentId = appointment.Id;
                _context.SaveChanges();
                // Add a success message to TempData
            TempData["SuccessMessage"] = "Appointment successfully booked.";
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        ViewBag.Patients = _context.Patients.Where(p => !p.IsDeleted).ToList();

        // ✅ Use UtcNow here too
        ViewBag.TimeSlots = _context.TimeSlots
            .Where(ts => ts.DoctorId == appointment.DoctorId && !ts.IsBooked && ts.StartTime > DateTime.UtcNow)
            .OrderBy(ts => ts.StartTime)
            .ToList();

        return View(appointment);
    }


}
