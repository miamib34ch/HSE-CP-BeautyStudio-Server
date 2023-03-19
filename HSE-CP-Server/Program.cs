using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using HSE_CP_Server;
using HSE_CP_Server.Models;
using HSE_CP_Server.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

// Configure the HTTP request pipeline.
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


app.MapPost("/login", (string login, string pass) =>
{
    Context context = new Context();
    var user = context.Users.FirstOrDefault(u => u.Password == pass && u.Login == login);
    if (user == null) 
        return Results.NotFound("Not good password");

    var role = context.Role.First(r => r.IdRole == user.Role).RoleName;
    var token = user.Token;

    var goodResponse = new
    {
        token = token,
        role = role,
    };

    return Results.Json(goodResponse, options);
});

app.MapPost("/registration", (string login, string pass) =>
{
    Context context = new Context();
    var user = context.Users.FirstOrDefault(u => u.Login == login);
    if (user != null)
        return Results.BadRequest("User exist yet");

    // ������� JWT-�����
    var jwt = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

    var token = new JwtSecurityTokenHandler().WriteToken(jwt);

    var User = new Users(login, pass, 2, token);
    context.Users.Add(User);
    context.SaveChanges();

    var goodResponse = new
    {
        token = token,
        role = "client",
    };

    return Results.Json(goodResponse, options);
});



app.Map("/role", [Authorize] (HttpContext context) => {
    var token = context.Response.Headers.Authorization;
    Context context2 = new Context();


    return JsonSerializer.Serialize(context2.Role.Take(context2.Role.Count()).ToList(), options); });

app.MapPost("/role", (string name) => { Context context = new Context(); context.Role.Add(new Role(0, name)); context.SaveChanges(); return "ok"; });

#endregion

app.Run();
