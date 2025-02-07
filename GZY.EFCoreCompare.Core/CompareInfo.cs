using GZY.EFCoreCompare.Core.CompareEnum;
using System.ComponentModel.DataAnnotations;

namespace GZY.EFCoreCompare.Core
{
    public class CompareInfo
    {
        /// <summary>
        /// 比较内容的表名
        /// </summary>
        [Display(Name = "数据库名")]
        public string DatabaseName { get; }
        /// <summary>
        /// 比较内容的表名
        /// </summary>
        [Display(Name = "表名")]
        public string TableName { get; }
        public CompareInfo(CompareType type, CompareState state, string name,
           CompareAttributes attribute = CompareAttributes.MatchAnything, string tablename = null, string expected = null, string found = null, string database = null)
        {
            Type = type;
            State = state;
            Name = name;
            Attribute = attribute;
            Expected = expected;
            Found = found;
            TableName = tablename;
            DatabaseName = database;
        }
        /// <summary>
        /// 比较的类型
        /// </summary>
        [Display(Name = "比较的类型")]
        public CompareType Type { get; }

        /// <summary>
        /// 比较的结果
        /// </summary>
        [Display(Name = "比较的结果")]
        public CompareState State { get; }
        /// <summary>
        /// 比较内容的名称(字段名,索引名等等)
        /// </summary>
        [Display(Name = "比较的内容")]
        public string Name { get; }

        /// <summary>
        /// 比较的特性
        /// </summary>
        [Display(Name = "比较的内容的特性")]
        public CompareAttributes Attribute { get; }

        /// <summary>
        /// 实体中的内容
        /// </summary>
        [Display(Name = "实体中的内容")]
        public string Expected { get; }

        /// <summary>
        ///数据库中的内容
        /// </summary>
        [Display(Name = "数据库中的内容")]
        public string Found { get; }
    }
}
