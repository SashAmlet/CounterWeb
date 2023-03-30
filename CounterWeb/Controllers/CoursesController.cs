using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterWeb.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;
using FluentAssertions.Equivalency;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using ClosedXML.Excel;

namespace CounterWeb.Controllers
{
    public class CoursesController : Controller
    {
        private readonly CounterDbContext _context;
        private readonly IdentityContext idContext;
        private readonly UserManager<UserIdentity> userManager;

        public CoursesController(CounterDbContext context, IdentityContext idContext, UserManager<UserIdentity> userManager)
        {
            _context = context;
            this.idContext = idContext;
            this.userManager = userManager;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            var courses = await (from uc in _context.UserCourses
                          join c in _context.Courses on uc.CourseId equals c.CourseId
                          where uc.UserId == currentUser.UserId
                          select c).ToListAsync();
            return _context.Courses != null ?
                          View(courses) :
                          Problem("Entity set 'CounterDbContext.Courses'  is null.");
        }

        // GET: Courses/CREATE
        [Authorize(Roles = "teacher, admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/CREATE
        [Authorize(Roles = "teacher, admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,Name,ZoomLink")] Course course)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await userManager.GetUserAsync(HttpContext.User);
                var user = _context.Users.Where(b => b.UserId == currentUser.UserId).FirstOrDefault();

                var _userCourse = new UserCourse();
                _userCourse.CourseId = course.CourseId;
                _userCourse.Course = course;
                _userCourse.UserId = user.UserId;
                
                _userCourse.User = user;
                currentUser.UserCourses.Add(_userCourse);
                course.UserCourses.Add(_userCourse);

                _context.Add(course);
                _context.Add(_userCourse);
                await _context.SaveChangesAsync();
                //await userManager.UpdateAsync(currentUser);
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }
        // GET: Courses/Tasks/5
        public async Task<IActionResult> Tasks(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return RedirectToAction("index", "Tasks", new { id = course.CourseId, name = course.Name });
            //return View(course);
        }

        // GET: Courses/EDIT
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/EDIT
        [Authorize(Roles = "teacher, admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Name,ZoomLink")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    course.Tasks = await _context.Tasks.Where(b=>b.CourseId == id).Include(b=>b.CompletedTasks).ToListAsync();
                    course.UserCourses = await _context.UserCourses.Where(b=>b.CourseId==id).ToListAsync();
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
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
            return View(course);
        }

        // GET: Courses/SHOW
        public async Task<IActionResult> Show(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var teachers = await userManager.GetUsersInRoleAsync("teacher");
            var students = await userManager.GetUsersInRoleAsync("student");

            var teacherIds = teachers.Select(t => t.UserId).ToList();
            var studentIds = students.Select(t => t.UserId).ToList();

            var teachList = await _context.UserCourses.Where(b => teacherIds.Contains(b.UserId.Value)).Where(b=>b.CourseId == id).Select(b => b.User).ToListAsync();
            var studList = await _context.UserCourses.Where(b => studentIds.Contains(b.UserId.Value)).Where(b => b.CourseId == id).Select(b => b.User).ToListAsync();
            var taskList = await _context.Tasks.Where(c=>c.CourseId == id).Include(a=>a.CompletedTasks).ToListAsync();
            if (teachList == null && studList == null)
            {
                return NotFound();
            }
            ViewBag.Id = id;
            var userCourse = await _context.UserCourses
                                                        .Where(c => c.CourseId == id)
                                                        .Select(c => new CounterWeb.Models.UserCourse
                                                        {
                                                            UserCourseId = c.UserCourseId,
                                                            UserId = c.UserId,
                                                            CourseId = c.CourseId
                                                        })
                                                        .ToListAsync();

            ViewBag.UserCourseId = userCourse;
            return View(Tuple.Create(teachList, studList, taskList));
        }

        // GET: Courses/JOIN
        public IActionResult Join()
        {
            return View();
        }

