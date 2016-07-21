﻿using Npgsql;
using System.Collections.Generic;
using System.Data;
using Tortuga.Chain.Core;
using Tortuga.Chain.DataSources;

namespace Tortuga.Chain.PostgreSql
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="CommandExecutionToken{NpgsqlCommand, NpgsqlParameter}" />
    public class PostgreSqlCommandExecutionToken : CommandExecutionToken<NpgsqlCommand, NpgsqlParameter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlCommandExecutionToken" /> class.
        /// </summary>
        public PostgreSqlCommandExecutionToken(ICommandDataSource<NpgsqlCommand, NpgsqlParameter> dataSource, string operationName, string commandText, IReadOnlyList<NpgsqlParameter> parameters, CommandType commandType = CommandType.Text) : base(dataSource, operationName, commandText, parameters, commandType)
        {
        }
    }
}