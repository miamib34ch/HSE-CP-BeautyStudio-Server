using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using HSE_CP_Server;
using HSE_CP_Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.EnableAnnotations());
builder.Services.AddAuthentication("Bearer")  // ����� �������������� - � ������� jwt-�������
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ���������, ����� �� �������������� �������� ��� ��������� ������
            ValidateIssuer = true,
            // ������, �������������� ��������
            ValidIssuer = AuthOptions.ISSUER,
            // ����� �� �������������� ����������� ������
            ValidateAudience = true,
            // ��������� ����������� ������
            ValidAudience = AuthOptions.AUDIENCE,
            // ����� �� �������������� ����� �������������
            ValidateLifetime = true,
            // ��������� ����� ������������
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // ��������� ����� ������������
            ValidateIssuerSigningKey = true,
        };
    });      // ����������� �������������� � ������� jwt-�������
builder.Services.AddAuthorization();            // ���������� �������� �����������

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // ���������� middleware �������������� 
app.UseAuthorization();   // ���������� middleware ����������� 

#region mapping

var options = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
    WriteIndented = true
};

#region get

app.MapGet("/login", 
    [SwaggerOperation(
        Summary = "�����������",
        Description = "����������� � ���������� �� ������ � ������, ����� ����� ������������ � ��� ���� �� �������.")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(400, "Wrong password")]
    [SwaggerResponse(404, "There is not such person")]
    (string login, string pass) =>
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

app.MapGet("/price",
    [SwaggerOperation(
        Summary = "������ ���� �������� ������",
        Description = "��������� ����� ������ � ��������� ������� ������ ���������, ����� ��������.")]
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
            var responsePrice = new ResponsePrice(procedure.IdProcedure, procedure.Cost, procedure.PhotoName, procedure.ProcedureName, categorieName);
            response.Add(responsePrice);
        }
        return Results.Json(response, options);
    }
    else
        return Results.NotFound("There is not prices");
});

app.MapGet("/price/{id}",
    [SwaggerOperation(
        Summary = "���������� ���������",
        Description = "���������� ������ ���������� � ��������� �� � id.")]
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
        Summary = "��������� ���� � �������",
        Description = "����� ���� ���������� � �������. � ��������� ����� ������� ������ �������� ����� � �����������, ���������� �� �������� ��������� �� �������� /price ��� /price/{id}.")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(404, "There is not such photo")]
    (string photoName) => 
{
    var path = $"{Environment.CurrentDirectory}\\images\\{photoName}";
    if (File.Exists(path))
        return Results.File(path);
    else
        return Results.NotFound("There is not such photo");
});

app.MapGet("/visit",
    [SwaggerOperation(
        Summary = "��� ��������� ��� ����������� ������������",
        Description = "������ ��� ��������� ���� ��������� � ������������. ������ ������� ��������� bearer token ������������ � ���������.")]
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
        Summary = "���������� ��������� ����������� ������������",
        Description = "������ ��� ��������� ����������� ��������� � ������������. ������ ������� ��������� bearer token ������������ � ���������.")]
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

#region post

app.MapPost("/registration",
    [SwaggerOperation(
        Summary = "�����������",
        Description = "������ �� ����������� �� �������. ������ ������ ��������� ������� � ��������� �����, �����, ����������� �������, ����� �� ������ ���� ������ ��� ����� 8 ��������.")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(400, "User exist yet")]
    [SwaggerResponse(406, "Name can not be empty")]
    [SwaggerResponse(409, "Weak password")]
    (string login, string pass) =>
{
    Context context = new Context();
    var user = context.Users.FirstOrDefault(u => u.Login == login);
    if (user != null)
        return Results.BadRequest("User exist yet");
    if (!Helper.IsPasswordValid(pass, Helper.PasswordRules.All, null) && pass.Count() < 8)
        return Results.Conflict("Weak password");
    if (login == null || login == "")
        return Results.StatusCode(406);

    // ������� JWT-�����
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

app.MapPost("note",
    [SwaggerOperation(
        Summary = "�������� ������� �� ������",
        Description = "�������� ������� �� ������ �� ���������. ������ ������� ��������� bearer token ������������ � ���������.")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(401, "Not authorize")]
    [Authorize] 
    (HttpContext context, int idProcedure, string? massage, string? phone) => 
{
    //var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    //Context contextDB = new Context();

    //var user = contextDB.Users.First(u => u.Token == token);
});

#endregion

#region put

app.MapPut("/update",
    [SwaggerOperation(
        Summary = "�������� ������ ��� �������",
        Description = "���������� ������: ������ ��� ������ ��������. ������ ������ ��������� ������� � ��������� �����, �����, ����������� �������, ����� �� ������ ���� ������ ��� ����� 8 ��������. ����� �������� ������ ���� ����������. ������ ������� ��������� bearer token ������������ � ���������.")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(400, "No okay phone number")]
    [SwaggerResponse(401, "Not authorize")]
    [SwaggerResponse(409, "Weak password")]
    [Authorize] 
    (HttpContext context, string? pass, string? phone) => 
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

app.MapDelete("/delete",
    [SwaggerOperation(
        Summary = "�������� ��������",
        Description = "������ � ������� ��������� ������: �����, ������, bearer token, ������ ��� �����������. ������ ������� ��������� bearer token ������������ � ���������.")]
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

    return Results.Ok("User deleted");
});

#endregion

#endregion

app.Run();
