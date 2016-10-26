﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Tortuga.Anchor;
using Tortuga.Chain.Metadata;

namespace Tortuga.Chain.SqlServer
{

    /// <summary>
    /// Class SqlServerMetadataCache.
    /// </summary>
    public sealed class SqlServerMetadataCache : AbstractSqlServerMetadataCache<SqlDbType>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMetadataCache"/> class.
        /// </summary>
        /// <param name="connectionBuilder">The connection builder.</param>
        public SqlServerMetadataCache(SqlConnectionStringBuilder connectionBuilder) : base(connectionBuilder)
        {
        }


        /// <summary>
        /// Returns the user's default schema.
        /// </summary>
        /// <returns></returns>
        public string DefaultSchema
        {
            get
            {
                if (m_DefaultSchema == null)
                {
                    using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand("SELECT SCHEMA_NAME () AS DefaultSchema", con))
                        {
                            m_DefaultSchema = (string)cmd.ExecuteScalar();
                        }
                    }
                }
                return m_DefaultSchema;
            }
        }







        /// <summary>
        /// Preloads the stored procedures.
        /// </summary>
        public override void PreloadStoredProcedures()
        {
            const string StoredProcedureSql =
            @"SELECT 
				s.name AS SchemaName,
				sp.name AS Name
				FROM SYS.procedures sp
				INNER JOIN sys.schemas s ON sp.schema_id = s.schema_id;";


            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(StoredProcedureSql, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetString(reader.GetOrdinal("SchemaName"));
                            var name = reader.GetString(reader.GetOrdinal("Name"));
                            GetStoredProcedure(new SqlServerObjectName(schema, name));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Preloads metadata for all tables.
        /// </summary>
        /// <remarks>This is normally used only for testing. By default, metadata is loaded as needed.</remarks>
        public override void PreloadTables()
        {
            const string tableList = "SELECT t.name AS Name, s.name AS SchemaName FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id=s.schema_id ORDER BY s.name, t.name";

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(tableList, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetString(reader.GetOrdinal("SchemaName"));
                            var name = reader.GetString(reader.GetOrdinal("Name"));
                            GetTableOrView(new SqlServerObjectName(schema, name));
                        }
                    }
                }
            }

        }
        /// <summary>
        /// Preloads the user defined types.
        /// </summary>
        /// <remarks>This is normally used only for testing. By default, metadata is loaded as needed.</remarks>
        public override void PreloadUserDefinedTypes()
        {
            const string tableList = @"SELECT s.name AS SchemaName, t.name AS Name FROM sys.types t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE	t.is_user_defined = 1;";

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(tableList, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetString(reader.GetOrdinal("SchemaName"));
                            var name = reader.GetString(reader.GetOrdinal("Name"));
                            GetUserDefinedType(new SqlServerObjectName(schema, name));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Preloads the table value functions.
        /// </summary>
        public override void PreloadTableFunctions()
        {
            const string TvfSql =
                @"SELECT 
				s.name AS SchemaName,
				o.name AS Name,
				o.object_id AS ObjectId
				FROM sys.objects o
				INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
				WHERE o.type in ('TF', 'IF', 'FT')";


            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(TvfSql, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetString(reader.GetOrdinal("SchemaName"));
                            var name = reader.GetString(reader.GetOrdinal("Name"));
                            GetTableFunction(new SqlServerObjectName(schema, name));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Preloads metadata for all views.
        /// </summary>
        /// <remarks>This is normally used only for testing. By default, metadata is loaded as needed.</remarks>
        public override void PreloadViews()
        {
            const string tableList = "SELECT t.name AS Name, s.name AS SchemaName FROM sys.views t INNER JOIN sys.schemas s ON t.schema_id=s.schema_id ORDER BY s.name, t.name";

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(tableList, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schema = reader.GetString(reader.GetOrdinal("SchemaName"));
                            var name = reader.GetString(reader.GetOrdinal("Name"));
                            GetTableOrView(new SqlServerObjectName(schema, name));
                        }
                    }
                }
            }

        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        internal static SqlDbType? TypeNameToSqlDbType(string typeName)
        {
            switch (typeName)
            {
                case "bigint": return SqlDbType.BigInt;
                case "binary": return SqlDbType.Binary;
                case "bit": return SqlDbType.Bit;
                case "char": return SqlDbType.Char;
                case "date": return SqlDbType.Date;
                case "datetime": return SqlDbType.DateTime;
                case "datetime2": return SqlDbType.DateTime2;
                case "datetimeoffset": return SqlDbType.DateTimeOffset;
                case "decimal": return SqlDbType.Decimal;
                case "float": return SqlDbType.Float;
                //case "geography": m_SqlDbType = SqlDbType.; 
                //case "geometry": m_SqlDbType = SqlDbType; 
                //case "hierarchyid": m_SqlDbType = SqlDbType.; 
                case "image": return SqlDbType.Image;
                case "int": return SqlDbType.Int;
                case "money": return SqlDbType.Money;
                case "nchar": return SqlDbType.NChar;
                case "ntext": return SqlDbType.NText;
                case "numeric": return SqlDbType.Decimal;
                case "nvarchar": return SqlDbType.NVarChar;
                case "real": return SqlDbType.Real;
                case "smalldatetime": return SqlDbType.SmallDateTime;
                case "smallint": return SqlDbType.SmallInt;
                case "smallmoney": return SqlDbType.SmallMoney;
                //case "sql_variant": m_SqlDbType = SqlDbType; 
                //case "sysname": m_SqlDbType = SqlDbType; 
                case "text": return SqlDbType.Text;
                case "time": return SqlDbType.Time;
                case "timestamp": return SqlDbType.Timestamp;
                case "tinyint": return SqlDbType.TinyInt;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                case "varbinary": return SqlDbType.VarBinary;
                case "varchar": return SqlDbType.VarChar;
                case "xml": return SqlDbType.Xml;
            }

            return null;
        }



        List<ColumnMetadata<SqlDbType>> GetColumns(int objectId)
        {
            const string ColumnSql =
                @"WITH    PKS
						  AS ( SELECT   c.name ,
										1 AS is_primary_key
							   FROM     sys.indexes i
										INNER JOIN sys.index_columns ic ON i.index_id = ic.index_id
																		   AND ic.object_id = @ObjectId
										INNER JOIN sys.columns c ON ic.column_id = c.column_id
																	AND c.object_id = @ObjectId
							   WHERE    i.is_primary_key = 1
										AND ic.is_included_column = 0
										AND i.object_id = @ObjectId
							 )
					SELECT  c.name AS ColumnName ,
							c.is_computed ,
							c.is_identity ,
							c.column_id ,
							Convert(bit, ISNULL(PKS.is_primary_key, 0)) AS is_primary_key,
							COALESCE(t.name, t2.name) AS TypeName,
							c.is_nullable,
		                    CONVERT(INT, t.max_length) AS max_length, 
		                    CONVERT(INT, t.precision) AS precision,
		                    CONVERT(INT, t.scale) AS scale
					FROM    sys.columns c
							LEFT JOIN PKS ON c.name = PKS.name
							LEFT JOIN sys.types t on c.system_type_id = t.user_type_id
							LEFT JOIN sys.types t2 ON c.user_type_id = t2.user_type_id
                            WHERE   object_id = @ObjectId;";

            var columns = new List<ColumnMetadata<SqlDbType>>();
            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(ColumnSql, con))
                {
                    cmd.Parameters.AddWithValue("@ObjectId", objectId);
                    using (var reader = cmd.ExecuteReader(/*CommandBehavior.SequentialAccess*/))
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(reader.GetOrdinal("ColumnName"));
                            var computed = reader.GetBoolean(reader.GetOrdinal("is_computed"));
                            var primary = reader.GetBoolean(reader.GetOrdinal("is_primary_key"));
                            var isIdentity = reader.GetBoolean(reader.GetOrdinal("is_identity"));
                            var typeName = reader.IsDBNull(reader.GetOrdinal("TypeName")) ? null : reader.GetString(reader.GetOrdinal("TypeName"));
                            var isNullable = reader.GetBoolean(reader.GetOrdinal("is_nullable"));
                            int? maxLength = reader.GetInt32(reader.GetOrdinal("max_length"));
                            int? precision = reader.GetInt32(reader.GetOrdinal("precision"));
                            int? scale = reader.GetInt32(reader.GetOrdinal("scale"));
                            string fullTypeName;
                            AdjustTypeDetails(typeName, ref maxLength, ref precision, ref scale, out fullTypeName);

                            columns.Add(new ColumnMetadata<SqlDbType>(name, computed, primary, isIdentity, typeName, TypeNameToSqlDbType(typeName), "[" + name + "]", isNullable, maxLength, precision, scale, fullTypeName));
                        }
                    }
                }
            }
            return columns;
        }

        internal override TableFunctionMetadata<SqlServerObjectName, SqlDbType> GetTableFunctionInternal(SqlServerObjectName tableFunctionName)
        {
            const string TvfSql =
                @"SELECT 
				s.name AS SchemaName,
				o.name AS Name,
				o.object_id AS ObjectId
				FROM sys.objects o
				INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
				WHERE o.type in ('TF', 'IF', 'FT') AND s.name = @Schema AND o.Name = @Name";

            /*
             * TF = SQL table-valued-function
             * IF = SQL inline table-valued function
             * FT = Assembly (CLR) table-valued function
             */


            string actualSchema;
            string actualName;
            int objectId;

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(TvfSql, con))
                {
                    cmd.Parameters.AddWithValue("@Schema", tableFunctionName.Schema ?? DefaultSchema);
                    cmd.Parameters.AddWithValue("@Name", tableFunctionName.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new MissingObjectException($"Could not find table valued function {tableFunctionName}");
                        actualSchema = reader.GetString(reader.GetOrdinal("SchemaName"));
                        actualName = reader.GetString(reader.GetOrdinal("Name"));
                        objectId = reader.GetInt32(reader.GetOrdinal("ObjectId"));
                    }
                }
            }
            var objectName = new SqlServerObjectName(actualSchema, actualName);

            var columns = GetColumns(objectId);
            var parameters = GetParameters(objectName.ToString(), objectId);

            return new TableFunctionMetadata<SqlServerObjectName, SqlDbType>(objectName, parameters, columns);
        }

        List<ParameterMetadata<SqlDbType>> GetParameters(string procedureName, int objectId)
        {
            try
            {
                const string ParameterSql =
                    @"SELECT  p.name AS ParameterName ,
            COALESCE(t.name, t2.name) AS TypeName,
			COALESCE(t.is_nullable, t2.is_nullable)  as is_nullable,
		    CONVERT(INT, t.max_length) AS max_length, 
		    CONVERT(INT, t.precision) AS precision,
		    CONVERT(INT, t.scale) AS scale
            FROM    sys.parameters p
                    LEFT JOIN sys.types t ON p.system_type_id = t.user_type_id
                    LEFT JOIN sys.types t2 ON p.user_type_id = t2.user_type_id
            WHERE   p.object_id = @ObjectId
            ORDER BY p.parameter_id;";

                var parameters = new List<ParameterMetadata<SqlDbType>>();

                using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
                {
                    con.Open();

                    using (var cmd = new SqlCommand(ParameterSql, con))
                    {
                        cmd.Parameters.AddWithValue("@ObjectId", objectId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader.GetString(reader.GetOrdinal("ParameterName"));
                                var typeName = reader.GetString(reader.GetOrdinal("TypeName"));
                                parameters.Add(new ParameterMetadata<SqlDbType>(name, name, typeName, TypeNameToSqlDbType(typeName)));
                            }
                        }
                    }
                }
                return parameters;
            }
            catch (Exception ex)
            {
                throw new MetadataException($"Error getting parameters for {procedureName}", ex);
            }
        }

        internal override StoredProcedureMetadata<SqlServerObjectName, SqlDbType> GetStoredProcedureInternal(SqlServerObjectName procedureName)
        {
            const string StoredProcedureSql =
                @"SELECT 
				s.name AS SchemaName,
				sp.name AS Name,
				sp.object_id AS ObjectId
				FROM SYS.procedures sp
				INNER JOIN sys.schemas s ON sp.schema_id = s.schema_id
				WHERE s.name = @Schema AND sp.Name = @Name";


            string actualSchema;
            string actualName;
            int objectId;

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(StoredProcedureSql, con))
                {
                    cmd.Parameters.AddWithValue("@Schema", procedureName.Schema ?? DefaultSchema);
                    cmd.Parameters.AddWithValue("@Name", procedureName.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new MissingObjectException($"Could not find stored procedure {procedureName}");

                        actualSchema = reader.GetString(reader.GetOrdinal("SchemaName"));
                        actualName = reader.GetString(reader.GetOrdinal("Name"));
                        objectId = reader.GetInt32(reader.GetOrdinal("ObjectId"));
                    }
                }
            }
            var objectName = new SqlServerObjectName(actualSchema, actualName);
            var parameters = GetParameters(objectName.ToString(), objectId);

            return new StoredProcedureMetadata<SqlServerObjectName, SqlDbType>(objectName, parameters);
        }
        internal override TableOrViewMetadata<SqlServerObjectName, SqlDbType> GetTableOrViewInternal(SqlServerObjectName tableName)
        {
            const string TableSql =
                @"SELECT 
				s.name AS SchemaName,
				t.name AS Name,
				t.object_id AS ObjectId,
				CONVERT(BIT, 1) AS IsTable 
				FROM SYS.tables t
				INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
				WHERE s.name = @Schema AND t.Name = @Name

				UNION ALL

				SELECT 
				s.name AS SchemaName,
				t.name AS Name,
				t.object_id AS ObjectId,
				CONVERT(BIT, 0) AS IsTable 
				FROM SYS.views t
				INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
				WHERE s.name = @Schema AND t.Name = @Name";


            string actualSchema;
            string actualName;
            int objectId;
            bool isTable;

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(TableSql, con))
                {
                    cmd.Parameters.AddWithValue("@Schema", tableName.Schema ?? DefaultSchema);
                    cmd.Parameters.AddWithValue("@Name", tableName.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new MissingObjectException($"Could not find table or view {tableName}");
                        actualSchema = reader.GetString(reader.GetOrdinal("SchemaName"));
                        actualName = reader.GetString(reader.GetOrdinal("Name"));
                        objectId = reader.GetInt32(reader.GetOrdinal("ObjectId"));
                        isTable = reader.GetBoolean(reader.GetOrdinal("IsTable"));
                    }
                }
            }


            var columns = GetColumns(objectId);

            return new TableOrViewMetadata<SqlServerObjectName, SqlDbType>(new SqlServerObjectName(actualSchema, actualName), isTable, columns);
        }




        internal override UserDefinedTypeMetadata<SqlServerObjectName, SqlDbType> GetUserDefinedTypeInternal(SqlServerObjectName typeName)
        {
            const string sql =
                @"SELECT	s.name AS SchemaName,
		t.name AS Name,
		tt.type_table_object_id AS ObjectId,
		t.is_table_type AS IsTableType,
		t2.name AS BaseTypeName,
		t.is_nullable,
		CONVERT(INT, t.max_length) AS max_length, 
		CONVERT(INT, t.precision) AS precision,
		CONVERT(INT, t.scale) AS scale
FROM	sys.types t
		INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
		LEFT JOIN sys.table_types tt ON tt.user_type_id = t.user_type_id
		LEFT JOIN sys.types t2 ON t.system_type_id = t2.user_type_id
WHERE	s.name = @Schema AND t.name = @Name;";

            string actualSchema;
            string actualName;
            string baseTypeName = null;
            int? objectId = null;
            bool isTableType;
            bool isNullable;
            int? maxLength;
            int? precision;
            int? scale;
            string fullTypeName;

            using (var con = new SqlConnection(m_ConnectionBuilder.ConnectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Schema", typeName.Schema ?? DefaultSchema);
                    cmd.Parameters.AddWithValue("@Name", typeName.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new MissingObjectException($"Could not find user defined type {typeName}");

                        actualSchema = reader.GetString(reader.GetOrdinal("SchemaName"));
                        actualName = reader.GetString(reader.GetOrdinal("Name"));
                        if (!reader.IsDBNull(reader.GetOrdinal("ObjectId")))
                            objectId = reader.GetInt32(reader.GetOrdinal("ObjectId"));
                        isTableType = reader.GetBoolean(reader.GetOrdinal("IsTableType"));
                        if (!reader.IsDBNull(reader.GetOrdinal("BaseTypeName")))
                            baseTypeName = reader.GetString(reader.GetOrdinal("BaseTypeName"));

                        isNullable = reader.GetBoolean(reader.GetOrdinal("is_nullable"));
                        maxLength = reader.GetInt32(reader.GetOrdinal("max_length"));
                        precision = reader.GetInt32(reader.GetOrdinal("precision"));
                        scale = reader.GetInt32(reader.GetOrdinal("scale"));

                        AdjustTypeDetails(baseTypeName, ref maxLength, ref precision, ref scale, out fullTypeName);

                    }
                }
            }

            List<ColumnMetadata<SqlDbType>> columns;

            if (isTableType)
                columns = GetColumns(objectId.Value);
            else
            {
                columns = new List<ColumnMetadata<SqlDbType>>();
                columns.Add(new ColumnMetadata<SqlDbType>(null, false, false, false, baseTypeName, TypeNameToSqlDbType(baseTypeName), null, isNullable, maxLength, precision, scale, fullTypeName));
            }

            return new UserDefinedTypeMetadata<SqlServerObjectName, SqlDbType>(new SqlServerObjectName(actualSchema, actualName), isTableType, columns);
        }



    }

}


