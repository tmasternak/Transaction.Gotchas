using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;

namespace Scenarios
{
    [TestFixture]
    public class EscalationsWithSingleSqlServerInstance
    {
        const string ConnectionString = @"Server=localhost\SqlExpress;Database=nservicebus;Trusted_Connection=True;";
        const string ConnectionStringReformated = @"Database=nservicebus;Server=localhost\SqlExpress;Trusted_Connection=True;";

        [Test]
        public async Task Opening_two_connection_at_the_same_time_escalates_to_dtc()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var outer = new SqlConnection(ConnectionString))
                {
                    await outer.OpenAsync();

                    using (var inner = new SqlConnection(ConnectionString))
                    {
                        await inner.OpenAsync();
                    }
                }

                Assert.AreNotEqual(Guid.Empty, Transaction.Current.TransactionInformation.DistributedIdentifier);
            }
        }

        [Test]
        public async Task Opening_two_connections_one_by_one_with_different_connection_string_escalates_to_dtc()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var first = new SqlConnection(ConnectionString))
                {
                    await first.OpenAsync();
                }

                using (var second = new SqlConnection(ConnectionStringReformated))
                {
                    await second.OpenAsync();
                }

                Assert.AreNotEqual(Guid.Empty, Transaction.Current.TransactionInformation.DistributedIdentifier);
            }
        }
    }
}
