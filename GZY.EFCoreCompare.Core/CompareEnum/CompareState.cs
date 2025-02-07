using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core.CompareEnum
{
    /// <summary>
    /// 对比状态
    /// </summary>
    public enum CompareState
    {
        [Description("正常")]
        Ok,
        [Description("未比较")]
        NotChecked,
        [Description("存在不同")]
        Different, //存在不同
        [Description("数据库中没有")]
        NotInDatabase, //数据库中没有
        [Description("数据库中有,实体没有")]
        ExtraInDatabase //数据库中有
    }
}
