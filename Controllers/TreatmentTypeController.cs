using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Controllers
{
    public class TreatmentTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TreatmentTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TreatmentType/Index
        public async Task<IActionResult> Index()
        {
            var treatmentTypes = await _context.TreatmentTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(treatmentTypes);
        }

        // GET: TreatmentType/Create
        public IActionResult Create()
        {
            // Improved doctor selection with full name display
            ViewData["DoctorIds"] = new MultiSelectList(
                _context.Doctors
                    .Where(d => d.Status == StatusType.Active)
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .Select(d => new 
                    { 
                        d.Id, 
                        FullName = $"Dr. {d.FirstName} {d.LastName} ({d.Specialty})" 
                    }),
                "Id", 
                "FullName");
            
            return View();
        }

        // POST: TreatmentType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TreatmentType treatmentType, int[] DoctorIds)
        {
            if (ModelState.IsValid)
            {
                treatmentType.CreatedAt = DateTime.UtcNow;
                treatmentType.UpdatedAt = DateTime.UtcNow;
                _context.Add(treatmentType);
                await _context.SaveChangesAsync();

                // Link doctors to the treatment
                if (DoctorIds != null && DoctorIds.Length > 0)
                {
                    foreach (var doctorId in DoctorIds)
                    {
                        _context.DoctorTreatments.Add(new DoctorTreatment
                        {
                            DoctorId = doctorId,
                            TreatmentTypeId = treatmentType.Id
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Treatment '{treatmentType.Name}' was created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            ViewData["DoctorIds"] = new MultiSelectList(
                _context.Doctors
                    .Where(d => d.Status == StatusType.Active)
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .Select(d => new 
                    { 
                        d.Id, 
                        FullName = $"Dr. {d.FirstName} {d.LastName} ({d.Specialty})" 
                    }),
                "Id", 
                "FullName");
                
            return View(treatmentType);
        }
    }
}