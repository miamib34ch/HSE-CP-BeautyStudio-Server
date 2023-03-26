using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HSE_CP_Server
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authAttributes = context.MethodInfo.GetCustomAttributes(inherit: true)
                .OfType<AuthorizeAttribute>()
                .Distinct();

            if (authAttributes.Any())
            {
                AddResponseIfNotExists(operation.Responses, "401", new OpenApiResponse { Description = "Unauthorized" });

                operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                }
            };
            }
        }

        private void AddResponseIfNotExists(IDictionary<string, OpenApiResponse> responses, string key, OpenApiResponse value)
        {
            if (!responses.ContainsKey(key))
            {
                responses.Add(key, value);
            }
        }
    }

}
