using CounterWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;

namespace CounterWeb
{
    public static class Helper
    {
        public static string ToCode(int CourseId, string CourseName)
        {
            Tuple<int, string> myObject = Tuple.Create(CourseId, CourseName);
            string json = JsonConvert.SerializeObject(myObject);
            string encodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return encodedString;
        }
        public static (int, string) ToDecode(string encodedString)
        {
            /*byte[] bytes = Convert.FromBase64String(encodedString);
            string json = Encoding.UTF8.GetString(bytes);
            Tuple<int,string> myObject = (Tuple<int, string>)JsonConvert.DeserializeObject(json);
            return myObject; */
            
            var decodedBytes = Convert.FromBase64String(encodedString); // Декодую string у масив байтів
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);    // Декодую масив байтів у json
            var decodedObject = JsonConvert.DeserializeAnonymousType(decodedJson, new { courseId = default(int), courseName = default(string) }); // декодую json у об'єкт, що буде зберігати id курса та name
            return (decodedObject.courseId, decodedObject.courseName);
        }
        public static bool ToValidate<T>(T _object, int? curValue, int newValue, string propertyName)
        {
            if (curValue != newValue)
            {
                // створюємо об'єкт ValidationContext, вказуючи тип об'єкта та ім'я властивості, яку потрібно перевірити
                var validationContext = new ValidationContext(_object)
                {
                    MemberName = propertyName,
                };
                // виконуємо валідацію властивості task.MaxGrade
                var validationResults = new List<ValidationResult>();
                if (Validator.TryValidateProperty(newValue, validationContext, validationResults))
                {
                    // якщо валідація успішна
                    return true;
                }
                else
                {
                    // TASK.MAXGRADE НЕ ВІДПОВІДАЄ ВАЛІДАЦІЇ
                    return false;
                }

            }
            return false;
        }

    }
}
