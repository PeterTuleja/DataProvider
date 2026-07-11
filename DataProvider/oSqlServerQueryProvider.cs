namespace DataProvider
{
    using Kros.KORM.Helper;
    using Kros.KORM.Materializer;
    using Kros.KORM.Query.Sql;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;

    public class oSqlServerQueryProvider : Kros.KORM.Query.SqlServerQueryProvider
    {
        public oSqlServerQueryProvider(ConnectionStringSettings connectionString, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            IModelBuilder modelBuilder, ILogger logger) : base(connectionString, sqlExpressionVisitorFactory, modelBuilder, logger)
        {
        }

        public oSqlServerQueryProvider(DbConnection connection, ISqlExpressionVisitorFactory sqlExpressionVisitorFactory,
            IModelBuilder modelBuilder, ILogger logger) : base(connection, sqlExpressionVisitorFactory, modelBuilder, logger)
        {
        }

        public IDbTransaction Transaction
        {
            get
            {
                return base.GetCurrentTransaction();
            }
        }

        public ConnectionState CurrentConnectionState
        {
            get
            {
                return base.Connection.State;
            }
        }
    }

}
