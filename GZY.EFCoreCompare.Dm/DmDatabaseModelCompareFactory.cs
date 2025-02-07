using GZY.EFCoreCompare.Core.CompareInterface;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Dm.Scaffolding.Internal;

namespace GZY.EFCoreCompare.Dm
{
    public class DmDatabaseModelCompareFactory : IDatabaseModelCompareFactory
    {
        public IDatabaseModelFactory GetDatabaseModelFactory(DbContext context)
        {
            var logger = context.GetService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>();
            return new DmDatabaseModelFactory(logger);
        }
    }
}
