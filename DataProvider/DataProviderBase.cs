using Kros.Data.BulkActions;
using Kros.KORM;
using Kros.KORM.Data;
using Kros.KORM.Materializer;
using Kros.KORM.MsAccess.Query.Sql;
using Kros.KORM.Query;
using Kros.KORM.Query.Expressions;
using Kros.KORM.Query.Providers;
using Kros.KORM.Query.Sql;
using Kros.Utils;
using System.Configuration;
using System.Data;
using System.Data.Common;


namespace DataProvider
{
    public abstract class DataProviderBase : IDataProvider
    {
        private class ConnectionList
        {
            private class ListMember
            {
                /// <summary>
                /// k tomuto "klucu" (spolu s <seealso cref="connection"/>) je priradeny <seealso cref="dataProvider"/>
                /// </summary>
                public string connectionString;
                public IDbConnection connection;
                /// <summary>
                /// ktory <seealso cref="IDataProvider"/> je priradeny ku zlozenemu klucu <seealso cref="connectionString"/> + <seealso cref="connection"/>
                /// </summary>
                public IDataProvider dataProvider;
            }
            private List<ListMember> _list = new List<ListMember>();

            // Cache je staticka a providery sa mozu vytvarat subezne z viacerych vlakien
            // (sluzby + GraphQL API v jednom procese) - bez zamku hrozi race na Liste.
            private readonly object _zamok = new object();

            internal void Add(string connectionString, IDbConnection connection, IDataProvider dataProvider)
            {
                lock (_zamok)
                {
                    _list.Add(new ListMember() { connectionString = connectionString, connection = connection, dataProvider = dataProvider });
                }
            }

            internal bool ContainsKey(string connectionString, IDbConnection connection)
            {
                lock (_zamok)
                {
                    return _list.Any(a => a.connectionString.Equals(connectionString) && ReferenceEquals(a.connection, connection));
                }
            }

            internal IDataProvider Item(string connectionString, IDbConnection connection)
            {
                lock (_zamok)
                {
                    ListMember foundItem = _list.FirstOrDefault(a => a.connectionString.Equals(connectionString) && ReferenceEquals(a.connection, connection));
                    return foundItem?.dataProvider;
                }
            }

            internal void Remove(string connectionString, IDbConnection connection)
            {
                lock (_zamok)
                {
                    _list.RemoveAll(a => a.connectionString.Equals(connectionString) && ReferenceEquals(a.connection, connection));
                }
            }

            internal void Clear()
            {
                lock (_zamok)
                {
                    _list.Clear();
                }
            }
        }

        private string _name;
        private string _connectionString;
        private Lazy<IDatabase> _db;
        private static ConnectionList _openConnections = new ConnectionList();

        public abstract string Provider { get; }
        public virtual string Name
        {
            get
            {
                return this._name;
            }
        }
        public abstract ISqlExpressionVisitorFactory SqlExpressionVisitorFactory { get; }
        public string ConnectionString
        {
            get
            {
                return this._connectionString;
            }
        }
        public IDatabase DB
        {
            get
            {
                return this._db.Value;
            }
        }


        public DataProviderBase(string name, string connectionString)
        {
            Check.NotNull(connectionString, nameof(connectionString));

            _name = name;
            _connectionString = connectionString;
            _db = new Lazy<IDatabase>(() => new Kros.KORM.Database(this.CreateConnectionSettings(), new QueryProviderFactory(this.SqlExpressionVisitorFactory)));
        }

        public DataProviderBase(string name, IDbConnection connection)
        {
            Check.NotNull(connection, nameof(connection));

            _name = name;
            _connectionString = connection.ConnectionString;
            _db = new Lazy<IDatabase>(() => new Kros.KORM.Database((DbConnection)connection, new QueryProviderFactory(this.SqlExpressionVisitorFactory)));
        }


        internal abstract IDbConnection CreateConnection();

        internal abstract ConnectionStringSettings CreateConnectionSettings();

        internal abstract IDbCommand CreateCommand();

        internal abstract IDbDataParameter CreateParameter();

        //public abstract IDbLock CreateRecordLock(Int32 idUzivatel, Int32 idTabulka, Int32 idZaznam, Int32 idCol);

        //public abstract IDbLock CreateTableLock(Int32 idUzivatel, Int32 idTabulka);

        protected abstract string DefaultSharedFolder();

        public abstract bool IsMsSql();




