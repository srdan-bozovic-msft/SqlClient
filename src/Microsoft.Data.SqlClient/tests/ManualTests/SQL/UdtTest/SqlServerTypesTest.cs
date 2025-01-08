// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;
using Xunit;

namespace Microsoft.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlServerTypesTest
    {
        // Synapse: Parse error at line: 1, column: 48: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void GetSchemaTableTest()
        {
            string db = new SqlConnectionStringBuilder(DataTestUtility.TCPConnectionString).InitialCatalog;
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TCPConnectionString))
            using (SqlCommand cmd = new SqlCommand("select hierarchyid::Parse('/1/') as col0", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                {
                    DataTable schemaTable = reader.GetSchemaTable();
                    DataTestUtility.AssertEqualsWithDescription(1, schemaTable.Rows.Count, "Unexpected schema table row count.");

                    string columnName = (string)(string)schemaTable.Rows[0][schemaTable.Columns["ColumnName"]];
                    DataTestUtility.AssertEqualsWithDescription("col0", columnName, "Unexpected column name.");

                    string dataTypeName = (string)schemaTable.Rows[0][schemaTable.Columns["DataTypeName"]];
                    DataTestUtility.AssertEqualsWithDescription($"{db}.sys.hierarchyid".ToUpper(), dataTypeName.ToUpper(), "Unexpected data type name.");

                    string udtAssemblyName = (string)schemaTable.Rows[0][schemaTable.Columns["UdtAssemblyQualifiedName"]];
                    Assert.True(udtAssemblyName?.StartsWith("Microsoft.SqlServer.Types.SqlHierarchyId", StringComparison.Ordinal), "Unexpected UDT assembly name: " + udtAssemblyName);
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 48: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void GetValueTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TCPConnectionString))
            using (SqlCommand cmd = new SqlCommand("select hierarchyid::Parse('/1/') as col0", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    reader.GetValue(0);
                    reader.GetSqlValue(0);
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtZeroByte()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/') as col0";
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.False(reader.IsDBNull(0));
                    SqlBytes sqlBytes = reader.GetSqlBytes(0);
                    Assert.False(sqlBytes.IsNull, "Expected a zero length byte array");
                    Assert.True(sqlBytes.Length == 0, "Expected a zero length byte array");
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetSqlBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.SequentialAccess);
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetSqlBytes()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetSqlBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    SqlBytes sqlBytes = null;

                    sqlBytes = reader.GetSqlBytes(0);
                    Assert.Equal("5ade", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(1);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(2);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(sqlBytes.Value));

                    if (behavior == CommandBehavior.Default)
                    {
                        sqlBytes = reader.GetSqlBytes(0);
                        Assert.Equal("5ade", ToHexString(sqlBytes.Value));
                    }
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.SequentialAccess);
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetBytes()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    int byteCount = 0;
                    byte[] bytes = null;

                    byteCount = (int)reader.GetBytes(0, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                    Assert.Equal("5ade", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(1, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(1, 0, bytes, 0, bytes.Length);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(2, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(2, 0, bytes, 0, bytes.Length);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        byteCount = (int)reader.GetBytes(0, 0, null, 0, 0);
                        Assert.True(byteCount > 0);
                        bytes = new byte[byteCount];
                        reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetStreamSequentialAccess()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.SequentialAccess);
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSqlDataReaderGetStream()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetStream(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    MemoryStream buffer = null;
                    byte[] bytes = null;

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(0))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("5ade", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(1))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(2))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        buffer = new MemoryStream();
                        using (Stream stream = reader.GetStream(0))
                        {
                            stream.CopyTo(buffer);
                        }
                        bytes = buffer.ToArray();
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 41: Incorrect syntax near 'hierarchyid'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtSchemaMetadata()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    ReadOnlyCollection<System.Data.Common.DbColumn> columns = reader.GetColumnSchema();

                    System.Data.Common.DbColumn column = null;

                    // Validate Microsoft.SqlServer.Types.SqlHierarchyId, Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
                    column = columns[0];
                    Assert.Equal("col0", column.ColumnName);
                    Assert.True(column.DataTypeName.EndsWith(".hierarchyid", StringComparison.Ordinal), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlHierarchyId");

                    // Validate Microsoft.SqlServer.Types.SqlGeometry, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[1];
                    Assert.Equal("col1", column.ColumnName);
                    Assert.True(column.DataTypeName.EndsWith(".geometry", StringComparison.Ordinal), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeometry");

                    // Validate Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[2];
                    Assert.Equal("col2", column.ColumnName);
                    Assert.True(column.DataTypeName.EndsWith(".geography", StringComparison.Ordinal), $"Unexpected DataTypeName \"{column.DataTypeName}\"");
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeography");
                }
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'geometry'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtParameterSetSqlByteValue()
        {
            const string ExpectedPointValue = "POINT (1 1)";
            SqlBytes geometrySqlBytes = null;
            string actualtPointValue = null;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT geometry::Parse('{ExpectedPointValue}')";
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        geometrySqlBytes = reader.GetSqlBytes(0);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT @geometry.STAsText()";
                    var parameter = command.Parameters.AddWithValue("@geometry", geometrySqlBytes);
                    parameter.SqlDbType = SqlDbType.Udt;
                    parameter.UdtTypeName = "geometry";
                    actualtPointValue = System.Convert.ToString(command.ExecuteScalar());
                }

                Assert.Equal(ExpectedPointValue, actualtPointValue);
            }
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'geometry'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestUdtParameterSetRawByteValue()
        {
            const string ExpectedPointValue = "POINT (1 1)";
            byte[] geometryBytes = null;
            string actualtPointValue = null;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TCPConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT geometry::Parse('{ExpectedPointValue}')";
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        geometryBytes = reader.GetSqlBytes(0).Buffer;
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT @geometry.STAsText()";
                    var parameter = command.Parameters.AddWithValue("@geometry", geometryBytes);
                    parameter.SqlDbType = SqlDbType.Udt;
                    parameter.UdtTypeName = "geometry";
                    actualtPointValue = System.Convert.ToString(command.ExecuteScalar());
                }

                Assert.Equal(ExpectedPointValue, actualtPointValue);
            }
        }
        private static void AssertSqlUdtAssemblyQualifiedName(string assemblyQualifiedName, string expectedType)
        {
            List<string> parts = assemblyQualifiedName.Split(',').Select(x => x.Trim()).ToList();

            string type = parts[0];
            string assembly = parts.Count < 2 ? string.Empty : parts[1];
            string version = parts.Count < 3 ? string.Empty : parts[2];
            string culture = parts.Count < 4 ? string.Empty : parts[3];
            string token = parts.Count < 5 ? string.Empty : parts[4];

            Assert.Equal(expectedType, type);
            Assert.Equal("Microsoft.SqlServer.Types", assembly);
            Assert.StartsWith("Version", version);
            Assert.StartsWith("Culture", culture);
            Assert.StartsWith("PublicKeyToken", token);
        }

        private static string ToHexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        private static string GetUdtName(Type udtClrType)
        {
            if (typeof(SqlHierarchyId) == udtClrType)
            {
                return "hierarchyid";
            }
            if (typeof(SqlGeography) == udtClrType)
            {
                return "geography";
            }
            if (typeof(SqlGeometry) == udtClrType)
            {
                return "geometry";
            }

            throw new ArgumentException("Unknwon UDT CLR Type " + udtClrType.FullName);
        }

        // Synapse: Parse error at line: 1, column: 8: Incorrect syntax near 'geometry'.
        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), nameof(DataTestUtility.IsNotAzureSynapse))]
        public static void TestSqlServerTypesInsertAndRead()
        {
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("Type");
            string allTypesSQL = @$"
                    if not exists (select * from sysobjects where name='{tableName}' and xtype='U')
                    Begin
                    create table {tableName}
                    (
                        id int identity not null,
                        c1 hierarchyid not null,
                        c2 uniqueidentifier not null,
                        c3 geography not null,
                        c4 geometry not null,
                    );
                    End
                    ";

            Dictionary<string, object> rowValues = new();
            rowValues["c1"] = SqlHierarchyId.Parse(new SqlString("/1/1/3/"));
            rowValues["c2"] = Guid.NewGuid();
            rowValues["c3"] = SqlGeography.Point(1.1, 2.2, 4120);
            rowValues["c4"] = SqlGeometry.Point(5.2, 1.1, 4120);

            using SqlConnection conn = new(DataTestUtility.TCPConnectionString);
            conn.Open();
            try
            {
                using SqlCommand cmd1 = conn.CreateCommand();

                // Create db and table
                cmd1.CommandText = allTypesSQL.ToString();
                cmd1.ExecuteNonQuery();

                using SqlCommand cmd2 = conn.CreateCommand();

                StringBuilder columnsSql = new();
                StringBuilder valuesSql = new();

                foreach (KeyValuePair<string, object> pair in rowValues)
                {
                    string paramName = "@" + pair.Key;
                    object paramValue = pair.Value;
                    columnsSql.Append(pair.Key);
                    valuesSql.Append(paramName);

                    columnsSql.Append(",");
                    valuesSql.Append(",");

                    SqlParameter param = new(paramName, paramValue);
                    cmd2.Parameters.Add(param);

                    if (paramValue.GetType().Assembly == typeof(SqlHierarchyId).Assembly)
                    {
                        param.UdtTypeName = GetUdtName(paramValue.GetType());
                    }
                }

                columnsSql.Length--;
                valuesSql.Length--;

                string insertSql = string.Format(CultureInfo.InvariantCulture, $"insert {tableName}" + @" ({0}) values({1})",
                    columnsSql.ToString(), valuesSql.ToString());

                cmd2.CommandText = insertSql;
                cmd2.ExecuteNonQuery();

                cmd1.CommandText = @$"select * from dbo.{tableName}";
                using SqlDataReader r = cmd1.ExecuteReader();
                while (r.Read())
                {
                    Assert.Equal(rowValues["c1"].GetType(), r.GetValue(1).GetType());
                    Assert.Equal(rowValues["c2"].GetType(), r.GetValue(2).GetType());
                    Assert.Equal(rowValues["c3"].GetType(), r.GetValue(3).GetType());
                    Assert.Equal(rowValues["c4"].GetType(), r.GetValue(4).GetType());

                    Assert.Equal(rowValues["c1"].ToString(), r.GetValue(1).ToString());
                    Assert.Equal(rowValues["c2"].ToString(), r.GetValue(2).ToString());
                    Assert.Equal(rowValues["c3"].ToString(), r.GetValue(3).ToString());
                    Assert.Equal(rowValues["c4"].ToString(), r.GetValue(4).ToString());
                }
            }
            finally
            {
                DataTestUtility.DropTable(conn, tableName);
            }
        }
    }
}
