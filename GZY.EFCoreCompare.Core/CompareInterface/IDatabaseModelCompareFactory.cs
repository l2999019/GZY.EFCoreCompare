using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core.CompareInterface
{
    public interface IDatabaseModelCompareFactory
    {
        IDatabaseModelFactory GetDatabaseModelFactory(DbContext context);
    }
}
