using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DentalManagement.Authorization;

namespace DentalManagement.Controllers
{
    [Authorize]
    [AdminOnly]
    public class TreatmentTypeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TreatmentTypeController> _logger;
        private readonly UserManager<User> _userManager;

        public TreatmentTypeController(
            ApplicationDbContext context, 
            ILogger<TreatmentTypeController> logger,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: TreatmentType
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                return _context.TreatmentTypes != null ? 
                    View(await _context.TreatmentTypes.ToListAsync()) :
                    Problem("Entity set 'ApplicationDbContext.TreatmentTypes' is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading treatment types");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TreatmentType/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (id == null || _context.TreatmentTypes == null)
                {
                    return NotFound();
                }

                var treatmentType = await _context.TreatmentTypes
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (treatmentType == null)
                {
                    return NotFound();
                }

                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while viewing treatment type details");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TreatmentType/Create
        public IActionResult Create()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create treatment type page");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TreatmentType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TreatmentType treatmentType, int[] DoctorIds)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating treatment type");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TreatmentType/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (id == null || _context.TreatmentTypes == null)
                {
                    return NotFound();
                }

                var treatmentType = await _context.TreatmentTypes.FindAsync(id);
                if (treatmentType == null)
                {
                    return NotFound();
                }
                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit treatment type page");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TreatmentType/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Cost")] TreatmentType treatmentType)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (id != treatmentType.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(treatmentType);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!TreatmentTypeExists(treatmentType.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing treatment type");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TreatmentType/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (id == null || _context.TreatmentTypes == null)
                {
                    return NotFound();
                }

                var treatmentType = await _context.TreatmentTypes
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (treatmentType == null)
                {
                    return NotFound();
                }

                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading delete treatment type page");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TreatmentType/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                if (_context.TreatmentTypes == null)
                {
                    return Problem("Entity set 'ApplicationDbContext.TreatmentTypes' is null.");
                }
                var treatmentType = await _context.TreatmentTypes.FindAsync(id);
                if (treatmentType != null)
                {
                    _context.TreatmentTypes.Remove(treatmentType);
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting treatment type");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        private bool TreatmentTypeExists(int id)
        {
          return (_context.TreatmentTypes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}