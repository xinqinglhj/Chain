﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Tortuga.Chain.Core;
using Tortuga.Chain.Materializers;

namespace Tortuga.Chain.SqlServer.CommandBuilders
{
    /// <summary>
    /// Class SqlServerInsertOrUpdateObject.
    /// </summary>
    internal sealed class SqlServerInsertOrUpdateObject<TArgument> : SqlServerObjectCommand<TArgument>
        where TArgument : class
    {
        readonly UpsertOptions m_Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerInsertOrUpdateObject{TArgument}"/> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="options">The options.</param>
        public SqlServerInsertOrUpdateObject(SqlServerDataSourceBase dataSource, SqlServerObjectName tableName, TArgument argumentValue, UpsertOptions options) : base(dataSource, tableName, argumentValue)
        {
            m_Options = options;
        }

        /// <summary>
        /// Prepares the command for execution by generating any necessary SQL.
        /// </summary>
        /// <param name="materializer">The materializer.</param>
        /// <returns>ExecutionToken&lt;TCommand&gt;.</returns>

        public override CommandExecutionToken<SqlCommand, SqlParameter> Prepare(Materializer<SqlCommand, SqlParameter> materializer)
        {
            if (materializer == null)
                throw new ArgumentNullException(nameof(materializer), $"{nameof(materializer)} is null.");

            var sqlBuilder = Table.CreateSqlBuilder(StrictMode);
            sqlBuilder.ApplyArgumentValue(DataSource, ArgumentValue, m_Options);
            sqlBuilder.ApplyDesiredColumns(materializer.DesiredColumns());

            var availableColumns = sqlBuilder.GetParameterizedColumns().ToList();

            var sql = new StringBuilder($"MERGE INTO {Table.Name.ToQuotedString()} target USING ");
            sql.Append("(VALUES (" + string.Join(", ", availableColumns.Select(c => c.SqlVariableName)) + ")) AS source (" + string.Join(", ", availableColumns.Select(c => c.QuotedSqlName)) + ")");
            sql.Append(" ON ");
            sql.Append(string.Join(" AND ", sqlBuilder.GetKeyColumns().ToList().Select(c => $"target.{c.QuotedSqlName} = source.{c.QuotedSqlName}")));

            sql.Append(" WHEN MATCHED THEN UPDATE SET ");
            sql.Append(string.Join(", ", sqlBuilder.GetUpdateColumns().Select(x => $"{x.QuotedSqlName} = source.{x.QuotedSqlName}")));

            var insertColumns = sqlBuilder.GetInsertColumns();
            sql.Append(" WHEN NOT MATCHED THEN INSERT (");
            sql.Append(string.Join(", ", insertColumns.Select(x => x.QuotedSqlName)));
            sql.Append(") VALUES (");
            sql.Append(string.Join(", ", insertColumns.Select(x => "source." + x.QuotedSqlName)));
            sql.Append(" )");
            sqlBuilder.BuildSelectClause(sql, " OUTPUT ", "Inserted.", null);
            sql.Append(";");

            return new SqlServerCommandExecutionToken(DataSource, "Insert or update " + Table.Name, sql.ToString(), sqlBuilder.GetParameters());
        }
    }
}
