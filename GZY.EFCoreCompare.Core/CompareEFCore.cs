﻿using GZY.EFCoreCompare.Core.CompareEnum;
using GZY.EFCoreCompare.Core.CompareInterface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZY.EFCoreCompare.Core
{
    public class CompareEFCore
    {
        private readonly CompareEFCoreConfig _config;
        private readonly DbContext? _dbContext;
        public List<CompareInfo> compareInfos = new List<CompareInfo>();
        public CompareEFCore(CompareEFCoreConfig? config =null)
        {
            _config = config ?? new CompareEFCoreConfig();
        }
        public CompareEFCore(DbContext dbContext, CompareEFCoreConfig? config = null)
        {
            _dbContext = dbContext;
            _config = config ?? new CompareEFCoreConfig();
        }
        public async Task<bool> CompareEfWithDbAsync()
        {
            return await Task.Run<bool>(() => { return CompareEfWithDb(); });
        }

        public async Task<bool> CompareEfWithDbAsync(DbContext dbContexts)
        {
            return await Task.Run<bool>(() => { return CompareEfWithDb(dbContexts); });
        }

        public bool CompareEfWithDb()
        {
            if (_dbContext == null) throw new ArgumentNullException(nameof(_dbContext));
            return CompareEfWithDb(_dbContext);
        }
        public bool CompareEfWithDb(DbContext dbContexts)
        {
            if (dbContexts == null) throw new ArgumentNullException(nameof(dbContexts));
            var connectionString = dbContexts.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            return FinishRestOfCompare(connectionString, dbContexts);
        }

        private bool FinishRestOfCompare(string connectionString, DbContext dbContexts)
        {
            var databaseModel = GetDatabaseModelViaScaffolder(dbContexts, connectionString);
            var stage1Comparer = new EntityAndDatabaseComparator(dbContexts, databaseModel, _config);
            stage1Comparer.CompareDatabase();
            compareInfos.AddRange(stage1Comparer._compareInfoBuild._compareInfos);
            if (compareInfos.Count > 0)
            {
                return true;
            }

            return false;
        }
        private DatabaseModel GetDatabaseModelViaScaffolder(DbContext contexts, string connectionString)
        {
            var databaseFactory = GetDatabaseModelFactory(contexts);

            var databaseModel = databaseFactory.Create(connectionString,
                new DatabaseModelFactoryOptions(new string[] { }, new string[] { }));
            return databaseModel;
        }
        private IDatabaseModelFactory GetDatabaseModelFactory(DbContext contexts)
        {
            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            var providerName = contexts.Database.ProviderName;
            SqlType databaseType = SqlType.SqlServer;                                       // ProviderName: Microsoft.EntityFrameworkCore.SqlServer

            if (providerName?.EndsWith(SqlType.PostgreSql.ToString(), ignoreCase) ?? false) // ProviderName: Npgsql.EntityFrameworkCore.PostgreSQL
            {
                databaseType = SqlType.PostgreSql;
            }
            else if (providerName?.EndsWith(SqlType.MySql.ToString(), ignoreCase) ?? false) // ProviderName: Pomelo.EntityFrameworkCore.MySql
            {
                databaseType = SqlType.MySql;
            }
            else if (providerName?.EndsWith(SqlType.Sqlite.ToString(), ignoreCase) ?? false) // ProviderName: Microsoft.EntityFrameworkCore.Sqlite
            {
                databaseType = SqlType.Sqlite;
            }
#if NET7_0_OR_GREATER
            else if (providerName?.StartsWith(SqlType.Dm.ToString(), ignoreCase) ?? false) // ProviderName:DM.Microsoft.EntityFrameworkCore
            {
                databaseType = SqlType.Dm;
            }
#else

            else if (providerName?.EndsWith(SqlType.Dm.ToString(), ignoreCase) ?? false) // ProviderName: Microsoft.EntityFrameworkCore.Dm
            {
                databaseType = SqlType.Dm;
            }
#endif
            string namespaceSqlAdaptersTEXT = "GZY.EFCoreCompare";
            Type? dbServerType = null;

            if (databaseType == SqlType.SqlServer)
            {
                dbServerType = Type.GetType(namespaceSqlAdaptersTEXT + ".SqlServer.SqlServerDatabaseModelCompareFactory,GZY.EFCoreCompare.SqlServer");
            }
            //else if (databaseType == SqlType.PostgreSql)
            //{
            //    dbServerType = Type.GetType(namespaceSqlAdaptersTEXT + ".PostgreSql,GZY.EFCoreCompare.PostgreSql");
            //}
            else if (databaseType == SqlType.MySql)
            {
                dbServerType = Type.GetType(namespaceSqlAdaptersTEXT + ".MySql.MySqlDatabaseModelCompareFactory,GZY.EFCoreCompare.MySql");
            }
            //else if (databaseType == SqlType.Sqlite)
            //{
            //    dbServerType = Type.GetType(namespaceSqlAdaptersTEXT + ".Sqlite,GZY.EFCoreCompare.Sqlite");
            //}
            else if (databaseType == SqlType.Dm)
            {
                dbServerType = Type.GetType(namespaceSqlAdaptersTEXT + ".Dm.DmDatabaseModelCompareFactory,GZY.EFCoreCompare.Dm");
            }
            var databaseModelCompareFactory = Activator.CreateInstance(dbServerType ?? typeof(int));
            var databaseModelFactory = databaseModelCompareFactory as IDatabaseModelCompareFactory;
            return databaseModelFactory?.GetDatabaseModelFactory(contexts) ?? throw new InvalidOperationException("该数据库暂不支持,或未引用正确的包!");
        }
    }
}

    
