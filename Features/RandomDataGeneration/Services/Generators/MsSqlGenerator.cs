using Bogus;
using KnifeSQLExtension.Core.Models;
using KnifeSQLExtension.Features.RandomDataGeneration.Services.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services.Generators
{
    public class MsSqlGenerator : IGenerator
    {
        public object GenerateValue(Faker faker, ColumnSchema column)
        {
            // Nullable columns — occasionally emit null
            if(column.IsNullable && faker.Random.Bool(0.1f))
                return null!;

            // Try name-based inference first
            var name = column.Name.ToLower();

            if(Regex.IsMatch(name, "email")) return faker.Internet.Email();
            if(Regex.IsMatch(name, "first.?name")) return faker.Name.FirstName();
            if(Regex.IsMatch(name, "last.?name")) return faker.Name.LastName();
            if(Regex.IsMatch(name, "phone")) return faker.Phone.PhoneNumber();
            if(Regex.IsMatch(name, "city")) return faker.Address.City();
            if(Regex.IsMatch(name, "country")) return faker.Address.Country();
            if(Regex.IsMatch(name, "zip|postal")) return faker.Address.ZipCode();
            if(Regex.IsMatch(name, "price|amount|cost")) return faker.Finance.Amount();
            if(Regex.IsMatch(name, "date|created|updated")) return faker.Date.Past();
            if(Regex.IsMatch(name, "username")) return faker.Internet.UserName();
            if(Regex.IsMatch(name, "url|website")) return faker.Internet.Url();
            if(Regex.IsMatch(name, "company")) return faker.Company.CompanyName();

            // Fall back to SQL type
            return column.SqlType.ToLower() switch
            {
                "int" or "bigint" or "smallint" => faker.Random.Int(1, 10000),
                "varchar" or "nvarchar" or "text" => faker.Lorem.Word(),
                "decimal" or "numeric" or "float" => faker.Random.Decimal(1, 1000),
                "datetime" or "datetime2" or "date" => faker.Date.Past(),
                "bit" or "boolean" => faker.Random.Bool(),
                "uniqueidentifier" or "uuid" => Guid.NewGuid(),
                _ => faker.Lorem.Word()
            };
        }
    }
}
