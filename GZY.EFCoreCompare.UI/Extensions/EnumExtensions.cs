using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.UI.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 返回枚举项的描述信息。
        /// </summary>
        /// <param name="value">要获取描述信息的枚举项。</param>
        /// <returns>枚举想的描述信息。</returns>
        public static string? GetDescription(this System.Enum value)
        {
            var enumType = value.GetType();
            var name = System.Enum.GetName(enumType, value);
            if (name == null) return null;
            var fieldInfo = enumType.GetField(name);
            if (fieldInfo == null) return null;
            if (Attribute.GetCustomAttribute(fieldInfo,
                typeof(DescriptionAttribute), false) is DescriptionAttribute attr)
            {
                return attr.Description;
            }
            return null;
        }

    }
}
