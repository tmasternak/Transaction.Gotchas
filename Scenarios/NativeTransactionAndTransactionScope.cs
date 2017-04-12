using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;

namespace Scenarios
{
    [TestFixture]
    public class NativeTransactionAndTransactionScope
    {
        const string ConnectionString = @"Server=DESKTOP-G5E7IEI;Database=master;Trusted_Connection=True;";
        const string ConnectionStringReformated = @"Database=master;Server=DESKTOP-G5E7IEI;Trusted_Connection=True;";

        [Test]
        public async Task Ado_transactions_completes_independently_from_scope()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var adoTransaction = connection.BeginTransaction())
                    {
                        var nativeCommand = new SqlCommand("create table test(id int)", connection);

                        nativeCommand.ExecuteNonQuery();

                        adoTransaction.Rollback();
                    }
                }

                using (var connection = new SqlConnection(ConnectionStringReformated))
                {
                    Assert.ThrowsAsync<TransactionAbortedException>(
                        async () => await connection.OpenAsync(),
                        "TransactionScope tries to Promote adoTransaction but it's already disposed");
                }
            }
        }

    }
}
