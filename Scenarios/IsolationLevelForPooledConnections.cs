using System;
using System.Data.SqlClient;
using System.Transactions;
using NUnit.Framework;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Scenarios
{
    [TestFixture]
    public class IsolationLevelForPooledConnections
    {
        //HINT: adding some spaces at the end to prevent pool reuse between testcases
        const string ConnectionString = @"Server=DESKTOP-G5E7IEI;Database=master;Trusted_Connection=True;    ";

        //HINT: This work as expected on some versions of SqlServer 2014 but not on 2008 or 2016
        //      see: http://stackoverflow.com/questions/9851415/sql-server-isolation-level-leaks-across-pooled-connections
        //      Using sync version for reader's convenience. @danielmarbach don't judge me ;)
        [Test]
        public void Isolation_level_is_not_reset_when_connection_returns_to_pool()
        {
            string firstIsolationLevel, secondIsolationLevel, thirdIsolationLevel;

            using (var first = new SqlConnection(ConnectionString))
            {
                first.Open();

                firstIsolationLevel = QueryIsolationLevel(first);
            }

            using (var scope = new TransactionScope(0, new TransactionOptions {IsolationLevel = IsolationLevel.Serializable}))
            {
                using (var second = new SqlConnection(ConnectionString))
                {
                    second.Open();

                    secondIsolationLevel = QueryIsolationLevel(second);
                }

                scope.Complete();
            }

            using (var third = new SqlConnection(ConnectionString))
            {
                third.Open();

                thirdIsolationLevel = QueryIsolationLevel(third);
            }

            Assert.AreEqual("ReadCommitted", firstIsolationLevel);
            Assert.AreEqual("Serializable", secondIsolationLevel);
            Assert.AreEqual("Serializable", thirdIsolationLevel);
        }

        string QueryIsolationLevel(SqlConnection sqlConnection)
        {
            var command = new SqlCommand(@"select
                                           case transaction_isolation_level
                                                WHEN 0 THEN 'Unspecified'
                                                WHEN 1 THEN 'ReadUncommitted'
                                                WHEN 2 THEN 'ReadCommitted'
                                                WHEN 3 THEN 'RepeatableRead'
                                                WHEN 4 THEN 'Serializable'
                                                WHEN 5 THEN 'Snapshot'
                                           end as lvl, @@SPID
                                         from sys.dm_exec_sessions
                                        where session_id = @@SPID", sqlConnection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return reader.GetValue(0).ToString();
                }
            }

            throw new Exception("Could not query session IsolationLevel");
        }
    }
}