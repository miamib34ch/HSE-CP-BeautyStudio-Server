namespace HSE_CP_Server
{
    public class Helper
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
    }
}
