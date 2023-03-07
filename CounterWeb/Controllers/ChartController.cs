using CounterWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CounterWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {
        private readonly CounterDbContext _context;
        public ChartController(CounterDbContext context)
        {
            _context = context;
        }
        [HttpGet("JsonData")]
        public JsonResult JsonData(int? id)
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
                    if (ct.Grade != null && task.MaxGrade != null)
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
            catStudents.Add(new object[] { "0-65 %", bad });
            return new JsonResult(catStudents);
        }
        // сделать диаграмку с рейтингом студентов по списку
    }
}
