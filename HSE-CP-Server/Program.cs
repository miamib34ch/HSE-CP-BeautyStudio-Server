using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using HSE_CP_Server;
using HSE_CP_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // добавление фильтра операций для добавления атрибута Authorize к операциям, требующим авторизации
    c.OperationFilter<AuthorizeCheckOperationFilter>();

    c.EnableAnnotations();
}
);
builder.Services.AddAuthentication("Bearer")  // схема аутентификации - с помощью jwt-токенов
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // указывает, будет ли валидироваться издатель при валидации токена
            ValidateIssuer = true,
            // строка, представляющая издателя
            ValidIssuer = AuthOptions.ISSUER,
            // будет ли валидироваться потребитель токена
            ValidateAudience = true,
            // установка потребителя токена
            ValidAudience = AuthOptions.AUDIENCE,
            // будет ли валидироваться время существования
            ValidateLifetime = true,
            // установка ключа безопасности
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // валидация ключа безопасности
            ValidateIssuerSigningKey = true,
        };
    });      // подключение аутентификации с помощью jwt-токенов
builder.Services.AddAuthorization();            // добавление сервисов авторизации

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации 

#region mapping

var options = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
    WriteIndented = true
};

#region post

app.MapPost("/login",
    [SwaggerOperation(
        Summary = "Авторизация",
        Description = "Авторизация в приложение по логину и паролю, вернёт токен пользователя и его роль на сервере.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(400, "Wrong password")]
[SwaggerResponse(404, "There is not such person")]
(string login, string pass) =>
    {
        Context context = new Context();
        var user = context.Users.FirstOrDefault(u => u.Login == login);
        if (user == null)
            return Results.NotFound("There is not such person");
        if (!Helpers.VerifyPassword(pass, user.Password))
            return Results.BadRequest("Wrong password");

        var role = context.Role.First(r => r.IdRole == user.Role).RoleName;
        var token = user.Token;

        var goodResponse = new
        {
            token,
            role
        };

        return Results.Json(goodResponse, options);
    });

app.MapPost("/registration",
    [SwaggerOperation(
        Summary = "Регистрация",
        Description = "Запрос на регистрацию на сервере. Пароль должен содержать большие и мальнькие буквы, цифры, специальные символы, также он должен быть больше или равен 8 символам.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(400, "User exist yet")]
[SwaggerResponse(406, "Name can not be empty")]
[SwaggerResponse(409, "Weak password")]
([SwaggerRequestBody(Required = true)] User newUser) =>
    {
        String login = newUser.login;
        String pass = newUser.pass;
        Context context = new Context();
        var user = context.Users.FirstOrDefault(u => u.Login == login);
        if (user != null)
            return Results.BadRequest("User exist yet");
        if (!Helpers.IsPasswordValid(pass, Helpers.PasswordRules.All, null) || pass.Count() < 8)
            return Results.Conflict("Weak password");
        if (login == null || login == "")
            return Results.StatusCode(406);

        // создаем JWT-токен
        var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        var User = new Users(login, Helpers.HashPassword(pass), null, 2, token);
        context.Users.Add(User);
        context.SaveChanges();

        var goodResponse = new
        {
            token = token,
            role = "client",
        };

        return Results.Json(goodResponse, options);
    });

app.MapPost("note",
    [SwaggerOperation(
        Summary = "Отправка запроса на звонок",
        Description = "Отправка запроса на запись на процедуру. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(400, "Not okay phone number")]
[SwaggerResponse(401, "Not authorize")]
[Authorize]
(HttpContext context, string ProcedureName, string phone, string? massage) =>
    {
        string motif = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
        if (!Regex.IsMatch(phone, motif))
            return Results.BadRequest("No okay phone number");

        //var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        //Context contextDB = new Context();

        //var user = contextDB.Users.First(u => u.Token == token);

        var result = new { msg = "Запрос принят" };

        return Results.Json(result, options);
    });

#endregion

#region get

app.MapGet("/phone",
    [SwaggerOperation(
        Summary = "Получить номер телефона клиента",
        Description = "Если у клиента есть в базе данных телефон, то вернёт его. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(401, "Not authorize")]
[SwaggerResponse(404, "There is no phone")]
[Authorize]
(HttpContext context) =>
    {
        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        Context contextDB = new Context();

        var user = contextDB.Users.First(u => u.Token == token);
        var client = contextDB.Clients.FirstOrDefault(c => c.IdClient == user.IdClient);
        if (client == null)
            return Results.NotFound("There is no phone");
        var phone = client.PhoneNumber;
        if (phone == null)
            return Results.NotFound("There is no phone");

        var result = new
        {
            phone
        };

        return Results.Json(result, options);
    });

app.MapGet("/price",
    [SwaggerOperation(
        Summary = "Список всех процедур салона",
        Description = "Получение всего прайса с основными данными каждой процедуры, кроме описания.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(404, "There is not prices")]
() =>
{
    Context context = new Context();
    var procedures = context.Procedure.Select(s => s);

    var response = new List<ResponsePrice>();

    if (procedures != null)
    {
        foreach (var procedure in procedures)
        {
            var categorieName = context.ProcedureCategorie.First(p => p.IdCategorie == procedure.IdCategorie).NameCategorie;
            var responsePrice = new ResponsePrice(procedure.IdProcedure, procedure.Cost, procedure.PhotoName, procedure.ProcedureName);
            response.Add(responsePrice);
        }
        return Results.Json(response, options);
    }
    else
        return Results.NotFound("There is not prices");
});

app.MapGet("/price/{id}",
    [SwaggerOperation(
        Summary = "Конкретная процедура",
        Description = "Возвращает полную информацию о процедуре по её id.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(404, "There is not procedure with this id")]
(int id) =>
{
    Context context = new Context();
    var procedure = context.Procedure.FirstOrDefault(s => s.IdProcedure == id);

    if (procedure != null)
        return Results.Json(procedure, options);
    else
        return Results.NotFound($"There is not procedure with id {id}");
});

app.MapGet("/photo",
    [SwaggerOperation(
        Summary = "Получение фото с сервера",
        Description = "Вернёт файл фотографии с сервера. В параметре нужно указать полное название файла с фотографией, полученное из описания процедуры по запросам /price или /price/{id}.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(404, "There is not such photo")]
(string photoName) =>
{
    var path = $"{Environment.CurrentDirectory}\\Images\\{photoName}";
    if (File.Exists(path))
    {
        FileStream fileStream = new FileStream(path, FileMode.Open);
        return Results.File(fileStream, "image/jpeg", photoName);
    }
    else
        return Results.NotFound("There is not such photo");
});

app.MapGet("/visit",
    [SwaggerOperation(
        Summary = "Все посещения для конкретного пользователя",
        Description = "Запрос для получения всех посещений у пользователя. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(401, "Not authorize")]
[SwaggerResponse(404, "This person don't have any visitings")]
[Authorize]
(HttpContext context) =>
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    Context contextDB = new Context();

    var user = contextDB.Users.First(u => u.Token == token);
    var visitings = contextDB.Visit.Where(v => v.IdClient == user.IdClient);

    var response = new List<ResponseVisit>();
    if (visitings.Count() > 0)
    {
        foreach (var visiting in visitings)
        {
            var responseVisit = new ResponseVisit(visiting.IdVisit, visiting.Date);
            response.Add(responseVisit);
        }
        return Results.Json(response, options);
    }
    else
        return Results.NotFound("This person don't have any visitings");
});

app.MapGet("/visit/{id}",
    [SwaggerOperation(
        Summary = "Конкретное посещение конкретного пользователя",
        Description = "Запрос для получения конкретного посещения у пользователя. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(401, "Not authorize")]
[SwaggerResponse(404, "Not found visit with this id")]
[Authorize]
(int id) =>
{
    Context contextDB = new Context();

    var visit = contextDB.Visit.FirstOrDefault(v => v.IdVisit == id);
    if (visit == null)
        return Results.NotFound($"Not found visit with id {id}");

    var saleSize = contextDB.Sale.First(s => s.IdSale == visit.IdSale).SaleSize;
    var proceduresClientId = contextDB.ProceduresInVisit.Where(p => p.IdVisit == visit.IdVisit);
    var procedures = new List<ResponsePriceInVisit>();

    foreach (var procedureClientId in proceduresClientId)
    {
        var procedureId = contextDB.ProcedureClient.First(p => p.IdProcedureClient == procedureClientId.IdProcedureClient).IdProcedure;
        var procedure = contextDB.Procedure.First(p => p.IdProcedure == procedureId);
        var ResponsePriceInVisit = new ResponsePriceInVisit(procedure.IdProcedure, procedure.Cost, procedure.ProcedureName);
        procedures.Add(ResponsePriceInVisit);
    }

    var response = new ResponseVisitId(visit.Date, visit.Cost, saleSize, procedures);

    return Results.Json(response, options);
});

#endregion

#region put

app.MapPut("/update",
    [SwaggerOperation(
        Summary = "Обновить пароль или телефон",
        Description = "Обновление данных: пароля или номера телефона. Пароль должен содержать большие и мальнькие буквы, цифры, специальные символы, также он должен быть больше или равен 8 символам. Номер телефона должен быть российским. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(400, "No okay phone number")]
[SwaggerResponse(401, "Not authorize")]
[SwaggerResponse(409, "Weak password")]
[SwaggerResponse(423, "Only for clients")]
[Authorize]
(HttpContext context, string? pass, string? phone) =>
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    Context contextDB = new Context();

    var user = contextDB.Users.First(u => u.Token == token);

    var result = new { msg = "Data updated" };

    if (pass != null)
    {
        if (!Helpers.IsPasswordValid(pass, Helpers.PasswordRules.All, null) || pass.Count() < 8)
            return Results.Conflict("Weak password");
        user.Password = Helpers.HashPassword(pass);
        contextDB.Users.Update(user);
        if (phone == null)
        {
            contextDB.SaveChanges();

            return Results.Json(result,options);
        }

    }

    var client = contextDB.Clients.FirstOrDefault(c => c.IdClient == user.IdClient);
    if (client != null)
    {
        if (phone != null)
        {
            string motif = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            if (!Regex.IsMatch(phone, motif))
                return Results.BadRequest("No okay phone number");

            client.PhoneNumber = phone;
            contextDB.Clients.Update(client);
        }
    }
    else
    {
        return Results.StatusCode(423);
    }
    contextDB.SaveChanges();

    return Results.Json(result,options);
});

#endregion

#region delete

app.MapDelete("/delete",
    [SwaggerOperation(
        Summary = "Удаление аккаунта",
        Description = "Удалит с сервера следующие данные: логин, пароль, bearer token, данные для уведомлений. Запрос требует указывать bearer token пользователя в заголовке.")]
[SwaggerResponse(200, "Success")]
[SwaggerResponse(401, "Not authorize")]
[Authorize]
(HttpContext context) =>
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    Context contextDB = new Context();

    var user = contextDB.Users.First(u => u.Token == token);
    var userDevices = contextDB.UserDevices.Where(u => u.Login == user.Login);
    if (userDevices != null)
        foreach (var userDevice in userDevices)
            contextDB.UserDevices.Remove(userDevice);
    contextDB.Users.Remove(user);
    contextDB.SaveChanges();

    var result = new { msg = "User deleted" };

    return Results.Json(result);
});

#endregion

#endregion

app.Run();
