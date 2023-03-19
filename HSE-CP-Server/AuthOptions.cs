using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HSE_CP_Server
{
    public class AuthOptions
    {
        public const string ISSUER = "HSE_CP_Server"; // издатель токена
        public const string AUDIENCE = "HSE_CP_Client"; // потребитель токена
        const string KEY = "secretkeyasdasdasdawsdasdasdasdasdasdasdas";   // ключ для шифрации
        public const int LIFETIME = 260640; // время жизни токена - 6 месяцев (в минутах)
        public static SymmetricSecurityKey GetSymmetricSecurityKey() // метод GetSymmetricSecurityKey() возвращает ключ безопасности, который применяется для генерации токена. Для генерации токена нам необходим объект класса SecurityKey
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
        }
    }
}
