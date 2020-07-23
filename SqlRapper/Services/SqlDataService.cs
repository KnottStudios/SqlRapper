using SqlRapper.CustomAttributes;
using SqlRapper.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace SqlRapper.Services
{
    public class SqlDataService : ISqlDataService
    {
        #region Fields And Properties
        /// <summary>
        /// For that rare occasion when your DB connection is screwy.
        /// </summary>
        private IFileLogger _logger;
        public int CmdTimeOut { get; set; } = 30;
        public string ConnectionString { get; set; }
        #endregion
        #region Constructors
        /// <summary>
        /// When SqlDataService fails, it needs to report its failure, however, a sql logger may recall the sql data service to write the error, possibly causing another error, causing a loop.
        /// So, we write to a file.  
        /// </summary>
        /// <param name="logger"></param>
        public SqlDataService() : this(ConfigurationManager.AppSettings["Sql_Con_String"], new FileLogger())
        {
        }

        public SqlDataService(string connectionString) : this(connectionString, new FileLogger())
        {
        }

        public SqlDataService(IFileLogger logger) : this(ConfigurationManager.AppSettings["Sql_Con_String"], logger)
        {
        }

        public SqlDataService(string connectionString, IFileLogger logger)
        {
            ConnectionString = connectionString;
            _logger = logger;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// WARNING: THE SQL string here can open this statement to SQL Injection, use this only with known inside information.
        /// USE $@"SprocName" and CommandType.StoredProcedure to use a stored procedure.
        /// USE "Parameterized SQL" 
        /// EXAMPLE: SELECT * FROM Table WHERE param = @paramName, CommandType.Text, and add SqlParameter("@paramName", myValue) to guard against sql injection.
        /// A simple wrapper to get data back in the form of a string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="commandType"></param>
        /// <param name="sqlParameterCollection"></param>
        /// <returns>a specified object T</returns>
        public string GetDataJson(string SQL, CommandType commandType, List<SqlParameter> sqlParameterCollection = null) 
        {
            var results = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(SQL))
            {
                throw new Exception("SQL statement was null or empty");
            }
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(SQL, con))
                {
                    cmd.CommandType = commandType;
                    cmd.Parameters.Clear();
                    cmd.CommandTimeout = CmdTimeOut;
                    try
                    {
                        if (sqlParameterCollection.NullSafeAny())
                        {
                            cmd.Parameters.AddRange(sqlParameterCollection.ToArray());
                        }

                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        var columns = reader.GetColumnNames(value => value.ToString());
                        while (reader.Read())
                        {
                            results.Add(SerializeRow(columns, reader));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        _logger.Log("Failed to Read Sql.", ex);
                        throw ex;
                    }
                }
            }
            return results.ConvertToJson();
        }

        /// <summary>
        /// WARNING:  The where clause opens this statement to sql injection, only use whereClause with internally created strings.
        /// Gets data from a table matching T + s or a specified table name.  The properties map to the table columns.  
        /// Custom sql can be put into the call through the whereClause, this allows for most customization.
        /// Ways to use: 
        /// 1.  GetData<tableNameSingular>() automatically selects all records in that table.
        /// 2.  GetData<anyclass>(tableName: sqlTableName) populates a table to a specified class.
        /// 3.  GetData<tableNameSingular>("Where x = 1") select * data from the tableNameSingulars table where X = 1.  
        /// Pretty much, this can be a shortcut to only write a where clause or Select * from a table.  You should use 
        /// GetData<T>(sql, commandType.Text, sqlParameterCollection) for more difficult or dangerous queries.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereClause"></param>
        /// <param name="tableName">Just specifying a table name here will </param>
        /// <returns></returns>
        public List<T> GetData<T>(string whereClause = null, string tableName = null)
        {
            var row = Activator.CreateInstance<T>();
            var objProps = row.GetType().GetProperties();
            var returnList = new List<T>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = CmdTimeOut;
                    cmd.Connection = con;
                    StringBuilder sb1 = new StringBuilder();
                    string tn = tableName ?? row.GetType().Name + "s";

                    sb1.Append($@"SELECT * FROM {tn} {whereClause}");
                    cmd.CommandText = sb1.ToString();
                    try
                    {
                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        var columns = reader.GetColumnNames(value => value.ToString().ToLower());
                        while (reader.Read())
                        {
                            var thisRow = Activator.CreateInstance<T>();
                            foreach (var prop in objProps)
                            {
                                if (columns.Contains(prop.Name.ToLower()))
                                {
                                    var val = reader[prop.Name];
                                    if (val != DBNull.Value)
                                    {
                                        prop.SetValue(thisRow, val);
                                    }
                                    else
                                    {
                                        prop.SetValue(thisRow, null);
                                    }
                                }
                            }
                            returnList.Add(thisRow);
                        }
                    }
                    catch (Exception ex) 
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        _logger.Log("Failed to Read Sql.", ex);
                        throw ex; 
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        /// WARNING: THE SQL string here can open this statement to SQL Injection, use this only with known inside information.
        /// USE: $@"SprocName" and CommandType.StoredProcedure to use a stored procedure.
        /// USE "Parameterized SQL" 
        /// EXAMPLE: SELECT * FROM Table WHERE param = @paramName, CommandType.Text, and add SqlParameter("@paramName", myValue) to guard against sql injection.
        /// A simple wrapper to get data back in the form of an object.  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="commandType"></param>
        /// <param name="sqlParameterCollection"></param>
        /// <returns>a specified object T</returns>
        public List<T> GetData<T>(string SQL, CommandType commandType, List<SqlParameter> sqlParameterCollection = null)
        {
            var objProps = Activator.CreateInstance<T>().GetType().GetProperties();
            var returnList = new List<T>();
            if (string.IsNullOrEmpty(SQL)) {
                throw new Exception("SQL statement was null or empty");
            }
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(SQL, con))
                {
                    cmd.CommandType = commandType;
                    cmd.Parameters.Clear();
                    cmd.CommandTimeout = CmdTimeOut;
                    try
                    {
                        if (sqlParameterCollection.NullSafeAny())
                        {
                            cmd.Parameters.AddRange(sqlParameterCollection.ToArray());
                        }

                        con.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        var columns = reader.GetColumnNames(value => value.ToString().ToLower());
                        while (reader.Read())
                        {
                            var thisRow = Activator.CreateInstance<T>();
                            foreach (var prop in objProps)
                            {
                                if (columns.Contains(prop.Name.ToLower()))
                                {
                                    var val = reader[prop.Name];
                                    if (val != DBNull.Value)
                                    {
                                        prop.SetValue(thisRow, val);
                                    }
                                    else
                                    {
                                        prop.SetValue(thisRow, null);
                                    }
                                }
                            }
                            returnList.Add(thisRow);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        _logger.Log("Failed to Read Sql.", ex);
                        throw ex;
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        /// Works with Simple Sql objects that mock tables.  
        /// Protected from SQL Injection using parameterized sql.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="tableName"></param>
        /// <returns>bool success</returns>
        public bool InsertData<T>(T row, string tableName = null)
        {
            string tn = tableName ?? row.GetType().Name + "s";
            int inserted = 0;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    StringBuilder sb = GetInsertableRows(row, tn, cmd);
                    cmd.CommandText = sb.ToString();
                    cmd.Connection = con;
                    cmd.CommandTimeout = CmdTimeOut;
                    try
                    {
                        con.Open();
                        inserted = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        //well logging to sql might not work... we could try... but could cause infinite loop if it fails.
                        //So Lets write to a local file.
                        _logger.Log($"Failed to Write to Sql. {row.ToJson()}", ex);
                        throw ex;
                    }
                }
            }
            if (inserted > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// SqlBulkCopy is allegedly protected from Sql Injection.
        /// Inserts a list of simple sql objects that mock tables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows">A list of rows to insert</param>
        /// <param name="tableName"></param>
        /// <returns>bool success</returns>
        public bool BulkInsertData<T>(List<T> rows, string tableName = null)
        {
            var template = Activator.CreateInstance<T>();
            string tn = tableName ?? template.GetType().Name + "s";
            int inserted = 0;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlBulkCopy sbc = new SqlBulkCopy(ConnectionString))
                {
                    var dt = new DataTable();
                    var columns = GetColumns(template);
                    int rowNum = 0;
                    foreach (var row in rows)
                    {
                        dt.Rows.Add();
                        int colNum = 0;
                        foreach (var col in columns)
                        {
                            if (rowNum == 0)
                            {
                                sbc.ColumnMappings.Add(col.Name, col.Name);
                                dt.Columns.Add(new DataColumn(col.Name));
                            }
                            var attributes = GetAttributes(row, col);
                            bool skip = IsPrimaryKey(attributes);
                            var value = row.GetType().GetProperty(col.Name).GetValue(row);
                            skip = skip ? skip : IsNullDefaultKey(attributes, value);
                            if (skip)
                            {
                                dt.Rows[rowNum][colNum] = DBNull.Value;
                                colNum++;
                                continue;
                            }
                            dt.Rows[rowNum][colNum] = value ?? DBNull.Value;
                            colNum++;
                        }
                        rowNum++;
                    }
                    try
                    {
                        con.Open();
                        sbc.DestinationTableName = tn;
                        sbc.WriteToServer(dt);
                        inserted = 1;
                    }
                    catch (Exception ex)
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        //well logging to sql might not work... we could try... but could cause infinite loop if it fails.
                        //So Lets write to a local file.
                        _logger.Log($"Failed to Bulk Copy to Sql:  {rows.ToCSV()}", ex);
                        throw ex;
                    }
                }
            }
            if (inserted > 0)
            {
                return true;
            }
            return false;
        }

        public bool UpdateData<T>(T row, string whereClause = null, string tableName = null)
        {
            string tn = tableName ?? row.GetType().Name + "s";
            int inserted = 0;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        StringBuilder sb = GetUpdateableRows(row, tn, cmd, whereClause);
                        cmd.CommandText = sb.ToString();
                        cmd.Connection = con;
                        cmd.CommandTimeout = CmdTimeOut;
                        con.Open();
                        inserted = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        if (con.State != ConnectionState.Closed)
                        {
                            con.Close();
                        }
                        //well logging to sql might not work... we could try... but could cause infinite loop if it fails.
                        //So Lets write to a local file.
                        _logger.Log("Failed to Write to Sql.", ex);
                        throw ex;
                    }
                }
            }
            if (inserted > 0)
            {
                return true;
            }
            return false;
        }

        #endregion
        #region Private methods
        private static StringBuilder GetUpdateableRows<T>(T row, string table, SqlCommand cmd, string whereClause = null)
        {
            StringBuilder sb1 = new StringBuilder();
            sb1.Append($"Update {table} Set ");
            string primaryKey = "";
            object primaryValue = "";
            var columns = GetColumns(row);
            foreach (var col in columns)
            {
                var attributes = GetAttributes(row, col);
                var value = row.GetType().GetProperty(col.Name).GetValue(row);
                if (IsPrimaryKey(attributes))
                {
                    primaryKey = col.Name;
                    primaryValue = value;
                    continue;
                }
                if (value != null)
                {
                    sb1.Append($"{col.Name} = @{col.Name},");
                    cmd.Parameters.AddWithValue($"@{col.Name}", value);
                }
            }
            sb1.Length--;
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sb1.Append($" {whereClause}");
            }
            else if (!string.IsNullOrWhiteSpace(primaryKey) && primaryValue != null)
            {
                sb1.Append($" WHERE {primaryKey} = @{primaryKey}");
                cmd.Parameters.AddWithValue($"@{primaryKey}", primaryValue);
            }
            else
            {
                throw new Exception("A where clause was not able to be derived from the class due to no primary key and a where clause was not provided.");
            }
            return sb1;
        }
        private static StringBuilder GetInsertableRows<T>(T row, string table, SqlCommand cmd)
        {
            StringBuilder sb1 = new StringBuilder();
            sb1.Append($"INSERT INTO {table} (");
            StringBuilder sb2 = new StringBuilder();
            sb2.Append($" VALUES (");
            var columns = GetColumns(row);
            foreach (var col in columns)
            {
                var attributes = GetAttributes(row, col);
                bool skip = IsPrimaryKey(attributes);
                var value = row.GetType().GetProperty(col.Name).GetValue(row);
                skip = skip ? skip : IsNullDefaultKey(attributes, value);
                if (skip)
                {
                    continue;
                }
                sb1.Append($"{col.Name},");
                value = value ?? DBNull.Value;
                cmd.Parameters.AddWithValue($"@{col.Name}", value);
                sb2.Append($"@{col.Name},");
            }
            sb1.Length--;
            sb2.Length--;
            sb1.Append(")");
            sb2.Append(")");
            sb1.Append(sb2);
            return sb1;
        }

        private static object[] GetAttributes<T>(T row, PropertyInfo col)
        {
            return row.GetType().GetProperty(col.Name).GetCustomAttributes(false);
        }

        private static PropertyInfo[] GetColumns<T>(T row)
        {
            return row.GetType().GetProperties();
        }

        private static bool IsPrimaryKey(object[] attributes)
        {
            bool skip = false;
            foreach (var attr in attributes)
            {
                if (attr.GetType() == typeof(PrimaryKeyAttribute))
                {
                    skip = true;
                }
            }

            return skip;
        }
        private static bool IsNullDefaultKey(object[] attributes, object value)
        {
            bool skip = false;
            foreach (var attr in attributes)
            {
                if (attr.GetType() == typeof(DefaultKeyAttribute) && value == null)
                {
                    skip = true;
                }
            }

            return skip;
        }

        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
        {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
                result.Add(col, reader[col]);
            return result;
        }
        #endregion
    }
}