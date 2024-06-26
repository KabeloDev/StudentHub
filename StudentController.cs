using System.Net;
using System.Net.Mail;
using ASPNETCore_DB.Areas.Identity.Pages.Account;
using ASPNETCore_DB.Interfaces;
using ASPNETCore_DB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCore_DB.Controllers
{
    [TypeFilter(typeof(CustomExceptionFilter))]
    public class StudentController : Controller
    {
        private readonly IStudent _studentRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public StudentController(IStudent studentRepo, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            try
            {
                _studentRepo = studentRepo;
                _httpContextAccessor = httpContextAccessor;
                _webHostEnvironment = webHostEnvironment;
            }
            catch (Exception ex)
            {
                throw new Exception("Constructor not initialized - IStudent studentRepo");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            pageNumber = pageNumber ?? 1;
            int pageSize = 3;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["StudentNumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            ViewResult viewResult = View();

            try
            {
                viewResult = View(PaginatedList<Student>.Create(_studentRepo.GetStudents(searchString, sortOrder).AsNoTracking(), pageNumber ?? 1, pageSize));
            }
            catch (Exception ex)
            {
                throw new Exception("No student records detected");
            }

            return viewResult;
        }
        public IActionResult Details(string id)
        {
            var student = _studentRepo.Details(id);
            return View(student);
        }


        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Create()
        {
            Student student = new Student();
            string fileName = "default.PNG";
            student.Photo = fileName;
            return View(student);

            
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("StudentNumber, FirstName, Surname, EnrollmentDate")] Student student)
        {
            var files = HttpContext.Request.Form.Files;
            string webRootPath = _webHostEnvironment.WebRootPath;
            string upload = webRootPath + WebConstants.ImagePath;
            string fileName = Guid.NewGuid().ToString();
            string extension = Path.GetExtension(files[0].FileName);
            using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension),
            FileMode.Create))
            {
                files[0].CopyTo(fileStream);
            }
            student.Photo = fileName + extension;
            try
            {
                if (ModelState.IsValid)
                {
                    _studentRepo.Create(student);

                }

            }
            catch (Exception ex)
            {
                throw new Exception("Student record not saved.");
            }

            //var studentExist = _studentRepo.ByStudentNumber(this.User.Identity.ToString());

            //if (studentExist != null)
            //{
            //    return RedirectToAction("Details", "Student", new { id =  studentExist.StudentNumber });
            //}
            //else
            //{
            //    return RedirectToAction("Create");
            //}

            return RedirectToAction("Details", new { id = student.StudentNumber});
        }


        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Edit(string id)
        {

            ViewResult viewDetail = View();
            try
            {
                viewDetail = View(_studentRepo.Details(id));
            }
            catch (Exception ex)
            {
                throw new Exception("Student detail not found");
            }
            return viewDetail;
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Student student)
        {
           
            string photoName = "DefaultPic.png";

            if (HttpContext.Request.Form.Files.Count > 0)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;
                string upload = webRootPath + WebConstants.ImagePath;
                string fileName = Guid.NewGuid().ToString();
                string extension = Path.GetExtension(files[0].FileName);
                var oldFile = Path.Combine(upload, photoName);
                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }
                using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension),
                FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                student.Photo = fileName + extension;
            }
            else
            {
               
                student.Photo = photoName;
            }
            try
            {
                _studentRepo.Edit(student);
            }
            catch (Exception ex)
            {
                throw new Exception("Student record not saved.");
            }
            return RedirectToAction("Details", new { id = student.StudentNumber });

        //try
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _studentRepo.Edit(student);
        //    }
        //}
        //catch (Exception ex)
        //{
        //    throw new Exception("Student detail could not be edited");
        //}

        //return RedirectToAction(nameof(Index));
    }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(string id)
        {
           
            ViewResult viewDetail = View();
            try
            {
                viewDetail = View(_studentRepo.Details(id));
            }
            catch (Exception ex)
            {
                throw new Exception("Student detail not found");
            }
            return viewDetail;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete([Bind("StudentNumber, FirstName, Surname, EnrollmentDate")] Student student)
        {
            try
            {
                _studentRepo.Delete(student);
            }
            catch (Exception ex)
            {
                throw new Exception("Student could not be deleted");
            }

            return RedirectToAction(nameof(Index));
        }

       
        [HttpGet]
        public IActionResult Contact()
        {
            return View("Contact");
        }

        [HttpPost]
        public IActionResult Contact(string name, string email, string emailMessage)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtpClient = new SmtpClient();
            message.From = new MailAddress(email);
            message.To.Add("kaebigs18@gmail.com");
            message.Subject = "BFN Campus Assignment 1 – Code Crafters";
            message.IsBodyHtml = true;
            message.Body = "<p>Name: " + name + "</p>" + "<p>Email: " + email + "</p>" + "<p>Message: " + emailMessage + "</p>";

            smtpClient.Port = 587;
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential("kaebigs18@gmail.com", "peywplnfthcneews");
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Send(message);

            return View("Contact");
        }
        public IActionResult Sports()
        {
            return View("Sports");
        }
        public IActionResult IGym()
        {
            return View("IGym");
        }
        public IActionResult Library()
        {
            return View("Library");
        }
        public IActionResult Wellness()
        {
            return View("Wellness");
        }
        public IActionResult About()
        {
            return View("About");
        }
    }
}
