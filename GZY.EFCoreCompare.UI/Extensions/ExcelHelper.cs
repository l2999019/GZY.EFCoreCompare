using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace GZY.EFCoreCompare.UI.Extensions
{
    public  class ExcelHelper
    {
        public static MemoryStream ExportToExcelMemoryStream_EnumToDescription<T>(List<T> dataList, int version = 1, bool Summary = false, string lastRowData = "") where T : class
        {
            try
            {
                //【1】基于NPOI创建工作簿和工作表对象
                HSSFWorkbook hssf = new HSSFWorkbook();   //2007以下版本
                XSSFWorkbook xssf = new XSSFWorkbook();   //2007以上版本
                                                          //根据不同的office版本创建不同的工作簿对象
                IWorkbook workBook = null;
                if (version == 0)
                    workBook = hssf;
                else
                    workBook = xssf;

                //【2】创建工作表
                ISheet sheet = workBook.CreateSheet("sheet1");//请学员根据实际需要把工作表的名称，变成一个参数

                Type type = typeof(T);
                PropertyInfo[] propertyinfos = type.GetProperties().Where(x => x.GetCustomAttribute<DisplayAttribute>() != null).ToArray();//获取类型的公共属性

                //【3】循环生成列标题和设置样式
                IRow rowTitle = sheet.CreateRow(0);
                for (int i = 0; i < propertyinfos.Length; i++)
                {
                    var titile = propertyinfos[i].GetCustomAttribute<DisplayAttribute>()?.Name;
                    if (string.IsNullOrEmpty(titile)) continue;
                    ICell cell = rowTitle.CreateCell(i);   //创建单元格  propertyinfos[i].Name
                    cell.SetCellValue(titile);// 设置行标题
                    SetCellStyle(workBook, cell);                                                 //设置单元格边框 
                    SetColumnWidth(sheet, i, 20);                                                //设置列宽   
                }
                //【4】循环实体集合生成各行数据
                for (int i = 0; i < dataList.Count; i++)
                {
                    IRow row = sheet.CreateRow(i + 1);
                    for (int j = 0; j < propertyinfos.Length; j++)
                    {
                        ICell cell = row.CreateCell(j);
                        T model = dataList[i];
                        //根据泛型找到具体化的实体对象
                        string value = propertyinfos[j].GetValue(model, null)?.ToString() ?? string.Empty;//基于反射获取实体属性值
                        if (propertyinfos[j].PropertyType.IsEnum)
                        {
                            FieldInfo field = propertyinfos[j].PropertyType.GetField(value.ToString());
                            DescriptionAttribute attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                                                   .SingleOrDefault() as DescriptionAttribute;
                            value = attribute != null ? attribute.Description : value.ToString();
                        }
                        SetCellValue(cell, propertyinfos[j], value);
                        //cell.SetCellValue(value);  //赋值
                        //SetCellStyle(workBook, cell); //设置样式时数据量太多会报错故注释 The maximum number of Cell Styles was exceeded.  You can define up to 64000 style in a .xlsx Workbook
                    }
                }

                if (Summary)
                {
                    ///添加汇总列
                    sheet = workBook.GetSheetAt(0);
                    int lastRowIndex = sheet.LastRowNum;
                    IRow lastRow = sheet.CreateRow(lastRowIndex + 1);
                    ICell lastcell = lastRow.CreateCell(0);
                    lastcell.SetCellValue(lastRowData);

                    //合并汇总行的10个单元格
                    CellRangeAddress region = new CellRangeAddress(lastRowIndex + 1, lastRowIndex + 1, 0, 9);
                    sheet.AddMergedRegion(region);
                }

                MemoryStream fs = new MemoryStream();
                workBook.Write(fs);
                return fs;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// 设置cell单元格边框的公共方法
        /// </summary>
        /// <param name="workBook">接口类型的工作簿，能适应不同版本</param>
        /// <param name="cell">cell单元格对象</param>
        /// <param name="indexedColors">cell单元格背景颜色</param>
        private static void SetCellStyle(IWorkbook workBook, ICell cell, short? indexedColors = null)
        {
            ICellStyle style = workBook.CreateCellStyle();

            if (indexedColors.HasValue)
            {
                style.FillForegroundColor = indexedColors.Value; //具体数字代表的颜色看NPOI颜色对照表
                style.FillPattern = FillPattern.SolidForeground;
            }
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.Alignment = HorizontalAlignment.Center;//水平对齐
            style.VerticalAlignment = VerticalAlignment.Center;//垂直对齐
            //                                                   //设置字体
            IFont font = workBook.CreateFont();
            font.FontName = "微软雅黑";
            font.FontHeight = 15 * 15;
            font.Color = IndexedColors.Black.Index;   //字体颜色         
            font.IsBold = true;
            style.SetFont(font);


            cell.CellStyle = style;
        }
        /// <summary>
        /// 设置cell单元格列宽的公共方法
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="index">第几列</param>
        /// <param name="width">具体宽度值</param>
        private static void SetColumnWidth(ISheet sheet, int index, int width)
        {
            sheet.SetColumnWidth(index, width * 256);
        }

        /// <summary>
        /// 写Excel单元格的数据
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        private static void SetCellValue(ICell cell, PropertyInfo type, string value)
        {
            string dataType = type.PropertyType.FullName;
            if (value.Equals(string.Empty))
            {
                cell.SetCellValue(string.Empty);
            }
            else
            {
                if (dataType.IndexOf("System.DateTime") > -1)
                {
                    var dateTime = Convert.ToDateTime(value);
                    if (dateTime == DateTime.MinValue)
                        cell.SetCellValue(string.Empty);
                    else
                        cell.SetCellValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss").Replace("00:00:00", string.Empty));
                }
                else if (dataType.IndexOf("System.Decimal") > -1)
                {
                    var mformat = "N2";
                    value = Convert.ToDecimal(value).ToString(mformat);
                    cell.SetCellValue(value);
                }
                else if (dataType.IndexOf("System.Double") > -1)
                {
                    cell.SetCellType(CellType.Numeric);
                }
                else
                {
                    cell.SetCellValue(value);
                }
            }
        }
    }

}