    //    public TOutputValue ExecuteStoredProcedure<TOutputValue>(string storedProcedureName, string outParamName,
    //        params StoredProcedureParameter[] @params) where TOutputValue : struct
    //    {
    //        using (var conn = this.CreateConnection())
    //        using (var cmd = conn.CreateCommand()
    //)
    //        {
    //            cmd.CommandText = storedProcedureName;
    //            cmd.CommandType = CommandType.StoredProcedure;

    //            foreach (var param in @params)
    //            {
    //                var cmdParam = cmd.CreateParameter();
    //                cmdParam.Direction = param.Direction;
    //                cmdParam.DbType = param.Type;
    //                cmdParam.ParameterName = param.Name;
    //                if (param.Direction == ParameterDirection.Input)
    //                    cmdParam.Value = param.Value;
    //                cmd.Parameters.Add(cmdParam);
    //            }

    //            if (conn.State == ConnectionState.Closed)
    //                conn.Open();
    //            cmd.ExecuteNonQuery();

    //            return (TOutputValue)(DbParameter)cmd.Parameters(outParamName).Value;
    //        }
    //    }

        public int ExecuteCommand(string query)
        {
            return this.ExecuteCommandInternall(query);
        }

        public int ExecuteCommand(string query, params object[] @params)
        {
            return this.ExecuteCommandInternall(query, @params);
        }

