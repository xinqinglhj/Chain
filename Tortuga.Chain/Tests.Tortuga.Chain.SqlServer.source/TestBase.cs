using Tortuga.Chain;
using Tortuga.Chain.SqlServer;

namespace Tests
{
    public abstract class TestBase
    {
        private static readonly SqlServerDataSource s_DataSource;
        private static readonly SqlServerDataSource s_StrictDataSource;

        static TestBase()
        {
            s_DataSource = SqlServerDataSource.CreateFromConfig("SqlServerTestDatabase");
            s_StrictDataSource = s_DataSource.WithSettings(new SqlServerDataSourceSettings() { StrictMode = true });
        }

        public static SqlServerDataSource DataSource
        {
            get { return s_DataSource; }
        }

        public static SqlServerDataSource StrictDataSource
        {
            get { return s_StrictDataSource; }
        }

        public string EmployeeTableName { get { return "HR.Employee"; } }

        public string Proc1Name { get { return "Sales.CustomerWithOrdersByState"; } }
    }

}
