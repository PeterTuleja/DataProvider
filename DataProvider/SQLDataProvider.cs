namespace DataProvider
{
    using Kros.KORM.Query.Sql;
    using System;
    using System.Configuration;
    using System.Data;

    public class SqlDataProvider : DataProviderBase
    {
        private ISqlExpressionVisitorFactory _sqlExpressionVisitorFactory;
        private string _fileName;
        private string _usrDirectory;
        private string _sharedFolder;


        public override string Provider
        {
            get
            {
                return "System.Data.SqlClient";
            }
        }
        public override ISqlExpressionVisitorFactory SqlExpressionVisitorFactory
        {
            get
            {
                return this._sqlExpressionVisitorFactory;
            }
        }
        /// <summary>
        /// Cesta, kam sa pri SQL zamkoch ukladaju pomocne subory.
        /// </summary>
        /// <example>
        /// x:/OmegaData/Kros/!SYSTEM/ROK_2017/USR/
        /// </example>
        //private string UsrDirectory
        //{
        //    get
        //    {
        //        if (string.IsNullOrWhiteSpace(this._usrDirectory))
        //            this._usrDirectory = PathService.BuildPath(this._sharedFolder, AutocounterAccess.BIN_NAZOV_ADRESARA_SYSTEM, System.IO.Path.GetFileNameWithoutExtension(this._fileName), AutocounterAccess.BIN_NAZOV_PODADRESARA_USR);

        //        return this._usrDirectory;
        //    }
        //}


        public SqlDataProvider(string name, string connectionString, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            string sharedFolder, string fileName) : base(name, connectionString)
        {
            this._sqlExpressionVisitorFactory = sqlExpressionVisitorFactory;
            this._sharedFolder = sharedFolder;
            this._fileName = fileName;
        }

        public SqlDataProvider(string name, IDbConnection connection, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            string sharedFolder, string fileName) : base(name, connection)
        {
            this._sqlExpressionVisitorFactory = sqlExpressionVisitorFactory;
            this._sharedFolder = sharedFolder;
            this._fileName = fileName;
        }


        internal override IDbConnection CreateConnection()
        {
            return new System.Data.SqlClient.SqlConnection(this.ConnectionString);
        }

        internal override ConnectionStringSettings CreateConnectionSettings()
        {
            return new ConnectionStringSettings(this.Name, this.ConnectionString, this.Provider);
        }

        internal override IDbCommand CreateCommand()
        {
            return new System.Data.SqlClient.SqlCommand();
        }

        internal override IDbDataParameter CreateParameter()
        {
            return new System.Data.SqlClient.SqlParameter();
        }

        //public override IDbLock CreateRecordLock(int idUzivatel, int idTabulka, int idZaznam, int idCol)
        //{
        //    return new SqlRecordLock(this, idUzivatel, idTabulka, idZaznam, idCol, this.UsrDirectory);
        //}

        //public override IDbLock CreateTableLock(int idUzivatel, int idTabulka)
        //{
        //    return new SqlTableLock(this, idUzivatel, idTabulka, this.UsrDirectory);
        //}

        protected override string DefaultSharedFolder()
        {
            if (!Directory.Exists(this._sharedFolder))
            {
                // zmenim shared folder, len ak ten, ktory mi prisiel neexistuje
                if (string.IsNullOrWhiteSpace(this._fileName))
                    this._fileName = this.ExecuteScalar("SELECT physical_name AS path FROM sys.master_files WHERE name = @1", $"{this.Name}_dat");

                return System.IO.Path.GetDirectoryName(this._fileName) + @"\";
            }
            else
                return this._sharedFolder;
        }

        public override void InitDatabaseForIdGenerator()
        {
            Kros.Data.SqlServer.SqlServerIdGenerator generator = new Kros.Data.SqlServer.SqlServerIdGenerator(ConnectionString, "T000_INI", 1);
            generator.InitDatabaseForIdGenerator();
        }

        public override bool IsMsSql()
        {
            return true;
        }


        public void InitializeSharedFolder()
        {
            this._sharedFolder = this.GetSharedFolder();

            if (!Directory.Exists(this._sharedFolder))
                throw new ArgumentException($"Zdielaný priečinok: {this._sharedFolder} neexistuje.");
        }
    }

}
