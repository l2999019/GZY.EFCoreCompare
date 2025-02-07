using GZY.EFCoreCompare.Core.CompareInterface;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;

namespace GZY.EFCoreCompare.SqlServer
{
    public class SqlServerDatabaseModelCompareFactory : IDatabaseModelCompareFactory
    {
        public IDatabaseModelFactory GetDatabaseModelFactory(DbContext context)
        {
            var logger = context.GetService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>();
            var typeMapper = context.GetService<IRelationalTypeMappingSource>();
            return new SqlServerDatabaseModelFactory(logger, typeMapper);
        }
    }
}
