using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;

namespace Scenarios
{
    [TestFixture]
    public class InDoubtSystemTransactionDoesNotMean2PCInDoubt
    {
        const string ConnectionString = @"Server=DESKTOP-G5E7IEI;Database=master;Trusted_Connection=True;";
        static readonly TimeSpan MoreThanTransactionScopeTimeout = TimeSpan.FromDays(1);

        [Test]
        public async Task TransactionInDoubt_is_thrown_on_slow_2PC_preparation()
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    await new SqlCommand("WAITFOR DELAY '00:00:01';", connection).ExecuteNonQueryAsync();
                }

                Transaction.Current.EnlistDurable(
                    Guid.NewGuid(),
                    new TestResourceManagerProxy(TestResourceManagerProxy.HangOn.Prepare),
                    EnlistmentOptions.None);

                scope.Complete();

                Assert.Throws<TransactionInDoubtException>(() => scope.Dispose(),
                    "Distributed Tx is not InDoubt but there is overlapping terminology in Ado .Net");
            }
        }

        [Test]
        public async Task Commit_has_to_succeed_on_resource_manager()
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    await new SqlCommand("WAITFOR DELAY '00:00:01';", connection).ExecuteNonQueryAsync();
                }

                Transaction.Current.EnlistDurable(
                    Guid.NewGuid(),
                    new TestResourceManagerProxy(TestResourceManagerProxy.HangOn.Commit),
                    EnlistmentOptions.None);

                scope.Complete();

                scope.Dispose();
            }
        }

        class TestResourceManagerProxy : IEnlistmentNotification
        {
            internal TestResourceManagerProxy(HangOn whereToHang)
            {
                this.whereToHang = whereToHang;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                if (whereToHang == HangOn.Prepare)
                {
                    Thread.Sleep(MoreThanTransactionScopeTimeout);
                }

                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                throw new NotImplementedException();
                if (whereToHang == HangOn.Commit)
                {
                    Thread.Sleep(MoreThanTransactionScopeTimeout);
                }

                throw new NotImplementedException();
            }

            public void Rollback(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            public void InDoubt(Enlistment enlistment)
            {
                throw new NotImplementedException();
            }

            HangOn whereToHang;

            public enum HangOn { Commit, Prepare}
        }
    }


}