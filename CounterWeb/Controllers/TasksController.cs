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

namespace CounterWeb.Controllers
{
    public class TasksController : Controller
    {
        private readonly CounterDbContext _context;

        public TasksController(CounterDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Details(int? id)
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

        // GET: Tasks/Create
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, [Bind("TaskId,CourseId,Name,Description,Grade,Solution")] Task task)
        {
            task.CourseId = courseId;
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Tasks", new {id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
                //return RedirectToAction(nameof(Index));
            }
            //ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            //return View(task);
            return RedirectToAction("Index", "Tasks", new { id = courseId, name = _context.Courses.Where(c => c.CourseId == courseId).FirstOrDefault().Name });
        }

        // GET: Tasks/Edit/5
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            task.Course = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault();
            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int taskId, [Bind("TaskId,CourseId,Name,Description,MaxGrade,CompletedTasks")] Task task)
        {
            if (taskId != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
        // GET: Tasks/EditStud/5
        public async Task<IActionResult> EditStud(int? id)
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", task.CourseId);
            task.Course = _context.Courses.Where(c => c.CourseId == task.CourseId).FirstOrDefault();
            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStud(int taskId, [Bind("TaskId,CourseId,Name,Description,MaxGrade, CompletedTasks.FirstOrDefault().Solution")] Task task)
        {
            if (taskId != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
        // GET: Tasks/Delete/5
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tasks == null)
            {
                return Problem("Entity set 'CounterDbContext.Tasks'  is null.");
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
          return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }
    }
}
