using System;
using System.Collections.Generic;
using System.Linq;
//using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CounterWeb.Models;
using Task = CounterWeb.Models.Task;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Exchange.WebServices.Data;

namespace CounterWeb.Controllers
{
    public class TasksController : Controller
    {
        private readonly CounterDbContext _context;
        private readonly UserManager<UserIdentity> userManager;

        public TasksController(CounterDbContext context, UserManager<UserIdentity> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }

        // GET: Tasks/INDEX
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if (id == null)
                return RedirectToAction("Courses", "Index");
            // Знаходження завдань за курсом
            var currentUser = await userManager.GetUserAsync(User);
            ViewBag.Id = id;
            ViewBag.Name = name;
            var userCourse = await _context.UserCourses.Where(b => b.CourseId == id).Where(b => b.UserId == currentUser.UserId).FirstOrDefaultAsync();
            ViewBag.userCoursseId = userCourse?.UserCourseId;
            // Load all Tasks and all related CTasks.
            var tasksByCourses = _context.Tasks.Where(b => b.CourseId == id).Include(b => b.Course).Include(b => b.CompletedTasks);
            return View(await tasksByCourses.ToListAsync());
        }

        // GET: Tasks/DETAILS
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Details(int? userCourseId, int? taskId)// userCourseId
        {
            if (userCourseId == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var ctask = await _context.CompletedTasks.Where(b => b.UserCourseId == userCourseId).Where(b=>b.TaskId == taskId).FirstOrDefaultAsync();

            if (ctask == null)
            {
                return NotFound();
            }
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Tasks.Any(t => t.TaskId == taskId));
            var usercourse = _context.UserCourses.Where(b => b.UserCourseId == userCourseId).FirstOrDefault();

            ViewBag.usercourse = usercourse;
            ViewBag.task = _context.Tasks.Where(b => b.CourseId == course.CourseId).FirstOrDefault();
            //ViewBag.context = _context;


            return View(ctask);
        }

        // GET: Tasks/CREATE
        [Authorize(Roles = "teacher, admin")]
        public IActionResult Create(int courseId)
        {
            ViewBag.CourseId = courseId;
            ViewBag.CourseName = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name;
            return View();
        }