        // POST: Courses/JOIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string encodedString)
        {
            var decodedObject = Helper.ToDecode(encodedString);
            var course = _context.Courses.Where(b=>b.CourseId == decodedObject.Item1 && b.Name == decodedObject.Item2).FirstOrDefault();
            if (course == null)
            {
                return NotFound();
            }

            var currentUser = await userManager.GetUserAsync(HttpContext.User);
            var user = _context.Users.Where(b => b.UserId == currentUser.UserId).FirstOrDefault();

            var _userCourse = new UserCourse
            {
                CourseId = course.CourseId,
                Course = course,
                UserId = user.UserId,
                User = user
            };
            currentUser.UserCourses.Add(_userCourse);
            course.UserCourses.Add(_userCourse);

            _context.Update(course);
            _context.Add(_userCourse);

            await _context.SaveChangesAsync();
            //await userManager.UpdateAsync(currentUser);

            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/DELETE
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Courses == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/DELETE
        [Authorize(Roles = "teacher, admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Courses == null)
            {
                return Problem("Entity set 'CounterDbContext.Courses'  is null.");
            }
            var course = await _context.Courses.Include(b=>b.UserCourses).Include(b => b.Tasks).ThenInclude(c=>c.CompletedTasks)
                .FirstOrDefaultAsync(b => b.CourseId == id);
            if (course != null)
            {
                foreach(var task in course.Tasks)
                {
                    foreach (var ct in task.CompletedTasks)
                        _context.CompletedTasks.Remove(ct);
                    _context.Tasks.Remove(task);
                }
                foreach(var usercourse in course.UserCourses)
                    _context.Remove(usercourse);
                _context.Courses.Remove(course);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Courses/Import
        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile fileExcel)
        {
            if (ModelState.IsValid)
            {
                if (fileExcel != null)
                {
                    using (var stream = new FileStream(fileExcel.FileName, FileMode.Create))
                    {
                        await fileExcel.CopyToAsync(stream);
                        using (XLWorkbook workBook = new XLWorkbook(stream.ToString(), XLEventTracking.Disabled))
                        {
                            //перегляд усіх листів (в даному випадку категорій)
                            //зробити: запитати, чи впвнений юзер у тому, що хоче заповнити усі курси (один worksheet - один курс, назва worksheet - назва курсу)
                            foreach (IXLWorksheet worksheet in workBook.Worksheets)
                            {
                                //worksheet.Name - назва курсу. Пробуємо знайти в БД, якщо відсутня, то створюємо нову
                                Course newcourse;
                                var c = (from course in _context.Courses
                                         where course.Name.Contains(worksheet.Name)
                                         select course).ToList();
                                if (c.Count > 0)
                                {
                                    newcourse = c[0];
                                }
                                else
                                {
                                    //зробити: запитати чи хоче юзер створити новий курс, інфа з якого є в excel file, але якого нема в бд
                                    newcourse = new Course();
                                    newcourse.Name = worksheet.Name;
                                    newcourse.ZoomLink = "from EXCEL";
                                    //додати в контекст
                                    _context.Courses.Add(newcourse);
                                }
                                //перегляд усіх рядків                    
                                foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
                                {
                                    try
                                    {
                                        Book book = new Book();
                                        book.Name = row.Cell(1).Value.ToString();
                                        book.Info = row.Cell(6).Value.ToString();
                                        book.Category = newcat;
                                        _context.Books.Add(book);
                                        //у разі наявності автора знайти його, у разі відсутності - додати
                                        for (int i = 2; i <= 5; i++)
                                        {
                                            if (row.Cell(i).Value.ToString().Length > 0)
                                            {
                                                Author author;

                                                var a = (from aut in _context.Authors
                                                         where aut.Name.Contains(row.Cell(i).Value.ToString())
                                                         select aut).ToList();
                                                if (a.Count > 0)
                                                {
                                                    author = a[0];
                                                }
                                                else
                                                {
                                                    author = new Author();
                                                    author.Name = row.Cell(i).Value.ToString();
                                                    author.Info = "from EXCEL";
                                                    //додати в контекст
                                                    _context.Add(author);
                                                }
                                                AuthorBook ab = new AuthorBook();
                                                ab.Book = book;
                                                ab.Author = author;
                                                _context.AuthorBooks.Add(ab);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        //logging самостійно :)

                                    }
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }*/

        private bool CourseExists(int id)
        {
          return (_context.Courses?.Any(e => e.CourseId == id)).GetValueOrDefault();
        }


    }
}
