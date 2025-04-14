using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DentalManagement.Authorization;
using DentalManagement.ViewModels;

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

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                var treatments = await _context.TreatmentTypes
                    .Where(t => !t.IsDeleted) 
                    .Include(t => t.DoctorTreatments)
                    .ToListAsync();
                
                foreach (var treatment in treatments)
                {
                    treatment.DoctorTreatments = treatment.DoctorTreatments
                        .Where(dt => dt.IsActive && !dt.IsDeleted)
                        .ToList();
                }
                    
                return treatments != null ? 
                    View(treatments) :
                    Problem("Entity set 'ApplicationDbContext.TreatmentTypes' is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading treatment types");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

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
                    .Include(t => t.DoctorTreatments.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .ThenInclude(dt => dt.Doctor)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                    
                if (treatmentType == null)
                {
                    return NotFound();
                }

                var treatmentHistory = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.TreatmentTypeId == id && a.Status == "Completed")
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentTime)
                    .Take(10) 
                    .Select(a => new TreatmentHistoryViewModel
                    {
                        AppointmentId = a.Id,
                        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        DoctorName = $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName}",
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime,
                        Notes = a.Notes
                    })
                    .ToListAsync();

                ViewData["TreatmentHistory"] = treatmentHistory;

                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while viewing treatment type details");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public IActionResult Create()
        {
            try
            {
                ViewData["DoctorIds"] = new MultiSelectList(
                    _context.Doctors
                        .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TreatmentType treatmentType, int[] DoctorIds, IFormFile ImageFile)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                ModelState.Remove("ImageFile");

                if (ModelState.IsValid)
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        try
                        {
                            string imageUrl = await SaveImageFile(ImageFile);
                            if (imageUrl != null)
                            {
                                treatmentType.ImageUrl = imageUrl;
                            }
                            else
                            {
                                _logger.LogWarning("Failed to save the image, but continuing with other updates");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image, but continuing with other updates");
                        }
                    }
                    
                    treatmentType.CreatedAt = DateTime.UtcNow;
                    treatmentType.UpdatedAt = DateTime.UtcNow;
                    _context.Add(treatmentType);
                    await _context.SaveChangesAsync();

                    if (DoctorIds != null && DoctorIds.Length > 0)
                    {
                        foreach (var doctorId in DoctorIds)
                        {
                            _context.DoctorTreatments.Add(new DoctorTreatment
                            {
                                DoctorId = doctorId,
                                TreatmentTypeId = treatmentType.Id,
                                IsActive = true,
                                IsDeleted = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Treatment '{treatmentType.Name}' was created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["DoctorIds"] = new MultiSelectList(
                    _context.Doctors
                        .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
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

                var treatmentType = await _context.TreatmentTypes
                    .Include(t => t.DoctorTreatments.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .ThenInclude(dt => dt.Doctor)
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
                    
                if (treatmentType == null)
                {
                    return NotFound();
                }
                
                var assignedDoctorIds = treatmentType.DoctorTreatments
                    .Where(dt => dt.IsActive && !dt.IsDeleted)
                    .Select(dt => dt.DoctorId)
                    .ToList();
                
                ViewData["DoctorIds"] = new MultiSelectList(
                    _context.Doctors
                        .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                        .OrderBy(d => d.LastName)
                        .ThenBy(d => d.FirstName)
                        .Select(d => new 
                        { 
                            d.Id, 
                            FullName = $"Dr. {d.FirstName} {d.LastName} ({d.Specialty})" 
                        }),
                    "Id", 
                    "FullName",
                    assignedDoctorIds);
                
                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit treatment type page");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Duration,IsActive,IsDeleted,ImageUrl")] TreatmentType treatmentType, int[] DoctorIds, IFormFile ImageFile, bool RemoveImage = false)
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

                if (DoctorIds == null)
                {
                    DoctorIds = Array.Empty<int>();
                }

                ModelState.Remove("ImageFile");

                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingTreatment = await _context.TreatmentTypes
                            .Include(t => t.DoctorTreatments)
                            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
                            
                        if (existingTreatment == null)
                        {
                            return NotFound();
                        }
                        
                        if (RemoveImage)
                        {
                            if (!string.IsNullOrEmpty(existingTreatment.ImageUrl))
                            {
                                await SaveImageFile(null, existingTreatment.ImageUrl);
                                existingTreatment.ImageUrl = null;
                                _logger.LogInformation("Image removed successfully");
                            }
                        }
                        else if (ImageFile != null && ImageFile.Length > 0)
                        {
                            try
                            {
                                string imageUrl = await SaveImageFile(ImageFile, null); 
                                if (imageUrl != null)
                                {
                                    existingTreatment.ImageUrl = imageUrl;
                                    _logger.LogInformation($"New image uploaded: {imageUrl}");
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to save the image, but continuing with other updates");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error uploading image, but continuing with other updates");
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No new image provided, keeping existing image");
                        }
                        
                        existingTreatment.Name = treatmentType.Name;
                        existingTreatment.Description = treatmentType.Description;
                        existingTreatment.Price = treatmentType.Price;
                        existingTreatment.Duration = treatmentType.Duration;
                        existingTreatment.IsActive = treatmentType.IsActive;
                        existingTreatment.IsDeleted = treatmentType.IsDeleted;
                        existingTreatment.UpdatedAt = DateTime.UtcNow;
                        
                        foreach (var doctorTreatment in existingTreatment.DoctorTreatments)
                        {
                            if (!DoctorIds.Contains(doctorTreatment.DoctorId) && doctorTreatment.IsActive && !doctorTreatment.IsDeleted)
                            {
                                doctorTreatment.IsActive = false;
                                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation($"Deactivated doctor {doctorTreatment.DoctorId} from treatment {existingTreatment.Id}");
                            }
                            else if (DoctorIds.Contains(doctorTreatment.DoctorId) && (!doctorTreatment.IsActive || doctorTreatment.IsDeleted))
                            {
                                doctorTreatment.IsActive = true;
                                doctorTreatment.IsDeleted = false;
                                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation($"Reactivated doctor {doctorTreatment.DoctorId} for treatment {existingTreatment.Id}");
                            }
                        }
                        
                        foreach (var doctorId in DoctorIds)
                        {
                            var existingAssignment = existingTreatment.DoctorTreatments
                                .FirstOrDefault(dt => dt.DoctorId == doctorId);
                                
                            if (existingAssignment == null)
                            {
                                _context.DoctorTreatments.Add(new DoctorTreatment
                                {
                                    DoctorId = doctorId,
                                    TreatmentTypeId = existingTreatment.Id,
                                    IsActive = true,
                                    IsDeleted = false,
                                    CreatedAt = DateTime.UtcNow
                                });
                                _logger.LogInformation($"Added new doctor {doctorId} to treatment {existingTreatment.Id}");
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Treatment '{existingTreatment.Name}' was updated successfully!";
                        return RedirectToAction(nameof(Index));
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
                }
                
                ViewData["DoctorIds"] = new MultiSelectList(
                    _context.Doctors
                        .Where(d => d.Status == StatusType.Active && !d.IsDeleted)
                        .OrderBy(d => d.LastName)
                        .ThenBy(d => d.FirstName)
                        .Select(d => new 
                        { 
                            d.Id, 
                            FullName = $"Dr. {d.FirstName} {d.LastName} ({d.Specialty})" 
                        }),
                    "Id", 
                    "FullName",
                    DoctorIds);
                    
                return View(treatmentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing treatment type");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

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
                    .Include(t => t.DoctorTreatments.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .ThenInclude(dt => dt.Doctor)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                    
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
                
                var treatmentType = await _context.TreatmentTypes
                    .Include(t => t.DoctorTreatments)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
                    
                if (treatmentType != null)
                {
                    foreach (var doctorTreatment in treatmentType.DoctorTreatments)
                    {
                        doctorTreatment.IsActive = false;
                        doctorTreatment.IsDeleted = true;
                        doctorTreatment.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    treatmentType.IsActive = false;
                    treatmentType.IsDeleted = true;
                    treatmentType.UpdatedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Treatment '{treatmentType.Name}' was deleted successfully!";
                }
                
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
          return (_context.TreatmentTypes?.Any(e => e.Id == id && !e.IsDeleted)).GetValueOrDefault();
        }
        
        private async Task<string> SaveImageFile(IFormFile imageFile, string oldImageUrl = null)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        
                        string oldImageRelativePath = oldImageUrl.TrimStart('/');
                        string oldImageFullPath = Path.Combine(webRootPath, oldImageRelativePath);
                        
                        _logger.LogInformation($"Looking for old image at: {oldImageFullPath}");
                        
                        if (System.IO.File.Exists(oldImageFullPath))
                        {
                            _logger.LogInformation($"Deleting old image at: {oldImageFullPath}");
                            System.IO.File.Delete(oldImageFullPath);
                        }
                        else
                        {
                            _logger.LogWarning($"Old image not found at: {oldImageFullPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error deleting old image: {oldImageUrl}");
                    }
                }
                
                return null;
            }

            try
            {
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsRelativePath = "images/treatments";
                string uploadsFolder = Path.Combine(webRootPath, uploadsRelativePath);
                
                _logger.LogInformation($"Full path to web root: {webRootPath}");
                _logger.LogInformation($"Full path to uploads folder: {uploadsFolder}");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation($"Creating directory structure for: {uploadsFolder}");
                    try {
                        Directory.CreateDirectory(uploadsFolder);
                        
                        if (!Directory.Exists(uploadsFolder)) {
                            _logger.LogError($"Failed to create directory: {uploadsFolder}");
                            return null;
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, $"Error creating directory {uploadsFolder}: {ex.Message}");
                        return null;
                    }
                }
                
                string originalFileName = Path.GetFileName(imageFile.FileName);
                string safeFileName = new string(originalFileName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                string uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                _logger.LogInformation($"Saving image to: {filePath}");
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                    fileStream.Flush();  
                }
                
                if (System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation($"File successfully saved at: {filePath}");
                    return $"/{uploadsRelativePath.Replace('\\', '/')}/{uniqueFileName}";
                }
                else
                {
                    _logger.LogError($"File not found after save attempt: {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving image file: {ex.Message}");
                return null;
            }
        }
    }
}