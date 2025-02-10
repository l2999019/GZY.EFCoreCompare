using GZY.EFCoreCompare.Core;
using GZY.EFCoreCompare.UI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;

namespace GZY.EFCoreCompare.UI.Areas.MyFeature.Pages;

public class CompareUIModel : PageModel
{
    private CompareEFCore _compareEFCore;
    private static List<Core.CompareInfo>? dbcompareInfos;
    public CompareUIModel(CompareEFCore compareEFCore)
    {
        _compareEFCore = compareEFCore;
    }
   
    /// <summary>
    /// 获取对比
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> OnGetSelectCompare()
    {
        var contextlist = EFCoreCompareUIExtension.DBContextTypes;
        foreach (var context in contextlist)
        {
            var dbcontext = HttpContext.RequestServices.GetService(context) as DbContext;
            if (dbcontext != null)
            {
                await _compareEFCore.CompareEfWithDbAsync(dbcontext);
            }
        }
       // await _compareEFCore.CompareEfWithDbAsync();
        var compareInfos = _compareEFCore.compareInfos;
        dbcompareInfos = compareInfos;
         var result = compareInfos.Select(a => new
        {
            a.Name,
            a.DatabaseName,
            Attribute = a.Attribute.GetDescription(),
            State = a.State.GetDescription(),
            a.Expected,
            a.Found,
            a.TableName,
            Type = a.Type.GetDescription()
        }).ToList();

        return new JsonResult(result);
    }
    public IActionResult OnGetDownLoadExecle ()
    {
        if (dbcompareInfos == null)
        {
            return new JsonResult(new {message="还未比对数据!" ,code=1});
        }
        var ms = ExcelHelper.ExportToExcelMemoryStream_EnumToDescription(dbcompareInfos);
        return File(ms.ToArray(), "application/octet-stream", "数据库与实体的差异.xlsx");
        
    }
}
