using KnifeSQLExtension.Core.Services.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.UI
{
    public class SqlSession
    {
        public IDatabaseClient? DbClient { get; set; } = null;
        public bool IsConnected { get; set; }
    }
}
