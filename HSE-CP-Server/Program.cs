using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using HSE_CP_Server;
using HSE_CP_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

#region get

app.MapGet("/login", (string login, string pass) =>
{
    Context context = new Context();
    var user = context.Users.FirstOrDefault(u => u.Login == login);
    if (user == null) 
        return Results.NotFound("There is not such person");
    if (user.Password != pass)
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

app.MapGet("/price", () => 
{
    Context context = new Context();
    var procedures = context.Procedure.Select(s => s);

    var response = new List<ResponsePrice>();

    if (procedures != null)
    {
        foreach (var procedure in procedures)
        {
            var responsePrice = new ResponsePrice(procedure.IdProcedure, procedure.Cost, procedure.PhotoName, procedure.ProcedureName, procedure.IdCategorie);
            response.Add(responsePrice);
        }
        return Results.Json(response, options);
    }
    else
        return Results.NotFound("There is not prices");
});

app.MapGet("/price/{id}", (int id) => 
{
    Context context = new Context();
    var procedure = context.Procedure.FirstOrDefault(s => s.IdProcedure == id);

    if (procedure != null)
        return Results.Json(procedure, options);
    else
        return Results.NotFound($"There is not procedure with id {id}");
});

app.MapGet("/photo", (string photoName) => 
{
    var path = $"{Environment.CurrentDirectory}\\images\\{photoName}";
    if (File.Exists(path))
        return Results.File(path);
    else
        return Results.NotFound("There is not such photo");
});

app.MapGet("/visit", [Authorize] (HttpContext context) => 
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    Context contextDB = new Context();

    var user = contextDB.Users.First(u => u.Token == token);
    var visitings = contextDB.Visit.Where(v => v.IdClient == user.IdClient);

    var response = new List<ResponseVisit>();
    if (visitings != null)
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

app.MapGet("/visit/{id}", [Authorize] (int id) => 
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

#region post

app.MapPost("/registration", (string login, string pass) =>
{
    Context context = new Context();
    var user = context.Users.FirstOrDefault(u => u.Login == login);
    if (user != null)
        return Results.BadRequest("User exist yet");
    if (!Helper.IsPasswordValid(pass, Helper.PasswordRules.All, null) && pass.Count() < 8)
        return Results.Conflict("Weak password");

    // создаем JWT-токен
    var jwt = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            expires: DateTime.UtcNow.Add(TimeSpan.FromDays(AuthOptions.LIFETIME)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

    var token = new JwtSecurityTokenHandler().WriteToken(jwt);

    var User = new Users(login, pass, null, 2, token);
    context.Users.Add(User);
    context.SaveChanges();

    var goodResponse = new
    {
        token = token,
        role = "client",
    };

    return Results.Json(goodResponse, options);
});

app.MapPost("note", [Authorize] (HttpContext context, int idProcedure, string? massage, string? phone) => 
{
    //var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    //Context contextDB = new Context();

    //var user = contextDB.Users.First(u => u.Token == token);
});

#endregion

#region put

app.MapPut("/update", [Authorize] (HttpContext context, string? pass, string? phone) => 
{
    var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    Context contextDB = new Context();

    var user = contextDB.Users.First(u => u.Token == token);

    if (pass != null) 
    {
        if (!Helper.IsPasswordValid(pass, Helper.PasswordRules.All, null) && pass.Count() < 8)
            return Results.Conflict("Weak password");
        user.Password = pass;
        contextDB.Users.Update(user);
    }
    
    var client = contextDB.Clients.FirstOrDefault(c => c.IdClient == user.IdClient);
    if (client != null)
        if (phone != null) 
        {
            string motif = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            if (!Regex.IsMatch(phone, motif))
                return Results.BadRequest("No okay phone number");

            client.PhoneNumber = phone;
            contextDB.Clients.Update(client);
        }

    contextDB.SaveChanges();   

    return Results.Ok("Data updated");
});

#endregion

#region delete

app.MapDelete("/delete", [Authorize] (HttpContext context) => 
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

    return Results.Ok("User deleted");
});

#endregion

#endregion

app.Run();
