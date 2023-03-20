using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HSE_CP_Server
{
    public class AuthOptions
    {
        public const string ISSUER = "HSE_CP_Server"; // издатель токена
        public const string AUDIENCE = "HSE_CP_Client"; // потребитель токена
        const string KEY = "secretkeyasdasdasdawsdasdasdasdasdasdasdas";   // ключ для шифрации
        public const int LIFETIME = 180; // время жизни токена - 180 дней
        public static SymmetricSecurityKey GetSymmetricSecurityKey() // метод GetSymmetricSecurityKey() возвращает ключ безопасности, который применяется для генерации токена. Для генерации токена нам необходим объект класса SecurityKey
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
        }
    }
}
