using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterWeb.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.AspNetCore.Authorization;
using System.Data;

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

        // GET: Courses/DETAILS
        public async Task<IActionResult> Details(int? id)
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

            return RedirectToAction("index", "Tasks", new {id = course.CourseId, name = course.Name });
            //return View(course);
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

            var teachList = await _context.UserCourses.Where(b => teacherIds.Contains(b.UserId.Value)).Select(b => b.User).ToListAsync();
            var studList = await _context.UserCourses.Where(b => studentIds.Contains(b.UserId.Value)).Select(b => b.User).ToListAsync();

            if (teachList == null && studList == null)
            {
                return NotFound();
            }
            return View(Tuple.Create(teachList, studList));
        }

        // GET: Courses/JOIN
        public IActionResult Join()
        {
            return View();
        }

        // POST: Courses/JOIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var course = _context.Courses.Where(b=>b.CourseId == id).FirstOrDefault();
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
            //TasksController tasksController = new TasksController(_context);

            if (_context.Courses == null)
            {
                return Problem("Entity set 'CounterDbContext.Courses'  is null.");
            }
            var course = await _context.Courses.Include(b => b.Tasks)
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course != null)
            {
                foreach(var task in course.Tasks)
                {
                    var compTasks = _context.CompletedTasks.Where(b => b.TaskId == task.TaskId).ToList();
                    foreach (var ct in compTasks)
                        _context.CompletedTasks.Remove(ct);
                    _context.Tasks.Remove(task);
                }
                _context.Courses.Remove(course);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
          return (_context.Courses?.Any(e => e.CourseId == id)).GetValueOrDefault();
        }
    }
}
