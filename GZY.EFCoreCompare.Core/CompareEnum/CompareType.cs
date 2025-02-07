using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core.CompareEnum
{
    /// <summary>
    /// 对比差异类型
    /// </summary>
    public enum CompareType
    {
        [Description("实体上下文")]
        DbContext,
        [Description("实体对象")]
        Entity,
        [Description("实体属性")]
        Property,
        [Description("数据库")]
        Database,
        [Description("表")]
        Table,
        [Description("字段")]
        Column,
        [Description("主键")]
        PrimaryKey,
        [Description("外键")]
        ForeignKey,
        [Description("索引")]
        Index
    }
}
