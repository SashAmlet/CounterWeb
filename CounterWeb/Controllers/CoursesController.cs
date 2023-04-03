using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using DocumentFormat.OpenXml.Wordprocessing;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int courseId, IFormFile fileExcel)
        {
            List<string> errors = new List<string>();
            if (ModelState.IsValid && fileExcel != null)
            {
                using (var stream = new FileStream(fileExcel.FileName, FileMode.Create))
                {
                    await fileExcel.CopyToAsync(stream);
                    using (XLWorkbook workBook = new XLWorkbook(stream))
                    {
                        // перевіряємо усі сторінки excel файлу
                        foreach (IXLWorksheet worksheet in workBook.Worksheets)
                        {
                            //worksheet.Name - назва курсу (назва excel файлу)
                            var course = await
                                (
                                    from crs in _context.Courses
                                    where crs.Name.Contains(worksheet.Name) && crs.CourseId == courseId
                                    select crs
                                ).AsNoTracking().FirstOrDefaultAsync();

                            if (course is null)
                            {
                                // запитати чи хоче юзер створити новий курс, інфа з якого є в excel file, але якого нема в бд
                                TempData["Message"] = "Помилка з назвою курсу в excel файлі";
                                return RedirectToAction(nameof(Show), new { id = courseId });
                            }

                            // формую список тасків, які є в excel файлі, та в БД у відповідного курса (зберігається номер колонки та відповідний таск).
                            List<(int, Models.Task)> tasks = new List<(int, Models.Task)>();
                            foreach (IXLColumn col in worksheet.ColumnsUsed().Skip(1))
                            {
                                // ДОПОВНИТИ ВІДСТАННЮ ЛЕВЕШТЕЙНА
                                string? taskName = col.Cell(1).Value.ToString().TrimEnd(')').Split('(').FirstOrDefault()?.Trim();
                                string? maxGrade = col.Cell(1).Value.ToString().TrimEnd(')').Split('(').LastOrDefault()?.Split(' ').FirstOrDefault()?.Trim();
                                int g = -1;


                                if (taskName is not null)
                                {
                                    var task = await _context.Tasks.Where(a => a.CourseId == courseId && a.Name.Contains(taskName)).FirstOrDefaultAsync();
                                    if (task is null)
                                        continue;
                                    

                                    // перевіряємо валідацію для Task.MaxGrade
                                    if (maxGrade is not null && int.TryParse(maxGrade, out g))
                                    {
                                        try
                                        {
                                            if(Helper.ToValidate<Models.Task>(task, task.MaxGrade, g, "MaxGrade"))
                                            {
                                                errors.Add("Через зміну максимальної кількості балів за " + task.Name.ToString() + " усі здані роботи на це завдання обнулились (окрім тих, що були в excel-файлі і відповідають валідації)\n");
                                                task.MaxGrade = g;
                                                _context.Update(task);
                                                _context.SaveChanges();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            TempData["Message"] = "#val1: " + ex.Message;
                                            return RedirectToAction(nameof(Show), new { id = courseId });
                                        }
                                    }
                                    // явно вкажемо контексту, щоб він не відслідковував task
                                    _context.Entry(task).State = EntityState.Detached;
                                    tasks.Add((col.ColumnNumber(), task));
                                }
                            
                            }

                            //перегляд усіх учнів
                            foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
                            {
                                /*// витягую ім'я та призвище з рядка excel
                                string lastName = row.Cell(1).Value.ToString().Trim().Split(' ').First();
                                string firstName = row.Cell(1).Value.ToString().Trim().Split(' ').Last();*/

                                // витягую саме того юзера, що має такі ім'я - прізвище, та є у відповідному курсі
                                /*var user = await
                                    (
                                        from usr in _context.Users
                                        where usr.FirstName == firstName && usr.LastName == lastName
                                        join usrCrs in _context.UserCourses on usr.UserId equals usrCrs.UserId
                                        where usrCrs.CourseId == courseId
                                        select usr

                                    ).AsNoTracking().FirstOrDefaultAsync();*/

                                // витягую ім'я юзера з клітинки
                                string cellUser = row.Cell(1).Value.ToString();
                                // витягую усіх юзерів мого курсу
                                var userList = await
                                    (
                                        from usr in _context.Users
                                        join usrCrs in _context.UserCourses on usr.UserId equals usrCrs.UserId
                                        where usrCrs.CourseId == courseId
                                        select usr

                                    ).AsNoTracking().ToListAsync();
                                // витягую найбільш схожого юзера та відцоток схожості
                                var jaroWink = Helper.StringMathing(userList, cellUser);
                                User user = new User();
                                if (jaroWink.Item1 >= 0.95)
                                    user = jaroWink.Item2;
                                else
                                {
                                    // У ВИПАДКУ ПОГАНОГО ЗБІГУ ЗРОБИТИ ЩОСЬ
                                    TempData["Message"] = "378 строка, реалізація jaroWink для user";
                                    return RedirectToAction(nameof(Show), new { id = courseId });
                                }



                                if (user is not null)
                                {
                                    //пробігаюсь по таскам
                                    foreach (var t in tasks)
                                    {
                                        // явно вкажемо контексту, щоб він скинув усі об'єкти, що відслідковував (запобігає вийнятку)
                                        _context.ChangeTracker.Clear();
                                        // витягую CompletedTask юзера, що відповідає нашому таску та юзеру
                                        var ctask = await
                                            (
                                                from ctsk in _context.CompletedTasks
                                                where ctsk.TaskId == t.Item2.TaskId
                                                join usrCrs in _context.UserCourses on ctsk.UserCourseId equals usrCrs.UserCourseId
                                                where usrCrs.CourseId == courseId && usrCrs.UserId == user.UserId
                                                select ctsk
                                            ).FirstOrDefaultAsync();
                                        // ВИВЕСТИ ПОВІДОМЛЕННЯ ЮЗЕРУ
                                        if (ctask is null)
                                        {
                                            int g1 = 0;
                                            if (int.TryParse( row.Cell(t.Item1).Value.ToString(),out g1) && g1 != 0)
                                                errors.Add("Готового домашнього завдання учня " + user.LastName + " " + user.FirstName + " до " + t.Item2.Name + " не існує, його не можливо оцінити ( клітинка [" + (row.RowNumber() - 1).ToString() + ";" + (t.Item1 - 1).ToString() + "]) \n");
                                            continue;
                                        }

                                        int g = 0;

                                        // перевіряємо валідацію для CompletedTask.Grade
                                        if (int.TryParse(row.Cell(t.Item1).Value.ToString(), out g) && (ctask.Grade != g))
                                        {
                                            try
                                            {
                                                if(Helper.ToValidate<Models.CompletedTask>(ctask, ctask.Grade, g, "Grade"))
                                                {
                                                    ctask.Grade = g;
                                                    _context.Update(ctask);
                                                    _context.SaveChanges();
                                                }
                                                else
                                                {
                                                    errors.Add("Клітинка [" + (row.RowNumber() - 1).ToString() + ";" + (t.Item1-1).ToString() + "] не пройшла валідацію (зверніть увагу на максимальний бал за завдання)\n");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                TempData["Message"] = "#val2: " + ex.Message;
                                                return RedirectToAction(nameof(Show), new { id = courseId });
                                            }
                                        }
                                        // явно вкажемо контексту, щоб він не відслідковував ctask
                                        /*if(ctask.Task is not null)
                                            _context.Entry(ctask.Task).State = EntityState.Detached;
                                        _context.Entry(ctask).State = EntityState.Detached;*/
                                    }
                                }
                            }
                        }
                    }
                }


                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    TempData["Message"] = "#2: " + e.Message;
                    return RedirectToAction(nameof(Show), new { id = courseId });

                }
            }
            string mess = string.Empty;
            foreach(var err in errors)
            {
                mess += err;
            }
            TempData["Message"] = "Успіх!\n" + mess;
            return RedirectToAction(nameof(Show), new { id = courseId });
        }

        // POST: Courses/Export
        public ActionResult Export(int? courseId, string? studListJson, string? userCourseListJson)
        {//експорт журналу оцінок
            List<User> studList = JsonConvert.DeserializeObject<List<User>>(studListJson);
            List<UserCourse> userCourseList = JsonConvert.DeserializeObject<List<UserCourse>>(userCourseListJson);

            using (XLWorkbook workbook = new XLWorkbook(/*XLEventTracking.Disabled*/))
            {
                var course = _context.Courses.Where(b => b.CourseId == courseId).Include(b=>b.Tasks).ThenInclude(a=>a.CompletedTasks).FirstOrDefault();

                var worksheet = workbook.Worksheets.Add(course.Name);

                for (int i = 0; i < course.Tasks.Count(); i++)
                {
                    var t = course.Tasks.ToArray()[i];
                    worksheet.Cell(1, i+2).Value =t.Name + " (" + t.MaxGrade + " балів)" ;
                }
                worksheet.Row(1).Style.Font.Bold = true;

                for (int i = 0; i < studList.Count(); i++)
                {
                    int j = 0;
                    var stud = studList[i];
                    worksheet.Cell(i + 2, j + 1).Value = stud.LastName + " " + stud.FirstName;
                    int? userCourseId = userCourseList.Where(c => c.UserId == stud.UserId).Select(a => a.UserCourseId).FirstOrDefault();
                    foreach (var task in course.Tasks)
                    {
                        var completedTask = task.CompletedTasks.FirstOrDefault(a => a.UserCourseId == userCourseId);
                        var grade = completedTask != null ? completedTask.Grade : 0;
                        worksheet.Cell(i+2, j + 2).Value = grade;
                        j++;
                    }
                }
                worksheet.Column(1).Style.Font.Bold = true;

                for (int i = 0; i < course.Tasks.Count()+1; i++)
                {
                    worksheet.Column(i + 1).AdjustToContents(); // автоматичний підбір ширини стовпця
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Flush();

                    return new FileContentResult(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    {
                        FileDownloadName = $"counter_{DateTime.UtcNow.ToShortDateString()}.xlsx"
                    };
                }
            }
        }


        private bool CourseExists(int id)
        {
          return (_context.Courses?.Any(e => e.CourseId == id)).GetValueOrDefault();
        }


    }
}
