using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SqlRapper.Services
{
    /// <summary>
    /// Wrote Implementation data in comments on interface so that the comments are visible to users.
    /// </summary>
    public interface ISqlDataService
    {
        string ConnectionString { get; set; }
        
        int CmdTimeOut { get; set; }

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
        string GetDataJson(string SQL, CommandType commandType, List<SqlParameter> sqlParameterCollection = null);

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
        List<T> GetData<T>(string SQL, CommandType commandType, List<SqlParameter> sqlParameterCollection = null);

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
        List<T> GetData<T>(string whereClause = null, string tableName = null);

        /// <summary>
        /// Works with Simple Sql objects that mock tables.  
        /// Protected from SQL Injection using parameterized sql.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="tableName"></param>
        /// <returns>bool success</returns>
        bool InsertData<T>(T row, string tableName = null);

        /// <summary>
        /// SqlBulkCopy is allegedly protected from Sql Injection.
        /// Inserts a list of simple sql objects that mock tables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rows">A list of rows to insert</param>
        /// <param name="tableName"></param>
        /// <returns>bool success</returns>
        bool BulkInsertData<T>(List<T> rows, string tableName = null);
    }
}
