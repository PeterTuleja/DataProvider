namespace DataProvider
{
    using Kros.KORM.Materializer;
    using Kros.KORM.Metadata;
    using Kros.KORM.Query;
    using Kros.KORM.Query.Sql;
    using System.Configuration;
    using System.Data.Common;


    /// <summary>

    /// Factory, ktora sa rozhoduje aky typ providera pouzit pre KORM.

    /// </summary>
    public class QueryProviderFactory : IQueryProviderFactory
    {
        private ISqlExpressionVisitorFactory _sqlExpressionVisitorFactory;


        public QueryProviderFactory(ISqlExpressionVisitorFactory sqlExpressionVisitorFactory)
        {
            this._sqlExpressionVisitorFactory = sqlExpressionVisitorFactory;
        }

        public IQueryProvider Create(ConnectionStringSettings connectionString, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
        {
            if (connectionString.ProviderName == "System.Data.SqlClient")
                return new oSqlServerQueryProvider(connectionString, _sqlExpressionVisitorFactory, modelBuilder, new Kros.KORM.Helper.Logger());
            else
                return new oMsAccessQueryProvider(connectionString, _sqlExpressionVisitorFactory, modelBuilder, new Kros.KORM.Helper.Logger());
        }

        public IQueryProvider Create(DbConnection connection, IModelBuilder modelBuilder, IDatabaseMapper databaseMapper)
        {
            if ((connection) is System.Data.SqlClient.SqlConnection)
                return new oSqlServerQueryProvider(connection, _sqlExpressionVisitorFactory, modelBuilder, new Kros.KORM.Helper.Logger());
            else
                return new oMsAccessQueryProvider(connection, _sqlExpressionVisitorFactory, modelBuilder, new Kros.KORM.Helper.Logger());
        }
    }

}
