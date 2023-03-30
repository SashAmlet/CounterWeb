using CounterWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Web;

namespace CounterWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {
        private readonly CounterDbContext _context;
        private readonly UserManager<UserIdentity> userManager;
        public ChartController(CounterDbContext context, UserManager<UserIdentity> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }
        [HttpGet("GetGrades")]
        public JsonResult GetGrades(int? id)
        {
            var course = _context.Courses.Where(b => b.CourseId == id).Include(b=>b.Tasks).ThenInclude(c=>c.CompletedTasks).FirstOrDefault();
            if (course == null)
            {
                return null;
            }
            List<object> catStudents = new List<object>();
            catStudents.Add(new[] {"Category", "Num of students"});
            int excellent = 0, good = 0, norm = 0, bad = 0;
            foreach(var task in course.Tasks)
            {
                foreach(var ct in task.CompletedTasks)
                {
                    if (ct.Grade != null && task.MaxGrade != null && task.MaxGrade != 0)
                    {
                        double perc = (double)((double)ct.Grade / task.MaxGrade);
                        if (perc >= 0.9)
                            ++excellent;
                        else if (perc >= 0.75)
                            ++good;
                        else if(perc >= 0.6)
                            ++norm;
                        else
                            ++bad;
                    }
                    
                }
            }
            catStudents.Add(new object[] { "90-100 %", excellent });
            catStudents.Add(new object[] { "75-90 %", good });
            catStudents.Add(new object[] { "60-75 %", norm });
            catStudents.Add(new object[] { "0-60 %", bad });
            return new JsonResult(catStudents);
        }

        [HttpGet("GetStudents")]
        [Produces("application/json")]
        public JsonResult GetStudents(int? id, string? students)
        {
            if(students == null || id==null)
                return new JsonResult(null);

            string studentsDecoded = HttpUtility.UrlDecode(students); // розкодування зі string у json
            List<User> studentsList = JsonConvert.DeserializeObject<List<User>>(studentsDecoded); // розкодування самого json у список моделей

            List<object> catStudents = new List<object>();
            catStudents.Add(new[] { "Student", "Середній бал" });

            var userCourse = (
                    from usercourse in _context.UserCourses.AsEnumerable()
                    join student in studentsList on usercourse.UserId equals student.UserId
                    select usercourse
                ).ToList();

            foreach (UserCourse usercourse in userCourse)
            {
                double avr = 0;
                int i = 0;
                var student = _context.Users.Where(b => b.UserId == usercourse.UserId).FirstOrDefault();
                foreach (var ct in _context.CompletedTasks.Where(b=>b.UserCourseId == usercourse.UserCourseId).ToList())
                {
                    var maxGrade = _context.Tasks.Where(b => b.TaskId == ct.TaskId).FirstOrDefault().MaxGrade;
                    if (maxGrade != null && ct.Grade != null && maxGrade != 0)
                    {
                        avr += (double)((double)ct.Grade / maxGrade);
                        ++i;
                    }
                }
                catStudents.Add(new object[] { student.FirstName + " " + student.LastName, (i == 0 ? 0 : (double)avr / i) });
            }
            
            return new JsonResult(catStudents);
        }
    }
}
