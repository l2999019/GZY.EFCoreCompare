using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.UI.Extensions
{
    public static class ServiceProviderExtensions
    {

        public static List<Type> GetAllRegisteredDbContextTypes(this IServiceCollection services)
        {
            return services
                .Where(d => d.ServiceType.IsSubclassOf(typeof(DbContext))) // 仅筛选继承 DbContext 的类型
                .Select(d => d.ServiceType)
                .Distinct()
                .ToList();
        }
      
    }
}
