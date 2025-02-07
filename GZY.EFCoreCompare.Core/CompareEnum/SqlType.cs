using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core.CompareEnum
{
    public enum SqlType
    {
        /// <summary>
        ///   SqlServer数据库
        /// </summary>
        SqlServer,

        /// <summary>
        /// Sqlite数据库
        /// </summary>
        Sqlite,

        /// <summary>
        ///  PostgreSql数据库
        /// </summary>
        PostgreSql,

        /// <summary>
        ///  MySQL数据库
        /// </summary>
        MySql,

        /// <summary>
        /// 达梦数据库
        /// </summary>
        Dm,
    }

}
