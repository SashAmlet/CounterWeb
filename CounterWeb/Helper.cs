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
        public static (double, User) StringMathing(List<User> userList, string cellUser)
        {//серед усіх юзерів з userList знаходжу того, ім'я якого найбільш похоже на cellUser

            double bestDistance = Double.MinValue;
            User bestMatch = new User();

            foreach (User u in userList)
            {
                double jaroWinklerDistance = JaroWinklerDistance(u.LastName.Replace(" ", "") + u.FirstName.Replace(" ", ""), cellUser.Replace(" ", ""));
                //double jaroWinklerDistance_not_working = JaroWinklerDistance_chat(u.LastName.Replace(" ","") + u.FirstName.Replace(" ", ""), cellUser.Replace(" ",""));

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
            // Алгоритм Джаро-Вінклера для підрахунку "схожості строк"
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


            for (int i = 0; i < len1; i++)
            {
                // якщо провіряю повну подібність двух символів на i-тому місці
                if (i <= (len2 - 1) && s1[i] == s2[i])
                {
                    completeMatches++;
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

            return jaro + (0.1 * commonPrefix * (1 - jaro));
        }

        private static double JaroWinklerDistance_chat(string s1, string s2)
        {
            // Алгоритм Джаро-Вінклера для підрахунку "схожості строк"
            int len1 = s1.Length;
            int len2 = s2.Length;
            if (len1 == 0 || len2 == 0)
                return 0;

            // спеціальна констатна, суть якої полягає в наступному:
            // для кожного симолу s1[i] рядку s1 ми будемо шукати такий же символ в рядку s2,
            // але порівнювати s1[i] та s2[i] тупо, бо може бути помилка виду Саша <-> Асаш (два перших символи поміняни місцями),
            // тому ми добавляємо для s2 діапазон, в якому може знаходитись відповідний символ, а діапазоє виглядає як s2[i] +- matchDistance.
            int matchDistance = Math.Max(len1, len2) / 2 - 1;

            bool[] s1Matches = new bool[len1];
            bool[] s2Matches = new bool[len2];

            int matches = 0;
            int transpositions = 0;

            for (int i = 0; i < len1; i++)
            {
                // встановлюємо той самий діапазон s1[i] <-> s2[i] +-matchDistance
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);
                // пробігаюсь по усім елементам діапазону, і якщо є сходство, то помічаю s1Matches та s2Matches
                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j])
                        continue;
                    if (s1[i] != s2[j])
                        continue;

                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
                return 0;

            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!s1Matches[i])
                    continue;

                while (!s2Matches[k])
                    k++;

                if (s1[i] != s2[k])
                    transpositions++;
                k++;
            }

            double jaro = (double)matches / len1;
            double jaroWinkler = jaro + (transpositions * 0.1 * (1 - jaro));

            // та сама модифікація "Вінклера" - рахую кількість співпадающих елементів з початку слова до першої відмінності
            int commonPrefix = 0;
            for (int i = 0; i <  Math.Min(len1, len2); i++)
            {
                if (s1[i] != s2[i])
                    break;

                commonPrefix++;
            }

            return jaroWinkler + (0.1 * commonPrefix * (1 - jaroWinkler));
        }
    }
}
