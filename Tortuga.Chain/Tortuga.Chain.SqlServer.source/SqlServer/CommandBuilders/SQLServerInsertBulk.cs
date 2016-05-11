﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain.CommandBuilders;
using Tortuga.Chain.Core;
using Tortuga.Chain.Metadata;

namespace Tortuga.Chain.SqlServer.CommandBuilders
{
    /// <summary>
    /// This class is used to perform bulk inserts
    /// </summary>
    /// <seealso cref="DbOperationBuilder{SqlConnection, SqlTransaction}" />
    public sealed class SqlServerInsertBulk : DbOperationBuilder<SqlConnection, SqlTransaction>
    {
        readonly IDataReader m_Source;
        readonly SqlServerDataSourceBase m_DataSource;
        readonly SqlBulkCopyOptions m_Options;
        readonly TableOrViewMetadata<SqlServerObjectName, SqlDbType> m_Table;
        private int? m_BatchSize;
        private bool m_EnableStreaming;
        private int? m_NotifyAfter;
        private SqlRowsCopiedEventHandler m_EventHandler;

        internal SqlServerInsertBulk(SqlServerDataSourceBase dataSource, SqlServerObjectName tableName, DataTable dataTable, SqlBulkCopyOptions options) : base(dataSource)
        {
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource), $"{nameof(dataSource)} is null.");
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), $"{nameof(dataTable)} is null.");

            m_DataSource = dataSource;
            m_Source = dataTable.CreateDataReader();
            m_Options = options;
            m_Table = dataSource.DatabaseMetadata.GetTableOrView(tableName);
            if (!m_Table.IsTable)
                throw new MappingException($"Cannot perform a bulk insert into the view {m_Table.Name}");
        }

        internal SqlServerInsertBulk(SqlServerDataSourceBase dataSource, SqlServerObjectName tableName, IDataReader dataReader, SqlBulkCopyOptions options) : base(dataSource)
        {
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource), $"{nameof(dataSource)} is null.");
            if (dataReader == null)
                throw new ArgumentNullException(nameof(dataReader), $"{nameof(dataReader)} is null.");

            m_DataSource = dataSource;
            m_Source = dataReader;
            m_Options = options;
            m_Table = dataSource.DatabaseMetadata.GetTableOrView(tableName);
            if (!m_Table.IsTable)
                throw new MappingException($"Cannot perform a bulk insert into the view {m_Table.Name}");
        }

        /// <summary>
        /// Modifies the batch size.
        /// </summary>
        /// <param name="batchSize">Size of the batch.</param>
        /// <returns>SqlServerInsertBulk.</returns>
        public SqlServerInsertBulk WithBatchSize(int batchSize)
        {
            m_BatchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Prepares the command for execution by generating any necessary SQL.
        /// </summary>
        /// <returns>ExecutionToken&lt;TCommand&gt;.</returns>
        public override OperationExecutionToken<SqlConnection, SqlTransaction> Prepare()
        {
            return new OperationExecutionToken<SqlConnection, SqlTransaction>(m_DataSource, "Bulk Insert into " + m_Table.Name);
        }


        /// <summary>
        /// Implementation the specified operation.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        protected override int? Implementation(SqlConnection connection, SqlTransaction transaction)
        {
            using (var bcp = new SqlBulkCopy(connection, m_Options, transaction))
            {
                SetupBulkCopy(bcp);

                bcp.WriteToServer(m_Source);
                return m_Source.RecordsAffected;
            }
        }

        private void SetupBulkCopy(SqlBulkCopy bcp)
        {
            bcp.DestinationTableName = m_Table.Name.ToQuotedString();
            if (m_BatchSize.HasValue)
                bcp.BatchSize = m_BatchSize.Value;
            bcp.EnableStreaming = m_EnableStreaming;

            if (m_EventHandler != null)
                bcp.SqlRowsCopied += m_EventHandler;
            if (m_NotifyAfter.HasValue)
                bcp.NotifyAfter = m_NotifyAfter.Value;


            for (var i = 0; i < m_Source.FieldCount; i++)
                bcp.ColumnMappings.Add(m_Source.GetName(i), m_Source.GetName(i));
        }

        /// <summary>
        /// Implementation the specified operation.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;System.Nullable&lt;System.Int32&gt;&gt;.</returns>
        protected override async Task<int?> ImplementationAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
        {
            using (var bcp = new SqlBulkCopy(connection, m_Options, transaction))
            {
                SetupBulkCopy(bcp);

                await bcp.WriteToServerAsync(m_Source).ConfigureAwait(false);
                return m_Source.RecordsAffected;
            }
        }

        /// <summary>
        /// Enables streaming.
        /// </summary>
        /// <returns>SqlServerInsertBulk.</returns>
        /// <remarks>When EnableStreaming is true, SqlBulkCopy reads from an IDataReader object using SequentialAccess, optimizing memory usage by using the IDataReader streaming capabilities. When it’s set to false, the SqlBulkCopy class loads all the data returned by the IDataReader object into memory before sending it to SQL Server or SQL Azure.</remarks>
        public SqlServerInsertBulk WithStreaming()
        {
            m_EnableStreaming = true;
            return this;
        }

        /// <summary>
        /// After every [notifyAfter] records, the event handler will be fired.
        /// </summary>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="notifyAfter">The notify after.</param>
        /// <returns>SqlServerInsertBulk.</returns>
        public SqlServerInsertBulk WithNotifications(SqlRowsCopiedEventHandler eventHandler, int notifyAfter)
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler), $"{nameof(eventHandler)} is null.");
            if (notifyAfter <= 0)
                throw new ArgumentException($"{nameof(notifyAfter)} must be greater than 0.", nameof(notifyAfter));
            m_EventHandler = eventHandler;
            m_NotifyAfter = notifyAfter;
            return this;
        }

        /// <summary>
        /// Returns the column associated with the column name.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        /// <remarks>
        /// If the column name was not found, this will return null
        /// </remarks>
        public override ColumnMetadata TryGetColumn(string columnName)
        {
            return m_Table.Columns.TryGetColumn(columnName);
        }
    }
}
