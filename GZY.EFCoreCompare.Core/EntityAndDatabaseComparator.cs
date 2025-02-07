using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using static System.Reflection.Metadata.BlobBuilder;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RelationalPropertyExtensions = Microsoft.EntityFrameworkCore.RelationalPropertyExtensions;
using GZY.EFCoreCompare.Core.CompareEnum;
using GZY.EFCoreCompare.Core;

namespace GZY.EFCoreCompare.Core
{
    public class EntityAndDatabaseComparator
    {
        private const string NoPrimaryKey = "-没有主键-";
        private readonly IModel _model;
        private readonly string _dbContextName;
        private string _defaultSchema;
        private readonly IRelationalTypeMappingSource _relationalTypeMapping;
        private readonly StringComparer _caseComparer;
        private readonly StringComparison _caseComparison;
        private Dictionary<string, DatabaseTable> _tableViewDict;
        private bool _hasErrors;
        private DatabaseModel _databaseMode;
        public CompareInfoBuild _compareInfoBuild { get; set; }

        public EntityAndDatabaseComparator(DbContext context, DatabaseModel databaseModel, CompareEFCoreConfig config)
        {
            _model = context.Model;
            _dbContextName = context.GetType().Name;
            _relationalTypeMapping = context.GetService<IRelationalTypeMappingSource>();
            _caseComparer = config.CaseComparer;
            _compareInfoBuild = new CompareInfoBuild(_dbContextName);
            _caseComparison = _caseComparer.GetStringComparison();
            _defaultSchema = databaseModel.DefaultSchema;
            _tableViewDict = databaseModel.Tables.ToDictionary(x => x.FormSchemaTableFromDatabase(_defaultSchema), _caseComparer);
            _databaseMode = databaseModel;
        }

        public void CompareModelToDatabase()
        {

            _compareInfoBuild.MarkAsOk(CompareType.Database, _dbContextName, _databaseMode.DatabaseName);

            // return _hasErrors;
        }

        public void CompareDatabase()
        {
            //获取所有的表和视图
            var entitiesNotMappedToTableOrView = _model.GetEntityTypes().Where(x => x.GetTableName() == null).ToList();// 获取没有映射到数据库表的实体(视图)
            foreach (var entityType in _model.GetEntityTypes().Where(x => !entitiesNotMappedToTableOrView.Contains(x))) //检查每个实体是否映射到数据库表或视图
            {
                if (_tableViewDict.ContainsKey(entityType.GetTableName()))//检查表或视图是否存在
                {
                    var databaseTable = _tableViewDict[entityType.GetTableName()];
                    if (entityType.GetTableName() != null)
                    {
                        var entityTypePrimaryKey = entityType.FindPrimaryKey()?.Properties.ToList();
                        var entityTypePrimaryKeyname = NoPrimaryKey;
                        if (entityTypePrimaryKey != null)
                        {
                            entityTypePrimaryKeyname = "";
                            foreach (var property in entityTypePrimaryKey)
                            {
                                entityTypePrimaryKeyname += property.Name + ",";
                            }
                        }
                        var databasePrimaryKey = databaseTable.PrimaryKey?.Columns.ToList();
                        var databasePrimaryKeyname = NoPrimaryKey;
                        if (databasePrimaryKey != null)
                        {
                            databasePrimaryKeyname = "";
                            foreach (var property in databasePrimaryKey)
                            {
                                databasePrimaryKeyname += property.Name + ",";
                            }
                        }
                        _compareInfoBuild.CheckDifferent(CompareType.PrimaryKey, entityTypePrimaryKeyname, databasePrimaryKeyname, CompareAttributes.PrimaryKey, _caseComparison, entityType.GetTableName(), entityType.GetTableName()); //检查主键是否一致
                        CompareColumns(entityType, databaseTable);
                        CompareIndexs(entityType, databaseTable);
                        DuplicationIndexs(entityType, databaseTable);
                        CompareForeignKeys(entityType, databaseTable);
                    }
                }
                else
                {
                    _compareInfoBuild.NotInDatabase(CompareType.Entity, entityType.GetTableName(), CompareAttributes.TableName);
                }
            }

            LookForUnusedTables();
        }


