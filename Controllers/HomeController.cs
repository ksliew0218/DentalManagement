using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace DentalManagement.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<User> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> AdminDashboard()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
            {
                return RedirectToAction("AccessDenied");
            }
            
            ViewBag.DoctorCount = _context.Doctors.Count();
            ViewBag.PatientCount = _context.Patients.Count();
            ViewBag.TreatmentCount = _context.TreatmentTypes.Where(t => !t.IsDeleted).Count();
            
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while accessing admin dashboard");
            return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
