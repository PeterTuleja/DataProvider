namespace DataProvider
{
    using System.Configuration;
    using System.Data;
    using Kros.KORM.Query.Sql;


    public class AccessDataProvider : DataProviderBase
    {
        private string _dataSource;
        private ISqlExpressionVisitorFactory _sqlExpressionVisitorFactory;

        public string DataSource
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this._dataSource))
                    this._dataSource = new System.Data.OleDb.OleDbConnectionStringBuilder(ConnectionString).DataSource;

                return this._dataSource;
            }
        }
        public override string Provider
        {
            get
            {
                return "System.Data.OleDb";
            }
        }
        public override ISqlExpressionVisitorFactory SqlExpressionVisitorFactory
        {
            get
            {
                return this._sqlExpressionVisitorFactory;
            }
        }

        public AccessDataProvider(string name, string connectionString, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory) : base(name, connectionString)
        {
            _sqlExpressionVisitorFactory = sqlExpressionVisitorFactory;
        }

        public AccessDataProvider(string name, IDbConnection connection, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory) : base(name, connection)
        {
            _sqlExpressionVisitorFactory = sqlExpressionVisitorFactory;
        }


        internal override IDbConnection CreateConnection()
        {
            return new System.Data.OleDb.OleDbConnection(this.ConnectionString);
        }

        internal override ConnectionStringSettings CreateConnectionSettings()
        {
            return new ConnectionStringSettings(this.Name, this.ConnectionString, this.Provider);
        }

        internal override IDbCommand CreateCommand()
        {
            return new System.Data.OleDb.OleDbCommand();
        }

        internal override IDbDataParameter CreateParameter()
        {
            return new System.Data.OleDb.OleDbParameter();
        }

        //public override IDbLock CreateRecordLock(int idUzivatel, int idTabulka, int idZaznam, int idCol)
        //{
        //    return new AccessRecordLock(this.DataSource, this.GetSharedFolder, this, idUzivatel, idTabulka, idZaznam, idCol);
        //}

        //public override IDbLock CreateTableLock(int idUzivatel, int idTabulka)
        //{
        //    return new AccessTableLock(this.DataSource, this.GetSharedFolder, this, idUzivatel, idTabulka);
        //}

        protected override string DefaultSharedFolder()
        {
            return Path.GetDirectoryName(this.DataSource) + @"\";
        }

        public override void InitDatabaseForIdGenerator()
        {
            // rovnaka implementacia ako v povodnom Omega.Database (AccessDataProvider.vb);
            // MsAccessIdGenerator zije v Kros.Utils.MsAccess.dll (C:\Omega)
            Kros.Data.MsAccess.MsAccessIdGenerator generator = new Kros.Data.MsAccess.MsAccessIdGenerator(ConnectionString, "T000_INI", 1);
            generator.InitDatabaseForIdGenerator();
        }

        public override bool IsMsSql()
        {
            return false;
        }
    }

}
