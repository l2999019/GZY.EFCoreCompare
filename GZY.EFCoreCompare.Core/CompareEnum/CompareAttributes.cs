using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core.CompareEnum
{
    /// <summary>
    /// 对比属性
    /// </summary>
    public enum CompareAttributes
    {
        [Description("实体中不存在")]
        NotSet,//实体中不存在
        [Description("正常全匹配")]
        MatchAnything,//正常全匹配
        [Description("字段名称")]
        ColumnName,//字段名称
        [Description("字段类型")]
        ColumnType, //字段类型
        [Description("非空属性")]
        Nullability,//非空属性
        [Description("默认值")]
        DefaultValueSql, //默认值
        [Description("计算列")]
        ComputedColumnSql, //计算列
        [Description("持久化计算列")]
        PersistentComputedColumn, //持久化计算列
        [Description("生成的值")]
        ValueGenerated,//生成的值
        [Description("表名")]
        TableName,//表名
        [Description("主键")]
        PrimaryKey, //主键
        [Description("约束条件")]
        ConstraintName, //约束条件
        [Description("索引")]
        IndexConstraintName,//索引
        [Description("索引重复")]
        IndexDuplication,//索引重复
        [Description("唯一约束")]
        Unique,//唯一约束
        [Description("删除行为")]
        DeleteBehavior,//删除行为
        [Description("未映射到数据库")]
        NotMappedToDatabase //未映射到数据库

    }
}
