using GZY.EFCoreCompare.Core.CompareEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core
{
    public class CompareInfoBuild
    {
        public IList<CompareInfo> _compareInfos { get; }
        private readonly string _defaultName;

        public CompareInfoBuild(string defaultName)
        {
            _defaultName = defaultName;
            _compareInfos = new List<CompareInfo>();
        }

        /// <summary>
        /// 没有对比出差异
        /// </summary>
        /// <param name="compareType"></param>
        /// <param name="expected"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public CompareInfo MarkAsOk(CompareType compareType, string expected, string name = null, string tablename = null)
        {
            var log = new CompareInfo(compareType, CompareState.Ok, name ?? _defaultName, CompareAttributes.NotSet, tablename, expected, null, _defaultName);
            _compareInfos.Add(log);
            return log;
        }
        /// <summary>
        /// 检查是否有差异
        /// </summary>
        /// <param name="compareType"></param>
        /// <param name="expected"></param>
        /// <param name="found"></param>
        /// <param name="attribute"></param>
        /// <param name="caseComparison"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckDifferent(CompareType compareType, string expected, string found, CompareAttributes attribute, StringComparison caseComparison, string name = null, string tablename = null)
        {
            if (!string.Equals(expected, found, caseComparison) &&
                !string.Equals(expected?.Replace(" ", ""), found?.Replace(" ", ""), caseComparison))
            {
                _compareInfos.Add(new CompareInfo(compareType, CompareState.Different, name ?? _defaultName, attribute, tablename, expected, found, _defaultName));

                return true;
            }
            return false;
        }

        /// <summary>
        /// 记录差异
        /// </summary>
        /// <param name="compareType"></param>
        /// <param name="expected"></param>
        /// <param name="found"></param>
        /// <param name="attribute"></param>
        /// <param name="name"></param>
        public void Different(CompareType compareType, string expected, string found, CompareAttributes attribute, string name = null, string tablename = null)
        {
            _compareInfos.Add((new CompareInfo(compareType, CompareState.Different, name ?? _defaultName, attribute, tablename, expected, found, _defaultName)));
        }
        /// <summary>
        /// 记录在数据库不存在但实体有的差异
        /// </summary>
        /// <param name="compareType"></param>
        /// <param name="expected"></param>
        /// <param name="attribute"></param>
        /// <param name="name"></param>
        public void NotInDatabase(CompareType compareType, string expected, CompareAttributes attribute = CompareAttributes.NotSet, string name = null, string tablename = null)
        {
            _compareInfos.Add((new CompareInfo(compareType, CompareState.NotInDatabase, name ?? _defaultName, attribute, tablename, expected, null, _defaultName)));
        }

        /// <summary>
        /// 记录在数据库中有但是实体没有的差异
        /// </summary>
        /// <param name="compareType"></param>
        /// <param name="found"></param>
        /// <param name="attribute"></param>
        /// <param name="name"></param>
        public void ExtraInDatabase(CompareType compareType, string found, CompareAttributes attribute, string name = null, string tablename = null)
        {
            _compareInfos.Add((new CompareInfo(compareType, CompareState.ExtraInDatabase, name ?? _defaultName, attribute, tablename, null, found, _defaultName)));
        }
    }
}
