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

        // GET: Tasks
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if (id == null)
                return RedirectToAction("Courses", "Index");
            // Знаходження завдань за курсом
            ViewBag.Id = id;
            ViewBag.Name = name;
            // Load all Tasks and all related CTasks.
            var tasksByCourses = _context.Tasks.Where(b => b.CourseId == id).Include(b => b.Course).Include(b => b.CompletedTasks);
            return View(await tasksByCourses.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)// userCourseId
        {
            if (id == null || _context.Tasks == null)
            {
                return NotFound();
            }

            var ctask = await _context.CompletedTasks.Where(b => b.UserCourseId == id).FirstOrDefaultAsync();

            if (ctask == null)
            {
                return NotFound();
            }
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Tasks.Any(t => t.TaskId == ctask.TaskId));
            var usercourse = _context.UserCourses.Where(b => b.UserCourseId == id).FirstOrDefault();

            ViewBag.usercourse = usercourse;
            ViewBag.task = _context.Tasks.Where(b => b.CourseId == course.CourseId).FirstOrDefault();
            //ViewBag.context = _context;


            return View(ctask);
        }

        // GET: Tasks/Create
        [Authorize(Roles = "teacher, admin")]
        public IActionResult Create(int courseId)
        {
            ViewBag.CourseId = courseId;
            ViewBag.CourseName = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name; 
            //ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");
            return View();
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
                return RedirectToAction("Index", "Tasks", new {id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
            }
            //ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            //return View(task);
            return RedirectToAction("Index", "Tasks", new { id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
        }

        // GET: Tasks/Edit/5
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

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
        // GET: Tasks/Complete/5
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

        // POST: Tasks/Complete/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
                    if(_context.CompletedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).FirstOrDefault() == null)
                        _context.Add(completedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).FirstOrDefault());
                    else
                        _context.Update(completedTasks.Where(b => b.UserCourseId == usercourse.UserCourseId).FirstOrDefault());

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
        // GET: Tasks/CompletedList/5
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

            var userList = (
                from userCourse in userCourseList
                join user in _context.Users on userCourse.UserId equals user.UserId
                select user
            ).ToList();

            //var userList = await _context.Users.Where(u => usercourseList.Any(uc => uc.UserId == u.UserId)).ToListAsync();
            ViewBag.context = _context;

            return View(userCourseList);
        }
        // GET: Tasks/Delete/5
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

        // POST: Tasks/Delete/5
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