        // POST: Tasks/CREATE
        [Authorize(Roles = "teacher, admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, [Bind("TaskId,CourseId,Name,Description,MaxGrade")] Task task)
        {
            task.CourseId = courseId;
            if (ModelState.IsValid)
            {
                var course = _context.Courses.Where(_c => _c.CourseId == courseId).FirstOrDefault();
                course.Tasks.Add(task);
                task.Course = course;
                _context.Add(task);
                _context.Update(course);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Tasks", new { id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
            }
            //ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            //return View(task);
            return RedirectToAction("Index", "Tasks", new { id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
        }

        // GET: Tasks/ESTIMATE
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Estimate(int? UserCourseId, int? TaskId)// userCourseId
        {
            if (UserCourseId == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var ctask = await _context.CompletedTasks.Where(b=>b.TaskId == TaskId).Where(b => b.UserCourseId == UserCourseId).FirstOrDefaultAsync();

            if (ctask == null)
            {
                return NotFound();
            }
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Tasks.Any(t => t.TaskId == ctask.TaskId));
            var usercourse = _context.UserCourses.Where(b => b.UserCourseId == UserCourseId).FirstOrDefault();

            ViewBag.usercourse = usercourse;
            ViewBag.task = _context.Tasks.Where(b => b.CourseId == course.CourseId).FirstOrDefault();
            //ViewBag.context = _context;


            return View(ctask);
        }
        [Authorize(Roles = "teacher, admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        // POST: Tasks/ESTIMATE
        public async Task<IActionResult> Estimate(int taskId, [Bind("CompletedTaskId, TaskId,Grade,Solution,UserCourseId")] CompletedTask task)
        {

            if (ModelState.IsValid)
            {
                /*var course = _context.Courses.Where(_c => _c.CourseId == courseId).FirstOrDefault();
                course.Tasks.Add(task);
                task.Course = course;
                _context.Add(task);*/
                _context.Update(task);
                await _context.SaveChangesAsync();
                //return RedirectToAction("Index", "Tasks", new {id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
            }
            //ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            //return View(task);
            return RedirectToAction("CompletedList", new { id = taskId});
        }

        // GET: Tasks/EDIT
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.Include(b => b.CompletedTasks)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }
            else if(task.CompletedTasks.Count == 0)
            {
                task.CompletedTasks.Add(new CompletedTask());
                task.CompletedTasks.ToList()[0].TaskId = task.TaskId;
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            task.Course = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault();
            return View(task);
        }

        // POST: Tasks/EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Edit(int taskId, [Bind("TaskId,CourseId,Name,Description,MaxGrade")] Task task, ICollection<CompletedTask> completedTasks)
        {
            if (taskId != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    completedTasks.ToList()[0].UserCourseId = 0;
                    completedTasks.ToList()[0].Task = task;
                    task.CompletedTasks = completedTasks;
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Tasks", new { id = task.CourseId, name = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault().Name });
                //RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            return View(task);
        }
        
        // GET: Tasks/COMPLETE
        [Authorize(Roles = "student, admin")]
        public async Task<IActionResult> Complete(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.Include(b => b.CompletedTasks)
                .FirstOrDefaultAsync(m => m.TaskId == id);

            if (task == null)
            {
                return NotFound();
            }

            var currentUser = await userManager.GetUserAsync(HttpContext.User);

            if (task.CompletedTasks.Where(b=>b.UserCourseId == currentUser.UserId).FirstOrDefault() == null)
            {
                var usercourse = await _context.UserCourses.Where(b => b.UserId == currentUser.UserId).Where(b => b.CourseId == task.CourseId).FirstOrDefaultAsync();
                var ctask = new CompletedTask
                {
                    Task = task,
                    TaskId = task.TaskId,
                    UserCourseId = usercourse.UserCourseId
                };
                ViewBag.usercourse = usercourse;
                task.CompletedTasks.Add(ctask);
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            task.Course = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault();
            ViewBag.userManager = userManager;
            return View(task);
        }

        // POST: Tasks/COMPLETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "student, admin")]
        public async Task<IActionResult> Complete(int taskId, [Bind("TaskId,CourseId,Name,Description,MaxGrade")] Task task, ICollection<CompletedTask> completedTasks)
        {
            if (taskId != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await userManager.GetUserAsync(User);
                    var usercourse = await _context.UserCourses.Where(b => b.UserId == currentUser.UserId).Where(b => b.CourseId == task.CourseId).FirstOrDefaultAsync();
                    var ctask = completedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).FirstOrDefault();
                    ctask.Task = task;
                    task.CompletedTasks = completedTasks;
                    _context.Update(task);
                    if(_context.CompletedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).Where(b=>b.TaskId == ctask.TaskId).FirstOrDefault() == null)
                        _context.Add(completedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).Where(b => b.TaskId == ctask.TaskId).FirstOrDefault());
                    else
                        _context.Update(completedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).Where(b => b.TaskId == ctask.TaskId).FirstOrDefault());

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Tasks", new { id = task.CourseId, name = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault().Name });
                //RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            return View(task);
        }
        
        // GET: Tasks/COMPLETEDLIST
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> CompletedList(int? id)//taskId
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }


            var ctaskList = await _context.CompletedTasks.Where(b=>b.TaskId == id).ToListAsync();

            if (ctaskList == null)
            {
                return NotFound();
            }

            var userCourseList =  (
                from completedTask in ctaskList
                join userCourse in _context.UserCourses on completedTask.UserCourseId equals userCourse.UserCourseId
                select userCourse
            ).ToList();
            // юзери, що виконали дз
            var userList = (
                from userCourse in userCourseList
                join user in _context.Users on userCourse.UserId equals user.UserId
                select user
            ).ToList();

			// дістаю курс в якому зараз працюю
			var course = await (
	            from t in _context.Tasks
	            join c in _context.Courses on t.CourseId equals c.CourseId
            	where t.TaskId == id
            	select c
            ).FirstOrDefaultAsync();

            // дістаю усі UserCourses для мого курсу
            var usercourses = await _context.UserCourses.Where(b => b.CourseId == course.CourseId).ToListAsync();
			// дістаю усі UserId з тих UserCourses, по факту це і є усі id-шники юзерів у моєму курсі
			var userIds = usercourses.Select(c => c.UserId).ToList();
            // дістаю усіх студентів
            var allStudents = await userManager.GetUsersInRoleAsync("student");
            // дістаю усіх студентів конкретного курсу
            var studentIds = allStudents.Where(b=>userIds.Contains(b.UserId)).Select(t => t.UserId).ToList();

            var foolIds =
                (
                from studId in studentIds
                where !userIds.Contains(studId)
                select studId
                ).ToList(); // усі UserId, що не зробили домашку
               

            var foolList = _context.Users.Where(b=> foolIds.Contains(b.UserId)).ToList();




            ViewBag.userList = userList;
            ViewBag.ctaskList = ctaskList;
            ViewBag.foolList = foolList;
            return View(userCourseList);
        }

        // GET: Tasks/DELETE
        [Authorize(Roles = "teacher, admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Course).Include(b => b.CompletedTasks)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/DELETE
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "teacher, admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tasks == null)
            {
                return Problem("Entity set 'CounterDbContext.Tasks'  is null.");
            }
            var task = await _context.Tasks.Include(b => b.CompletedTasks)
                .FirstOrDefaultAsync(m => m.TaskId == id);

            int CourseId = 0;
            if (task != null)
            {
                CourseId = task.CourseId;
                foreach (var c in task.CompletedTasks)
                    _context.CompletedTasks.Remove(c);
                _context.Tasks.Remove(task);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { id = CourseId });
        }

        private bool TaskExists(int id)
        {
          return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }
    }
}
