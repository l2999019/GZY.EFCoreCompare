using GZY.EFCoreCompare.Core.CompareInterface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.MySql
{
    public class MySqlDatabaseModelCompareFactory : IDatabaseModelCompareFactory
    {
        public IDatabaseModelFactory GetDatabaseModelFactory(DbContext context)
        {
            var typeMapper = context.GetService<IRelationalTypeMappingSource>();
            var mySqlOptions = context.GetService<IMySqlOptions>();
            var logger = context.GetService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>();
            return new GzyMySqlDatabaseModelFactory(logger, typeMapper, mySqlOptions);
        }
    }
}
