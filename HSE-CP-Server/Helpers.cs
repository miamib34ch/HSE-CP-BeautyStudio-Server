using System.Security.Cryptography;
using System.Text;

namespace HSE_CP_Server
{
    public class Helpers
    {
        [Flags]
        public enum PasswordRules
        {
            // пароль должен содержать цифры
            Digit = 1,

            // пароль должен содержать большие буквы
            UpperCase = 2,

            // пароль должен содержать маленькие буквы
            LowerCase = 4,

            // пароль должен содержать как большие, так и маленькие буквы
            MixedCase = 6,

            // пароль должен содержать специальные символы
            SpecialChar = 8,

            // все выше перечисленные правила
            All = 15,

            // не валидируем пароль
            None = 0
        }

        public static bool IsPasswordValid(string password, PasswordRules rules, params string[]? ruleOutList)
        {
            bool result = true;
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            string allChars = lower + upper + digits;

            // проверка на маленькие буквы
            if (Convert.ToBoolean(rules & PasswordRules.LowerCase))
                result &= (password.IndexOfAny(lower.ToCharArray()) >= 0);

            // проверка на большие буквы
            if (Convert.ToBoolean(rules & PasswordRules.UpperCase))
                result &= (password.IndexOfAny(upper.ToCharArray()) >= 0);

            // проверка на наличие цифр
            if (Convert.ToBoolean(rules & PasswordRules.Digit))
                result &= (password.IndexOfAny(digits.ToCharArray()) >= 0);

            // проверка на специальные символы
            if (Convert.ToBoolean(rules & PasswordRules.SpecialChar))
                result &= (password.Trim(allChars.ToCharArray()).Length > 0);

            // проверка на слова, которые нельзя использовать
            if (ruleOutList != null)
                for (int i = 0; i < ruleOutList.Length; i++)
                    result &= (password != ruleOutList[i]);

            return result;
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashBytes = sha256Hash.ComputeHash(bytes);

                byte[] inputHashBytes = StringToByteArray(hash);

                return CryptographicOperations.FixedTimeEquals(hashBytes, inputHashBytes);
            }
        }

        private static byte[] StringToByteArray(string hex)
        {
            int length = hex.Length / 2;
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
