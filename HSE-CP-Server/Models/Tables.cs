using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HSE_CP_Server.Models
{
    public class Users
    {
        [Key]
        public string Login { get; set; }
        public string Password { get; set; }
        public int? IdClient { get; set; }
        public int Role { get; set; }
        public string Token { get; set; }

        public Users(string Login, string Password, int? IdClient, int Role, string Token)
        {
            this.Login = Login;
            this.Password = Password;
            if (IdClient != null)
                this.IdClient = IdClient;
            this.Role = Role;
            this.Token = Token;
        }
    }

    public class Role 
    {
        [Key]
        public int IdRole { get; set; }
        public string RoleName { get; set; }
        public Role(int IdRole, string RoleName)
        {
            this.IdRole = IdRole;
            this.RoleName = RoleName;
        }
    }

    public class Clients 
    {
        [Key]
        public int IdClient { get; set; }
        public int? IdSkin { get; set; }
        public int? IdMethod { get; set; }
        public string? Date { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? FatherName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AdditionalInfo { get; set; }
        public string? Agreement { get; set; }
        public string? Alergies { get; set; }

        public Clients(int IdClient, int? IdSkin, int? IdMethod, string? Date, string? Name, string? Surname, string? FatherName, string? PhoneNumber, string? AdditionalInfo, string? Agreement, string? Alergies) 
        { 
            this.IdClient = IdClient;
            if (IdSkin != null)
                this.IdSkin = IdSkin;
            if (IdMethod != null)
                this.IdMethod = IdMethod;
            if (Date != null)
                this.Date = Date;
            if (Name != null)
                this.Name = Name;
            if (Surname != null)
                this.Surname = Surname;
            if (FatherName != null)
                this.FatherName = FatherName;
            if (PhoneNumber != null)
                this.PhoneNumber = PhoneNumber;
            if (AdditionalInfo != null)
                this.AdditionalInfo = AdditionalInfo;
            if (Agreement != null)
                this.Agreement = Agreement;
            if (AdditionalInfo != null)
                this.AdditionalInfo = AdditionalInfo;
            if (Alergies != null)
                this.Alergies = Alergies;
        }
    }

    public class Sale
    {
        [Key]
        public int IdSale { get; set; }
        public double SaleSize { get; set; }

        public Sale(int IdSale, double SaleSize)
        {
            this.IdSale = IdSale;
            this.SaleSize = SaleSize;
        }
    }

    public class Visit 
    {
        [Key]
        public int IdVisit { get; set; }
        public int IdClient { get; set; }
        public int IdSale { get; set; }
        public int IdNeedle { get; set; }
        public string Date { get; set; }
        public double Cost { get; set; }
        public string? AdditionalInfo { get; set; }

        public Visit(int IdVisit, int IdClient, int IdSale, int IdNeedle, string Date, double Cost, string? AdditionalInfo) 
        { 
            this.IdVisit = IdVisit;
            this.IdClient = IdClient;
            this.IdSale = IdSale;
            this.IdNeedle = IdNeedle;
            this.Date = Date;
            this.Cost = Cost;
            if (AdditionalInfo != null)
                this.AdditionalInfo = AdditionalInfo;
        }
    }

    [PrimaryKey(nameof(IdVisit), nameof(IdProcedureClient))]
    public class ProceduresInVisit
    {
        public int IdVisit { get; set; }
        public int IdProcedureClient { get; set; }

        public ProceduresInVisit(int IdVisit, int IdProcedureClient)
        {
            this.IdVisit = IdVisit;
            this.IdProcedureClient = IdProcedureClient;
        }
    }

    [PrimaryKey(nameof(Login), nameof(IdPhone))]
    public class UserDevices
    {
        public string Login { get; set; }
        public string IdPhone { get; set; }
        public string? IdService { get; set; }

        public UserDevices(string Login, string IdPhone, string? IdService)
        {
            this.Login = Login;
            this.IdPhone = IdPhone;
            if (IdService != null) 
                this.IdService = IdService;
        }
    }

    public class ProcedureClient
    {
        [Key]
        public int IdProcedureClient { get; set; }
        public int IdProcedure { get; set; }
        public string? AdditionalInfo { get; set; }

        public ProcedureClient(int IdProcedure, int IdProcedureClient, string? AdditionalInfo)
        {
            this.IdProcedure = IdProcedure;
            this.IdProcedureClient = IdProcedureClient;
            if (AdditionalInfo != null)
                this.AdditionalInfo = AdditionalInfo;
        }
    }

    public class Procedure
    {
        [Key]
        public int IdProcedure { get; set; }
        public double Cost { get; set;}
        public string? PhotoName { get; set;}
        public string ProcedureName { get; set;}
        public string? ProcedureInfo { get; set;}
        public int IdCategorie { get; set;}

        public Procedure(int IdProcedure, double Cost, string? PhotoName, string ProcedureName, string? ProcedureInfo, int IdCategorie)
        {
            this.IdProcedure = IdProcedure;
            this.Cost = Cost;
            this.ProcedureName = ProcedureName;
            if (PhotoName != null) 
                this.PhotoName = PhotoName;
            if (ProcedureInfo != null)
                this.ProcedureInfo = ProcedureInfo;
            this.IdCategorie = IdCategorie;
        }
    }

    public class ProcedureCategorie
    {
        [Key]
        public int IdCategorie { get; set; }
        public string NameCategorie { get; set; }

        public ProcedureCategorie(int IdCategorie, string NameCategorie)
        {
            this.IdCategorie = IdCategorie;
            this.NameCategorie = NameCategorie;
        }
    }
    /*
        public class Skin
        {
            [Key]
            public int IdSkin { get; set; }
            public string SkinName { get; set; }
            public Skin(int IdSkin, string SkinName)
            {
                this.IdSkin = IdSkin;
                this.SkinName = SkinName;
            }
        }

        public class Method
        {
            [Key]
            public int IdMethod { get; set; }
            public string MethodName { get; set; }

            public Method(int IdMethod, string MethodName)
            {
                this.IdMethod = IdMethod;
                this.MethodName = MethodName;
            }
        }

        public class Needle
        {
            [Key]
            public int IdNeedle { get; set; }
            public string NeedleName { get; set; }

            public Needle(int IdNeedle, string NeedleName)
            {
                this.IdNeedle = IdNeedle;
                this.NeedleName = NeedleName;
            }
        }

        public class Pigments
        {
            [Key]
            public int IdPigments { get; set; }
            public string PigmentsName { get; set; }

            public Pigments(int IdPigments, string PigmentsName)
            {
                this.IdPigments = IdPigments;
                this.PigmentsName = PigmentsName;
            }
        }

        [PrimaryKey(nameof(IdPigments), nameof(IdProcedureClient))]
        public class PigmentsInProcedures
        {
            public int IdPigments { get; set; }
            public int IdProcedureClient { get; set; }

            public PigmentsInProcedures(int IdPigments, int IdProcedureClient)
            {
                this.IdPigments = IdPigments;
                this.IdProcedureClient = IdProcedureClient;
            }
        }
    */
}
