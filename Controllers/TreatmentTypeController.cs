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

                // Load treatments with their active doctor assignments
                var treatments = await _context.TreatmentTypes
                    .Where(t => !t.IsDeleted) // Filter out deleted treatment types
                    .Include(t => t.DoctorTreatments)
                    .ToListAsync();
                
                // For each treatment, filter the doctor treatments to only active ones
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
        public async Task<IActionResult> Create(TreatmentType treatmentType, int[] DoctorIds, IFormFile ImageFile)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != UserRole.Admin)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Remove any model validation errors for ImageFile since it's optional
                ModelState.Remove("ImageFile");

                if (ModelState.IsValid)
                {
                    // Handle image upload
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
                                // Log the error but don't prevent saving other changes
                                _logger.LogWarning("Failed to save the image, but continuing with other updates");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the error but don't prevent saving other changes
                            _logger.LogError(ex, "Error uploading image, but continuing with other updates");
                        }
                    }
                    
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

                var treatmentType = await _context.TreatmentTypes
                    .Include(t => t.DoctorTreatments.Where(dt => dt.IsActive && !dt.IsDeleted))
                    .ThenInclude(dt => dt.Doctor)
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
                    
                if (treatmentType == null)
                {
                    return NotFound();
                }
                
                // Get the IDs of doctors currently assigned to this treatment
                var assignedDoctorIds = treatmentType.DoctorTreatments
                    .Where(dt => dt.IsActive && !dt.IsDeleted)
                    .Select(dt => dt.DoctorId)
                    .ToList();
                
                // Prepare the multiselect list with all doctors
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

                // Remove any model validation errors for ImageFile since it's optional
                ModelState.Remove("ImageFile");

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Get the existing treatment from the database to update
                        var existingTreatment = await _context.TreatmentTypes
                            .Include(t => t.DoctorTreatments)
                            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
                            
                        if (existingTreatment == null)
                        {
                            return NotFound();
                        }
                        
                        // Handle image removal and upload
                        if (RemoveImage)
                        {
                            // Just remove the image, don't upload a new one yet
                            if (!string.IsNullOrEmpty(existingTreatment.ImageUrl))
                            {
                                // Delete file using the helper method (passing null as new file)
                                await SaveImageFile(null, existingTreatment.ImageUrl);
                                existingTreatment.ImageUrl = null;
                                _logger.LogInformation("Image removed successfully");
                            }
                        }
                        else if (ImageFile != null && ImageFile.Length > 0)
                        {
                            try
                            {
                                // Only upload new image if RemoveImage is false and a file is provided
                                string imageUrl = await SaveImageFile(ImageFile, null); // Pass null to avoid deleting old image
                                if (imageUrl != null)
                                {
                                    existingTreatment.ImageUrl = imageUrl;
                                    _logger.LogInformation($"New image uploaded: {imageUrl}");
                                }
                                else
                                {
                                    // Log the error but don't prevent saving other changes
                                    _logger.LogWarning("Failed to save the image, but continuing with other updates");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log the error but don't prevent saving other changes
                                _logger.LogError(ex, "Error uploading image, but continuing with other updates");
                            }
                        }
                        // If no image is provided, keep the existing one (no action needed)
                        else
                        {
                            _logger.LogInformation("No new image provided, keeping existing image");
                        }
                        
                        // Update the treatment properties
                        existingTreatment.Name = treatmentType.Name;
                        existingTreatment.Description = treatmentType.Description;
                        existingTreatment.Price = treatmentType.Price;
                        existingTreatment.Duration = treatmentType.Duration;
                        existingTreatment.IsActive = treatmentType.IsActive;
                        existingTreatment.IsDeleted = treatmentType.IsDeleted;
                        existingTreatment.UpdatedAt = DateTime.UtcNow;
                        
                        // Mark doctors as inactive if they were removed
                        foreach (var doctorTreatment in existingTreatment.DoctorTreatments)
                        {
                            // If the doctor is not in the new list and not already marked as inactive
                            if (!DoctorIds.Contains(doctorTreatment.DoctorId) && doctorTreatment.IsActive && !doctorTreatment.IsDeleted)
                            {
                                doctorTreatment.IsActive = false;
                                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation($"Deactivated doctor {doctorTreatment.DoctorId} from treatment {existingTreatment.Id}");
                            }
                            // If the doctor is in the new list but was previously inactive
                            else if (DoctorIds.Contains(doctorTreatment.DoctorId) && (!doctorTreatment.IsActive || doctorTreatment.IsDeleted))
                            {
                                doctorTreatment.IsActive = true;
                                doctorTreatment.IsDeleted = false;
                                doctorTreatment.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation($"Reactivated doctor {doctorTreatment.DoctorId} for treatment {existingTreatment.Id}");
                            }
                        }
                        
                        // Add new doctor assignments
                        foreach (var doctorId in DoctorIds)
                        {
                            // Check if this doctor is already assigned
                            var existingAssignment = existingTreatment.DoctorTreatments
                                .FirstOrDefault(dt => dt.DoctorId == doctorId);
                                
                            // If not already assigned, create a new assignment
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
                        .Where(d => d.Status == StatusType.Active)
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
                // If there's an old image to delete but no new image
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        // Get the absolute path to the wwwroot folder
                        string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        
                        // Strip off the leading '/' if present
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
                // Get the absolute path to the wwwroot folder - this approach is more reliable
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsRelativePath = "images/treatments";
                string uploadsFolder = Path.Combine(webRootPath, uploadsRelativePath);
                
                _logger.LogInformation($"Full path to web root: {webRootPath}");
                _logger.LogInformation($"Full path to uploads folder: {uploadsFolder}");
                
                // Ensure the directory exists (create full path)
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation($"Creating directory structure for: {uploadsFolder}");
                    try {
                        // Create directory and all parent directories if they don't exist
                        Directory.CreateDirectory(uploadsFolder);
                        
                        // Verify directory was created
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
                
                // Create a unique filename with sanitization
                string originalFileName = Path.GetFileName(imageFile.FileName);
                string safeFileName = new string(originalFileName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                string uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                _logger.LogInformation($"Saving image to: {filePath}");
                
                // Save file with explicit file stream
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                    fileStream.Flush();  // Ensure all data is written
                }
                
                // Verify file was saved
                if (System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation($"File successfully saved at: {filePath}");
                    // Return URL path relative to website root - use forward slashes for URLs regardless of OS
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