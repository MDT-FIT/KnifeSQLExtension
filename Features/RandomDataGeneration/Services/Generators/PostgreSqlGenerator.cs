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
    public class PostgreSqlGenerator : IGenerator
    {
        public object GenerateValue(Faker faker, ColumnSchema column)
        {
            if(column.IsNullable && faker.Random.Bool(0.1f))
                return null!;

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

            return column.SqlType.ToLower() switch
            {
                "int4" or "int" or "integer" or "int8" or "bigint" or "int2" or "smallint" => faker.Random.Int(1, 10000),
                "varchar" or "text" or "character varying" or "char" or "bpchar" => faker.Lorem.Word(),
                "numeric" or "decimal" or "float4" or "float8" or "real" or "double precision" => faker.Random.Decimal(1, 1000),
                "timestamp" or "timestamptz" or "date" => faker.Date.Past(),
                "time" or "timetz" => faker.Date.Past().TimeOfDay.ToString(),
                "bool" or "boolean" => faker.Random.Bool(),
                "uuid" => Guid.NewGuid(),
                "json" or "jsonb" => "{}",
                "interval" => TimeSpan.FromHours(faker.Random.Int(1, 100)),
                _ => faker.Lorem.Word()
            };
        }
    }
}
