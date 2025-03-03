using DentalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Patient patient, string password, string email, string username)
        {
            if (!ModelState.IsValid)
                return View(patient);

            // 创建 User 账号
            var user = new User
            {
                Email = email,
                Username = username,
                Password = password, // 这里应该加密密码
                Role = UserRole.Patient
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 创建 Patient 记录
            patient.UserID = user.Id;
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Patient");
        }
    }
}
