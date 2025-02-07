using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core
{
    public class CompareEFCoreConfig
    {
        public StringComparer CaseComparer { get; set; } = StringComparer.CurrentCulture;
    }
}
