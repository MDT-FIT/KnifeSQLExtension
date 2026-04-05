using Bogus;
using KnifeSQLExtension.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.Features.RandomDataGeneration.Services.Generator
{
    public interface IGenerator
    {
        object GenerateValue(Faker faker, ColumnSchema column);
    }
}