        private void LookForUnusedTables()
        {
            var entitiesNotMappedToTableOrView = _model.GetEntityTypes().Where(x => x.GetTableName() != null).Select(a => a.GetTableName()).ToList();
            var tablesNotUsed = _tableViewDict.Where(p => !entitiesNotMappedToTableOrView.Contains(p.Key, _caseComparer));
            foreach (var table in tablesNotUsed)
            {
                _compareInfoBuild.ExtraInDatabase(CompareType.Table, table.Key, CompareAttributes.TableName);
            }
        }
        private void CompareColumns(IEntityType entityType, DatabaseTable table)
        {
            var columnDict = table.Columns.ToDictionary(x => x.Name, _caseComparer);
            var propertys = entityType.GetProperties();
            //This finds all the Owned Types and THP
            foreach (var property in propertys)
            {
                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()));
                if (columnName == null)
                {
                    continue;
                }
                if (columnDict.ContainsKey(columnName))
                {
                    var reColumn = GetRelationalColumn(columnName, table, false);
                    var error = ComparePropertyToColumn(table.Name, columnName, reColumn, property, columnDict[columnName]);
                }
                else
                {
                    _compareInfoBuild.NotInDatabase(CompareType.Column, columnName, CompareAttributes.ColumnName, columnName, table.Name);
                }
            }
            var propertydic = propertys.Select(a => a.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()))).ToList();
            var columnNotUsed = columnDict.Where(p => !propertydic.Contains(p.Key, _caseComparer));
            foreach (var column in columnNotUsed)
            {
                _compareInfoBuild.ExtraInDatabase(CompareType.Column, column.Value.Name, CompareAttributes.ColumnName, column.Value.Name, table.Name);
            }
        }
        private IColumnBase GetRelationalColumn(string columnName, DatabaseTable table, bool isView)
        {
            var xx = _model.GetRelationalModel().Views.ToList();
            IEnumerable<IColumnBase> columns;
            if (isView)
                columns = _model.GetRelationalModel().Views.Single(x => _caseComparer.Equals(x.Name, table.Name))
                    .Columns;
            else
            {
                columns = _model.GetRelationalModel().Tables.Single(x => _caseComparer.Equals(x.Name, table.Name))
                    .Columns;
            }
            return columns.Single(x => x.Name == columnName);
        }
        /// <summary>
        /// 判断字段属性是否一致
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="relColumn"></param>
        /// <param name="property"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool ComparePropertyToColumn(string tableName, string columnName, IColumnBase relColumn, IProperty property, DatabaseColumn column)
        {
            bool flag = _compareInfoBuild.CheckDifferent(CompareType.Column, RelationalPropertyExtensions.GetColumnType(property), column.StoreType, CompareAttributes.ColumnType, this._caseComparison, columnName, tableName);
            flag |= _compareInfoBuild.CheckDifferent(CompareType.Column, relColumn.IsNullable.NullableAsString(), column.IsNullable.NullableAsString(), CompareAttributes.Nullability, this._caseComparison, columnName, tableName);
            flag |= _compareInfoBuild.CheckDifferent(CompareType.Column, RelationalPropertyExtensions.GetComputedColumnSql(property).RemoveUnnecessaryBrackets(), column.ComputedColumnSql.RemoveUnnecessaryBrackets(), CompareAttributes.ComputedColumnSql, this._caseComparison, columnName, tableName);
            if (RelationalPropertyExtensions.GetComputedColumnSql(property) != null)
            {
                bool flag2 = flag;
                bool? isStored = RelationalPropertyExtensions.GetIsStored(property);
                string expected = ((isStored != null) ? isStored.GetValueOrDefault().ToString() : null) ?? false.ToString();
                isStored = column.IsStored;
                flag = (flag2 | _compareInfoBuild.CheckDifferent(CompareType.Column, expected, isStored.ToString(), CompareAttributes.PersistentComputedColumn, this._caseComparison, columnName, tableName));
            }
            return flag;
        }

        /// <summary>
        /// 判断表约束是否一致
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="table"></param>
        private void CompareForeignKeys(IEntityType entityType, DatabaseTable table)
        {
            var fKeyDict = table.ForeignKeys.ToList();
            var entityfkey = entityType.GetForeignKeys();
            foreach (var entityFKey in entityfkey)
            {
                var entityFKeyxprops = entityFKey.Properties;
                var allColumnNames = entityFKeyxprops
                                 .Select(a => a.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema())))
                                 .ToList();
                var fkeydata = fKeyDict.Where(a => _caseComparer.Equals(string.Join(",", a.Columns.OrderBy(a => a.Name).Select(a => a.Name).ToList()), string.Join(",", allColumnNames.OrderBy(a => a).ToList()))).FirstOrDefault();
                if (fkeydata == null)
                {
                    _compareInfoBuild.NotInDatabase(CompareType.ForeignKey, string.Join(",", allColumnNames.OrderBy(a => a).ToList()), CompareAttributes.ConstraintName, string.Join(",", allColumnNames.OrderBy(a => a).ToList()), table.Name);
                }
            }
            var tablefkey = fKeyDict.Select(a => new { fkeyname = a.Name, fkeycouml = string.Join(",", a.Columns.OrderBy(a => a.Name).Select(a => a.Name).ToList()) }).ToList();
            var entityfkeys = entityfkey.Select(a => string.Join(",", a.Properties.Select(f => f.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()))).ToList())).ToList();
            var fkeyotUsed = tablefkey.Where(p => !entityfkeys.Contains(p.fkeycouml, _caseComparer));
            foreach (var index in fkeyotUsed)
            {
                _compareInfoBuild.ExtraInDatabase(CompareType.ForeignKey, index.fkeycouml, CompareAttributes.ConstraintName, index.fkeyname, table.Name);
            }
        }
        /// <summary>
        /// 判断索引是否一致
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="table"></param>
        private void CompareIndexs(IEntityType entityType, DatabaseTable table)
        {
            var indexDict = table.Indexes.ToList();
            var entityIndexes = entityType.GetIndexes();
            foreach (var entityIdx in entityIndexes)
            {
                var entityIdxprops = entityIdx.Properties;
                var allColumnNames = entityIdxprops
                                    .Select(a => a.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema())))
                                    .ToList();

                var indexdata = indexDict.Where(a => _caseComparer.Equals(string.Join(",", a.Columns.OrderBy(a => a.Name).Select(a => a.Name).ToList()), string.Join(",", allColumnNames.OrderBy(a => a).ToList()))).FirstOrDefault();
                if (indexdata != null)
                {

                    _compareInfoBuild.CheckDifferent(CompareType.Index, entityIdx.IsUnique.ToString(), indexdata.IsUnique.ToString(), CompareAttributes.Unique, _caseComparison, entityIdx.Name, table.Name);

                }
                else
                {
                    _compareInfoBuild.NotInDatabase(CompareType.Index, string.Join(",", allColumnNames), CompareAttributes.IndexConstraintName, entityIdx.GetDatabaseName(), table.Name);
                }
            }
            var tableindex = indexDict.Select(a => new { indexname = a.Name, index = string.Join(",", a.Columns.OrderBy(a => a.Name).Select(a => a.Name).ToList()) }).ToList();
            var entityIndex = entityIndexes.Select(a => string.Join(",", a.Properties.Select(f => f.GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName(), entityType.GetSchema()))).ToList())).ToList();
            var indexNotUsed = tableindex.Where(p => !entityIndex.Contains(p.index, _caseComparer));
            foreach (var index in indexNotUsed)
            {
                _compareInfoBuild.ExtraInDatabase(CompareType.Index, index.index, CompareAttributes.IndexConstraintName, index.indexname, table.Name);
            }

        }

        /// <summary>
        /// 判断重复索引
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="table"></param>
        private void DuplicationIndexs(IEntityType entityType, DatabaseTable table)
        {
            var indexDict = table.Indexes.ToList();
            var indexdata = indexDict.Select(a => new { indexName = a.Name, indexcolumn = string.Join(",", a.Columns.OrderBy(a => a.Name).Select(a => a.Name)) }).ToList();
            var dupindex = indexdata.GroupBy(a => a.indexcolumn).Where(f => f.Count() > 1).Select(a => a.Key).ToList();
            foreach (var i in dupindex)
            {
                var idexn = indexdata.First(a => a.indexcolumn.Equals(i)).indexName;
                _compareInfoBuild.Different(CompareType.Index, i, "Duplicate Index", CompareAttributes.IndexDuplication, name: idexn, tablename: table.Name);
            }
        }
    }
}
