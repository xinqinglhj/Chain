using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain.CommandBuilders;

namespace Tortuga.Chain.Materializers
{
    /// <summary>
    /// Materializes the result set as a floating point number.
    /// </summary>
    /// <typeparam name="TCommandType">The type of the t command type.</typeparam>
    /// <typeparam name="TParameterType">The type of the t parameter type.</typeparam>
    public class DoubleMaterializer<TCommandType, TParameterType> : SingleColumnMaterializer<TCommandType, TParameterType, double> where TCommandType : DbCommand
        where TParameterType : DbParameter
    {
        /// <summary>
        /// </summary>
        /// <param name="commandBuilder">The command builder.</param>
        /// <param name="columnName">Name of the desired column.</param>
        public DoubleMaterializer(DbCommandBuilder<TCommandType, TParameterType> commandBuilder, string columnName = null)
            : base(commandBuilder, columnName)
        { }

        /// <summary>
        /// Execute the operation synchronously.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DataException">Unexpected null result</exception>
        public override double Execute(object state = null)
        {
            object temp = null;
            ExecuteCore(cmd => temp = cmd.ExecuteScalar(), state);
            if (temp == DBNull.Value)
                throw new MissingDataException("Unexpected null result");

            return (double)temp;
        }


        /// <summary>
        /// Execute the operation asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="state">User defined state, usually used for logging.</param>
        /// <returns></returns>
        /// <exception cref="DataException">Unexpected null result</exception>
        public override async Task<double> ExecuteAsync(CancellationToken cancellationToken, object state = null)
        {
            object temp = null;
            await ExecuteCoreAsync(async cmd => temp = await cmd.ExecuteScalarAsync(cancellationToken), cancellationToken, state).ConfigureAwait(false);
            if (temp == DBNull.Value)
                throw new MissingDataException("Unexpected null result");

            return (double)temp;
        }
    }
}
