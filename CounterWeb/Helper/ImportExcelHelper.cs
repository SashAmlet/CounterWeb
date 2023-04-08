using ClosedXML.Excel;
using CounterWeb.Models;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CounterWeb.Helper
{
    public class ImportExcelHelper
    {
        public static XLWorkbook GetWorkbook(IFormFile fileExcel)
        {
            using (var stream = new FileStream(fileExcel.FileName, FileMode.Create))
            {
                fileExcel.CopyTo(stream);
                XLWorkbook workBook = new XLWorkbook(stream);
                return workBook;
            }
        }
        public static Course? GetCourse(IXLWorksheet worksheet, int courseId)
        {
            using(var _context = new CounterDbContext())
            {
                //worksheet.Name - назва курсу (назва excel файлу)
                Course? course = (
                                from crs in _context.Courses
                                where crs.Name.Contains(worksheet.Name) && crs.CourseId == courseId
                                select crs
                                ).FirstOrDefault();
                return course;

            }
        }
        public static (List<(int, Models.Task)>, List<string>) GetTask(IXLWorksheet worksheet, int courseId)
        {
            // формую список тасків, які є в excel файлі, та в БД у відповідного курса (зберігається номер колонки та відповідний таск).
            List<(int, Models.Task)> tasks = new List<(int, Models.Task)>();
            // формую список можливих помилок
            List<string> errors = new List<string>();

            using (var _context = new CounterDbContext())
            {
                foreach (IXLColumn col in worksheet.ColumnsUsed().Skip(1))
                {
                    string cellText = col.Cell(1).Value.ToString();

                    // витягую значення MaxGrade (те що в скобочках біля назви завдання) якщо воно є
                    int? startIndex = cellText.IndexOf('(') + 1;
                    int? endIndex = cellText.LastIndexOf(')');
                    string? maxGrade = null;
                    if (startIndex is not null && endIndex is not null)
                        maxGrade = cellText[startIndex.Value..endIndex.Value]; // у цю змінну я витягую увесь вміст скобочок
                    else
                        errors.Add("Максимальний бал для стовпця " + (col.ColumnNumber() - 1).ToString() + " не був змінений через неправильний формат вводу. Якщо ви хочете змінити максимальну оцінку для завдання, то слідуйте наступному шаблону 'Назва завдання (16 балів)'\n");



                    // витягую task, чиє ім'я найбільш схоже до того, що в табличці написано
                    string? taskName = maxGrade is null ? cellText : cellText.Replace('(' + maxGrade + ')', "");

                    if (taskName is null)
                    {
                        continue;
                    }

                    var taskList = _context.Tasks.AsNoTracking().Where(a => a.CourseId == courseId).ToList();

                    var jaro_Wink = ImportExcelHelper.StringMatching<Models.Task>(taskList, taskName);

                    Models.Task? task = new Models.Task();
                    if (jaro_Wink.Item1 >= 0.5)
                        task = jaro_Wink.Item2;
                    else
                    {
                        // У ВИПАДКУ ПОГАНОГО ЗБІГУ ЗРОБИТИ ЩОСЬ
                        throw new Exception("ERROR:: ImportExcelHelper -> GetTask, реалізація jaroWink для task");
                    }

                    if (task is not null)
                    {
                        // перевіряємо валідацію для Task.MaxGrade
                        int g = -1;
                        if (maxGrade is not null && int.TryParse(new string(maxGrade.Where(char.IsDigit).ToArray()), out g)) // витягую усі числа зі строки maxGrade у g (бо maxGrade виглядає як '16 балів')
                        {
                            try
                            {
                                if (GeneralHelper.ToValidate<Models.Task>(task, task.MaxGrade, g, "MaxGrade"))
                                {
                                    errors.Add("Через зміну максимальної кількості балів за " + task.Name.ToString() + " усі здані роботи на це завдання обнулились (окрім тих, що були в excel-файлі і відповідають валідації)\n");
                                    task.MaxGrade = g;
                                    _context.Update(task);
                                    _context.SaveChanges();
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("ERROR:: ImportExcelHelper -> GetTask, #val1: " + ex.Message);
                            }
                        }
                        tasks.Add((col.ColumnNumber(), task));
                    }

                }
            }
            return (tasks, errors);
        }
        public static User? GetUser(string userName, int courseId)
        {
            using(var _context = new CounterDbContext())
            {
                // витягую усіх юзерів мого курсу
                var userList =(
                                from usr in _context.Users
                                join usrCrs in _context.UserCourses on usr.UserId equals usrCrs.UserId
                                where usrCrs.CourseId == courseId
                                select usr
                                ).ToList();

                // витягую найбільш схожого юзера та відцоток схожості
                var jaroWink = ImportExcelHelper.StringMatching<User>(userList, userName);

                User user = new User();

                if (jaroWink.Item1 >= 0.95)
                    user = jaroWink.Item2;
                else
                {
                    // У ВИПАДКУ ПОГАНОГО ЗБІГУ ЗРОБИТИ ЩОСЬ
                    throw new Exception("ImportExcelHelper -> GetUser, реалізація jaroWink для user");
                }

                return user;
            }
        }
                
        public static (double, T?) StringMatching<T>(List<T> userList, string cellUser)
        {//серед усіх юзерів з userList знаходжу того, ім'я якого найбільш похоже на cellUser

            double bestDistance = double.MinValue;
            double jaroWinklerDistance = 0;
            T? bestMatch = default;

            foreach (T u in userList)
            {
                if (u is User user)
                {
                    // використовую метор Джаро-Вінклера, бо він дозволяє відслідковувати тринспозиції слів
                    jaroWinklerDistance = JaroWinklerDistance(user.LastName.Replace(" ", "") + user.FirstName.Replace(" ", ""), cellUser.Replace(" ", ""));
                }
                else if (u is Models.Task task)
                {
                    // використовую метод Дамерау-Левештейна,бо транспозиція слів тут не потрібна, а в загальному випадку відповідь точніше
                    jaroWinklerDistance = DamerauLevenshteinDistance(task.Name.Replace(" ", ""), cellUser.Replace(" ", ""));
                }

                if (jaroWinklerDistance > bestDistance)
                {
                    bestDistance = jaroWinklerDistance;
                    bestMatch = u;
                }
            }

            return (bestDistance, bestMatch);
        }
        private static double JaroWinklerDistance(string s1, string s2)
        {
            // Алгоритм Джаро-Вінклера для підрахунку "схожості строк" // уся інфа за посиланням "https://habr.com/ru/post/671136/"
            int len1 = s1.Length;
            int len2 = s2.Length;
            if (len1 == 0 || len2 == 0)
                return 0;

            // спеціальна констатна, суть якої полягає в наступному:
            // для кожного симолу s1[i] рядку s1 ми будемо шукати такий же символ в рядку s2,
            // але порівнювати s1[i] та s2[i] тупо, бо може бути помилка виду Саша <-> Асаш (два перших символи поміняни місцями),
            // тому ми добавляємо для s2 діапазон, в якому може знаходитись відповідний символ, а діапазоє виглядає як s2[i] +- matchDistance.
            int matchDistance = Math.Max(len1, len2) / 2 - 1;

            int incompleteMatches = 0; // коли s1[i] == s2[i] +- matchDistance
            int completeMatches = 0;   // коли s1[i] == s2[i]
            //bool[] s1Maches = new bool[len1];
            //bool[] s2Maches = new bool[len2];

            for (int i = 0; i < len1; i++)
            {
                // якщо провіряю повну подібність двух символів на i-тому місці
                if (i <= len2 - 1 && s1[i] == s2[i])
                {
                    completeMatches++;
                    //s1Maches[i] = true;
                    //s2Maches[i] = true;
                }
                // встановлюємо той самий діапазон s1[i] <-> s2[i] +-matchDistance
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);

                // пробігаюсь по усім елементам діапазону, і якщо є s1[i] подібний до якогось з s2[j], то помічаю s1Matches та s2Matches
                for (int j = start; j < end; j++)
                {
                    if (s1[i] != s2[j])
                        continue;
                    if (i == j)
                        continue;
                    //if (s2Maches[j])
                    //    continue;
                    //s1Maches[i] = true;
                    //s2Maches[j] = true;
                    incompleteMatches++;
                    break;
                }
            }

            int matches = completeMatches + incompleteMatches;
            int transpositions = incompleteMatches / 2;

            if (matches == 0)
                return 0;

            double jaro = (double)(((double)matches / len1 + (double)matches / len2 + ((double)matches - transpositions) / matches) / 3);
            jaro = jaro > 1 ? 1 : jaro; // щоб jaro завжди був <= 1

            // та сама модифікація "Вінклера" - рахую кількість співпадающих елементів з початку слова до першої відмінності
            int commonPrefix = 0;
            for (int i = 0; i < Math.Min(len1, len2); i++)
            {
                if (s1[i] != s2[i])
                    break;

                commonPrefix++;
            }

            return jaro + 0.1 * commonPrefix * (1 - jaro);
        }
        private static double DamerauLevenshteinDistance(string str1, string str2)
        {// Алгоритм Дамерау - Левенштейна для підрахунку "схожості строк" //  уся іінфа за посиланням "https://habr.com/ru/post/676858/"
            int[,] distances = new int[str1.Length + 1, str2.Length + 1];

            for (int i = 0; i <= str1.Length; i++)
            {
                distances[i, 0] = i;
            }

            for (int j = 0; j <= str2.Length; j++)
            {
                distances[0, j] = j;
            }

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    int cost = str1[i - 1] == str2[j - 1] ? 0 : 1;

                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);

                    if (i > 1 && j > 1 && str1[i - 1] == str2[j - 2] && str1[i - 2] == str2[j - 1])
                    {
                        distances[i, j] = Math.Min(distances[i, j], distances[i - 2, j - 2] + cost);
                    }
                }
            }

            int maxLength = Math.Max(str1.Length, str2.Length);
            return 1.0 - (double)distances[str1.Length, str2.Length] / maxLength;
        }
    }
}
