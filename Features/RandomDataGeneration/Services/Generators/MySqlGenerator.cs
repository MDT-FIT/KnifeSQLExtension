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
    public class MySqlGenerator : IGenerator
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
                "int" or "bigint" or "smallint" or "tinyint" or "mediumint" => faker.Random.Int(1, 10000),
                "varchar" or "text" or "mediumtext" or "longtext" or "char" => faker.Lorem.Word(),
                "decimal" or "numeric" or "float" or "double" => faker.Random.Decimal(1, 1000),
                "datetime" or "timestamp" or "date" => faker.Date.Past(),
                "time" => faker.Date.Past().TimeOfDay.ToString(),
                "tinyint(1)" or "bool" or "boolean" => faker.Random.Bool(),
                "json" => "{}",
                "uuid" or "char(36)" => Guid.NewGuid(),
                _ => faker.Lorem.Word()
            };
        }
    }
}
