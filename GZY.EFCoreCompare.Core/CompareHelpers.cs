using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RelationalEntityTypeExtensions = Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions;

namespace GZY.EFCoreCompare.Core
{
    public static class CompareHelpers
    {
        /// <summary>
        /// 从数据模型中生成表
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static string? FormSchemaTableFromModel(this IEntityType entityType)
        {
            List<string> list = Enumerable.ToList<string>(Enumerable.Select<IAnnotation, string>(Enumerable.OrderBy<IAnnotation, string>(Enumerable.Where<IAnnotation>(entityType.GetAnnotations(), (IAnnotation a) => a.Name == "Relational:ViewName" || a.Name == "Relational:ViewSchema"), (IAnnotation a) => a.Name), (IAnnotation a) => (string)a.Value));
            if (Enumerable.Any<string>(list))
            {
                return Enumerable.Last<string>(list).FormSchemaTable(Enumerable.First<string>(list));
            }
            var tableName = RelationalEntityTypeExtensions.GetTableName(entityType);
            var schema = RelationalEntityTypeExtensions.GetSchema(entityType);
            if (tableName != null && schema != null)
            {
                return schema.FormSchemaTable(tableName);
            }
            return null;
        }


        /// <summary>
        /// 从数据库生成表结构
        /// </summary>
        /// <param name="table"></param>
        /// <param name="defaultSchema"></param>
        /// <returns></returns>
        public static string FormSchemaTableFromDatabase(this DatabaseTable table, string defaultSchema)
        {
            return ((table.Schema == defaultSchema) ? null : table.Schema).FormSchemaTable(table.Name);
        }

        /// <summary>
        ///在用Schema的时候使用此选项，默认模式的Schema为空
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string FormSchemaTable(this string? schema, string table)
        {
            if (!string.IsNullOrEmpty(schema))
            {
                return schema + "." + table;
            }
            return table;
        }

        public static string NullableAsString(this bool isNullable)
        {
            if (!isNullable)
            {
                return "NOT NULL";
            }
            return "NULL";
        }

        public static StringComparison GetStringComparison(this StringComparer caseComparer)
        {
            return caseComparer.ComparerToComparison();
        }

        private static StringComparison ComparerToComparison(this StringComparer caseComparer)
        {
            if (caseComparer.Equals(StringComparer.CurrentCulture))
            {
                return 0;
            }
            if (caseComparer.Equals(StringComparer.CurrentCultureIgnoreCase))
            {
                return StringComparison.CurrentCultureIgnoreCase;
            }
            if (caseComparer.Equals(StringComparer.InvariantCulture))
            {
                return StringComparison.InvariantCulture;
            }
            if (caseComparer.Equals(StringComparer.InvariantCultureIgnoreCase))
            {
                return StringComparison.InvariantCultureIgnoreCase;
            }
            if (caseComparer.Equals(StringComparer.Ordinal))
            {
                return StringComparison.Ordinal;
            }
            if (caseComparer.Equals(StringComparer.OrdinalIgnoreCase))
            {
                return StringComparison.OrdinalIgnoreCase;
            }
            throw new ArgumentOutOfRangeException("caseComparer");
        }

        /// <summary>
        /// 转换生成的空值
        /// </summary>
        /// <param name="valGen"></param>
        /// <param name="computedColumnSql"></param>
        /// <param name="defaultValueSql"></param>
        /// <returns></returns>
        public static string ConvertNullableValueGenerated(this ValueGenerated? valGen, string? computedColumnSql, string? defaultValueSql)
        {
            if (valGen == null && defaultValueSql != null)
            {
                return 1.ToString();
            }
            if (valGen == null && computedColumnSql != null)
            {
                return 3.ToString();
            }
            return ((valGen != null) ? valGen.GetValueOrDefault().ToString() : null) ?? 0.ToString();
        }

        /// <summary>
        /// 将参照动作转换为删除行为
        /// </summary>
        /// <param name="refAct"></param>
        /// <param name="entityBehavior"></param>
        /// <returns></returns>
        public static string ConvertReferentialActionToDeleteBehavior(this ReferentialAction? refAct, DeleteBehavior entityBehavior)
        {
            if (entityBehavior == DeleteBehavior.Restrict)
            {
                ReferentialAction? referentialAction = refAct;
                ReferentialAction referentialAction2 = 0;
                if (referentialAction.GetValueOrDefault() == referentialAction2 & referentialAction != null)
                {
                    return entityBehavior.ToString();
                }
            }
            return ((refAct != null) ? refAct.GetValueOrDefault().ToString() : null) ?? 0.ToString();
        }

        /// <summary>
        /// 移除不必要的括号
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string? RemoveUnnecessaryBrackets(this string? val)
        {
            if (val == null)
            {
                return null;
            }
            while (val.Length > 1 && val[0] == '(' && val[val.Length - 1] == ')')
            {
                val = val.Substring(1, val.Length - 2);
            }
            return val;
        }
    }
}
