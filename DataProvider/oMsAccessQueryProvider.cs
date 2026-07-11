namespace DataProvider
{
    using Kros.KORM.Helper;
    using Kros.KORM.Materializer;
    using Kros.KORM.Query.Sql;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;

    public class oMsAccessQueryProvider : Kros.KORM.Query.MsAccess.MsAccessQueryProvider
    {
        public oMsAccessQueryProvider(ConnectionStringSettings connectionString, ISqlExpressionVisitorFactory sqlGenerator,
            IModelBuilder modelBuilder, ILogger logger) : base(connectionString, sqlGenerator, modelBuilder, logger)
        {
        }

        public oMsAccessQueryProvider(DbConnection connection, ISqlExpressionVisitorFactory sqlGenerator, 
            IModelBuilder modelBuilder, ILogger logger) : base(connection, sqlGenerator, modelBuilder, logger)
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