        private Int32 ExecuteCommandInternall(string query, params object[] @params)
        {
            using (var conn = this.CreateConnection())
            using (var cmd = conn.CreateCommand()
    )
            {
                SqlExpression expression = new SqlExpression(query, @params);
                query = this.SqlExpressionVisitorFactory.CreateVisitor(conn).GenerateSql(expression).Query;

                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                if (@params?.Count() > 0)
                    ParameterExtractingExpressionVisitor.ExtractParametersToCommand((DbCommand)cmd, expression);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public IDataReader ExecuteReader(string query)
        {
            return ExecuteReaderInternall(query);
        }

        public IDataReader ExecuteReader(string query, params object[] @params)
        {
            return ExecuteReaderInternall(query, @params);
        }

        private IDataReader ExecuteReaderInternall(string query, params object[] @params)
        {
            var conn = this.CreateConnection();
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    SqlExpression expression = new SqlExpression(query, @params);
                    query = this.SqlExpressionVisitorFactory.CreateVisitor(conn).GenerateSql(expression).Query;

                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;

                    if (@params?.Count() > 0)
                        ParameterExtractingExpressionVisitor.ExtractParametersToCommand((DbCommand)cmd, expression);

                    if (conn.State == ConnectionState.Closed)
                        conn.Open();
                    return cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch
            {
                // spojenie zatvara az reader (CloseConnection) - pri vynimke pred jeho
                // vytvorenim (GenerateSql/Open/ExecuteReader) by inak zostalo visiet
                conn.Dispose();
                throw;
            }
        }

        public string ExecuteScalar(string query)
        {
            var value = this.ExecuteScalarInternal<string>(query);
            return value?.ToString() ?? string.Empty;
        }

        public string ExecuteScalar(string query, params object[] @params)
        {
            var value = this.ExecuteScalarInternal<string>(query, @params);
            return value?.ToString() ?? string.Empty;
        }

        public T? ExecuteScalar<T>(string query) where T : struct
        {
            var value = this.ExecuteScalarInternal<T>(query);
            return value != null ? (T)value : new Nullable<T>();
        }

        public T? ExecuteScalar<T>(string query, params object[] @params) where T : struct
        {
            var value = this.ExecuteScalarInternal<T>(query, @params);
            return value != null ? (T)value : new Nullable<T>();
        }

        private object? ExecuteScalarInternal<T>(string query, params object[] @params)
        {
            var value = this.DB.Query<object>().Sql(query, @params).ExecuteScalar();
            // SQL NULL (napr. SUM nad prazdnou mnozinou riadkov) pride z ADO.NET ako DBNull.Value,
            // co nie je null - volajuci by na (T)value dostal InvalidCastException
            return value == DBNull.Value ? null : value;
        }

        public string GetSharedFolder()
        {
            var result = this.ExecuteScalar("SELECT C097_MemoA FROM T000_INI WHERE C000_ID = @1 AND C010_IDUzivatel = @2", 11088, 0);
            if (string.IsNullOrWhiteSpace(result))
                return DefaultSharedFolder();
            // Omega INI bit-flag je "0"/"1" — Convert.ToBoolean(string) hádže na "0"/"1"
            // (očakáva "True"/"False"), preto najprv na int.
            else if (!System.Convert.ToBoolean(System.Convert.ToInt32(result.INIPolozka(0, "0"))))
                return DefaultSharedFolder();
            else
            {
                var iniAdresar = result.INIPolozka(1, DefaultSharedFolder());
                if (Services.IsFolderWritable(iniAdresar))
                    return iniAdresar;
                return DefaultSharedFolder();
            }
        }

        public IModelBuilder ModelBuilder
        {
            get
            {
                return this.DB.ModelBuilder;
            }
        }

        public DbProviderFactory DbProviderFactory
        {
            get
            {
                return this.DB.DbProviderFactory;
            }
        }

        private IQuery<T> Query<T>()
        {
            return this.DB.Query<T>();
        }


        /// <summary>
        /// Factory metoda na vytvorenie instancie IDataProvider.
        /// </summary>
        /// <param name="connectionString">Connection string pre vytvorenie spojenia s databazou</param>
        /// <param name="databaseFileName">Cesta k suboru s databazou spolu s nazvom suboru.</param>
        /// <example>
        /// <code>
        /// // Access cez ACE (x64 Access Database Engine musi byt nainstalovany; Jet 4.0 bol len x86):
        /// string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=X:\Data\TEST2016.MDB;Mode=ReadWrite";
        /// var dbs = DataProviderBase.CreateDataProvider(connectionString, "X:\Data\TEST2016.MDB");
        ///
        /// string connectionString = "Data Source=SKVARKA\SQLEXPRESS;Initial Catalog=x_411673400;Integrated Security=True";
        /// var dbs = DataProviderBase.CreateDataProvider(connectionString, "X:\Data\TEST2016.MDF");
        /// </code>
        /// </example>
        public static IDataProvider CreateDataProvider(string connectionString, string databaseFileName)
        {
            // Tato funkcia sa vola pri importe cez API, takze ju nemaz, aj ked v Omege nema referenciu
            ISqlExpressionVisitorFactory sqlExpressionVisitorFactory;

            sqlExpressionVisitorFactory = GetSqlExpressionVisitorFactory(connectionString);
            return DataProviderBase.CreateDataProvider(connectionString, sqlExpressionVisitorFactory, databaseFileName, databaseFileName,
                new DefaultDirectoryInitalizer(), true);
        }

        /// <summary>
        /// Factory metoda na vytvorenie instancie IDataProvider.
        /// </summary>
        /// <param name="connectionString">Connection string pre vytvorenie spojenia s databazou</param>
        /// <param name="sqlExpressionVisitorFactory">Factory na vytvorenie implementacie <seealso cref="ISqlExpressionVisitor"></param>
        /// <param name="sharedFolder">Cesta v sietovom tvare k zdielanemu adresaru pre vsetkcyh uzivatelov</param>
        /// <param name="databaseFileName">Cesta k suboru s databazou spolu s nazvom suboru.</param>
        /// <param name="directoryInitializer">Inicializator priecinkov spojenych s DB (AID, LCK, USR)</param>
        /// <example>
        /// <code>
        /// Dim connectionString As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=X:\Data\TEST2016.MDB;Mode=ReadWrite"
        /// Dim dbs = DataProviderBase.CreateDataProvider(connectionString, New DefaultQuerySqlGenerator(), "X:\Data\TEST2016.MDB")
        ///
        /// Dim connectionString As String = "Data Source=SKVARKA\SQLEXPRESS;Initial Catalog=x_411673400;Integrated Security=True"
        /// Dim dbs = DataProviderBase.CreateDataProvider(connectionString, New DefaultQuerySqlGenerator(), "X:\Data\TEST2016.MDF")
        /// </code>
        /// </example>
        public static IDataProvider CreateDataProvider(string connectionString, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            string sharedFolder, string databaseFileName, IDirectoryInitalizer directoryInitializer, bool addConnectionToOpenConnections)
        {
            Check.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            IDataProvider db;
            var values = ParseConnectionString(connectionString);

            db = CheckOpenConnections(IsMsSql(values), connectionString, null/* TODO Change to default(_) if this is not a reference type */);
            if (db != null)
                return db;

            if (IsMsSql(values))
            {
                db = new SqlDataProvider(GetInitialCatalog(values), connectionString, sqlExpressionVisitorFactory, sharedFolder, databaseFileName);
                ((SqlDataProvider)db).InitializeSharedFolder();
            }
            else
                db = new AccessDataProvider(databaseFileName, connectionString, sqlExpressionVisitorFactory);
            if (addConnectionToOpenConnections)
                _openConnections.Add(connectionString, null/* TODO Change to default(_) if this is not a reference type */, db);

            InitializeDirectory(db, databaseFileName, directoryInitializer);
            return db;
        }

        /// <summary>
        /// Metoda na vytvorenie instancie IDataProvider.
        /// </summary>
        /// <param name="connection">Aktivny connection na databazu</param>
        /// <param name="sqlExpressionVisitorFactory">Factory na vytvorenie implementacie <seealso cref="ISqlExpressionVisitor"></param>
        /// <param name="sharedFolder">Cesta v sietovom tvare k zdielanemu adresaru pre vsetkcyh uzivatelov</param>
        /// <param name="databaseFileName">Cesta k suboru s databazou spolu s nazvom suboru.</param>
        /// <param name="directoryInitializer">Inicializator priecinkov spojenych s DB (AID, LCK, USR)</param>
        public static IDataProvider CreateDataProvider(IDbConnection connection, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            string sharedFolder, string databaseFileName, IDirectoryInitalizer directoryInitializer, bool addConnectionToOpenConnections)
        {
            Check.NotNull(connection, nameof(connection));

            IDataProvider db;
            var values = ParseConnectionString(connection.ConnectionString);

            db = CheckOpenConnections(IsMsSql(values), connection.ConnectionString, connection);
            if (db != null)
                return db;
            if (IsMsSql(values))
            {
                db = new SqlDataProvider(GetInitialCatalog(values), connection, sqlExpressionVisitorFactory, sharedFolder, databaseFileName);
                ((SqlDataProvider)db).InitializeSharedFolder();
            }
            else
                db = new AccessDataProvider(databaseFileName, connection, sqlExpressionVisitorFactory);

            if (addConnectionToOpenConnections)
                _openConnections.Add(connection.ConnectionString, connection, db);

            InitializeDirectory(db, databaseFileName, directoryInitializer);
            return db;
        }

        private static IDataProvider CheckOpenConnections(bool jeSql, string connectionString, IDbConnection connection)
        {
            IDataProvider dataProvider = null/* TODO Change to default(_) if this is not a reference type */;
            if (_openConnections.ContainsKey(connectionString, connection))
            {
                System.Linq.IQueryProvider provider = _openConnections.Item(connectionString, connection).Query<Int32>().Provider;
                ConnectionState connectionState;
                if (jeSql)
                    connectionState = ((oSqlServerQueryProvider)provider).CurrentConnectionState;
                else
                    connectionState = ((oMsAccessQueryProvider)provider).CurrentConnectionState;
                // musim skontrolovat ConnectionState pre query providera, ak by bol close tak by nefungoval ziaden query dotaz
                if (connectionState == ConnectionState.Closed)
                {
                    // stary provider zahodim a vratim nothing, co zabezpeci vytvorenie noveho, inak vratim nejaky uz vytvoreny
                    _openConnections.Remove(connectionString, connection);
                    dataProvider = null/* TODO Change to default(_) if this is not a reference type */;
                }
                else
                    dataProvider = (IDataProvider)_openConnections.Item(connectionString, connection);
            }
            return dataProvider;
        }

        /// <summary>
        /// Meotoda sa vola pri zatvarani firmy a vycisti hash tabulku s otvorenymi pripojeniami na databazu
        /// </summary>
        public static void ClearTableOfOpenConnections()
        {
            _openConnections.Clear();
        }

        /// <summary>
        /// Vrati Initial catalog (nazov databazy na servery) z rozparsovaneho connection stringu
        /// </summary>
        /// <param name="parsedConnectionString">Rozparsovany connection string</param>
        /// <returns></returns>
        private static string GetInitialCatalog(Dictionary<string, string> parsedConnectionString)
        {
            string initial = "x_00000000";
            if (parsedConnectionString.ContainsKey("INITIAL CATALOG"))
                initial = parsedConnectionString["INITIAL CATALOG"];
            return initial;
        }

        /// <summary>
        /// Inicializuje priecinky spojene s DB (AID, LCK, USR)
        /// </summary>
        private static void InitializeDirectory(IDataProvider db, string databaseName, IDirectoryInitalizer directoryInitializer)
        {
            // vytvor pomocne adresare
            var baseFolder = Path.Combine(db.GetSharedFolder(), Constants.BIN_NAZOV_ADRESARA_SYSTEM,
                System.IO.Path.GetFileNameWithoutExtension(databaseName));
            directoryInitializer.Initialize(baseFolder);
        }

        private static ISqlExpressionVisitorFactory GetSqlExpressionVisitorFactory(string connectionString)
        {
            var databaseMapper = Database.DatabaseMapper;

            if (IsMsSql(connectionString))
                return new SqlServerSqlExpressionVisitorFactory(databaseMapper);
            else
                return new MsAccessSqlExpressionVisitorFactory(databaseMapper);
        }

        private static bool IsMsSql(string connectionString)
        {
            var values = ParseConnectionString(connectionString);

            return IsMsSql(values);
        }

        private static bool IsMsSql(Dictionary<string, string> parsedConnectionString)
        {
            return !parsedConnectionString.ContainsKey("PROVIDER");
        }

        private static Dictionary<string, string> ParseConnectionString(string connectionString)
        {
            // DbConnectionStringBuilder korektne zvlada '=' v hodnotach (napr. heslo)
            // aj tokeny bez '='. Povodny Split('=') na nich padal a hodnoty upper-casoval -
            // nazov DB VELKYMI potom zlyhal v SELECT ... WHERE name = '..._dat'
            // na case-sensitive kolacii. Kluce drzime upper (tak ich cakaju volajuci),
            // hodnoty v povodnej velkosti.
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var values = new Dictionary<string, string>();
            foreach (string key in builder.Keys)
            {
                values[key.ToUpperInvariant()] = builder[key]?.ToString() ?? string.Empty;
            }
            return values;
        }

        public IBulkInsert CreateBulkInsert()
        {
            return _db.Value.CreateBulkInsert();
        }

        public Int32 ExecuteNonQuery(string query)
        {
            return _db.Value.ExecuteNonQuery(query);
        }

        public Int32 ExecuteNonQuery(string query, CommandParameterCollection parameters)
        {
            return _db.Value.ExecuteNonQuery(query, parameters);
        }

        private TResult? IDatabase_ExecuteScalar<TResult>(string query) where TResult : struct
        {
            return _db.Value.ExecuteScalar<TResult>(query);
        }

        private TResult? IDatabase_ExecuteScalar1<TResult>(string query, params object[] args) where TResult : struct
        {
            return _db.Value.ExecuteScalar<TResult>(query, args);
        }

        private string IDatabase_ExecuteScalar2(string query)
        {
            return _db.Value.ExecuteScalar(query);
        }

        private string IDatabase_ExecuteScalar3(string query, params object[] args)
        {
            return _db.Value.ExecuteScalar(query, args);
        }

        public TResult ExecuteStoredProcedure<TResult>(string storedProcedureName)
        {
            return _db.Value.ExecuteStoredProcedure<TResult>(storedProcedureName);
        }

        public TResult ExecuteStoredProcedure<TResult>(string storedProcedureName, CommandParameterCollection parameters)
        {
            return _db.Value.ExecuteStoredProcedure<TResult>(storedProcedureName, parameters);
        }

        private bool disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
            }
            disposedValue = true;
        }

        // TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        // Protected Overrides Sub Finalize()
        // ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        // Dispose(False)
        // MyBase.Finalize()
        // End Sub

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
        }

        public IBulkUpdate CreateBulkUpdate()
        {
            return _db.Value.CreateBulkUpdate();
        }

        public ITransaction BeginTransaction()
        {
            return _db.Value.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return _db.Value.BeginTransaction(isolationLevel);
        }

        public abstract void InitDatabaseForIdGenerator();

        //Services.Locks.IDbLock IDataProvider.CreateRecordLock(int idUzivatel, int idTabulka, int idZaznam, int idCol)
        //{
        //    throw new NotImplementedException();
        //}

        //Services.Locks.IDbLock IDataProvider.CreateTableLock(int idUzivatel, int idTabulka)
        //{
        //    throw new NotImplementedException();
        //}

        IBulkInsert IDatabase.CreateBulkInsert()
        {
            return _db.Value.CreateBulkInsert();
        }

        IBulkUpdate IDatabase.CreateBulkUpdate()
        {
            return _db.Value.CreateBulkUpdate();
        }

        IQuery<T> IDatabase.Query<T>()
        {
            return _db.Value.Query<T>();
        }
    }

    /// <summary>

    /// Stored procedure parameter.

    /// </summary>

    /// <example><code source="../Omega.Database/Services/Locks/SqlRecordLock.vb" region="SPParameter example" lang="VB.nET"></code></example>
    public class StoredProcedureParameter
    {

        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Parameter database type.
        /// </summary>
        public DbType Type { get; set; }

        /// <summary>
        /// Parameter direction <seealso cref="ParameterDirection"/>.
        /// </summary>
        public ParameterDirection Direction { get; set; }
    }

}

