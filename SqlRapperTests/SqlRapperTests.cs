﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlRapper.Services;
using SqlRapperTests.ExampleSqlModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;

namespace SqlRapperTests
{
    [TestClass]
    public class SqlDataServiceTests
    {
        [TestMethod]
        public void CanWriteToSqlDb()
        {
            var success = true;
            //this really writes to the db.  So it is disabled.  
            /* */
            var conString = ConfigurationManager.AppSettings["Sql_Con_String"];
            SqlDataService db = new SqlDataService(ConfigurationManager.AppSettings["Sql_Con_String"], new FileLogger());
            var log = new Log() {
                Message = "Test",
                ApplicationId = int.Parse(ConfigurationManager.AppSettings["ApplicationId"])
            };
            Assert.AreEqual(log.ApplicationId, 2);
            success = db.InsertData(log);
            
            Assert.IsTrue(success);
        }
        [TestMethod]
        public void CanWriteToSqlDbQuickly()
        {
            var success = true;
            //this really writes to the db.  So it is disabled.  
            /*             */
            Stopwatch sw = new Stopwatch();

            SqlDataService db = new SqlDataService(ConfigurationManager.AppSettings["Sql_Con_String"], new FileLogger());
            var log = new Log() {
                Message = "Test",
                ApplicationId = int.Parse(ConfigurationManager.AppSettings["ApplicationId"])
            };
            Assert.AreEqual(log.ApplicationId, 2);
            sw.Start();
            success = db.InsertData(log);
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds <= 1000);
            

            Assert.IsTrue(success);
        }
        [TestMethod]
        public void CanInsertMultipleRowsToDbQuickly()
        {
            var success = true;
            //this really writes to the db.  So it is disabled.  
            /*    */
            Stopwatch sw = new Stopwatch();

            SqlDataService db = new SqlDataService(ConfigurationManager.AppSettings["Sql_Con_String"], new FileLogger());
            var log1 = new Log() {
                Message = "Test1",
                ApplicationId = int.Parse(ConfigurationManager.AppSettings["ApplicationId"])
            };

            var log2 = new Log()
            {
                Message = "Test2",
                ApplicationId = int.Parse(ConfigurationManager.AppSettings["ApplicationId"])
            };
            var logs = new List<Log>() { log1, log2 };
            Assert.AreEqual(log1.ApplicationId, 2);
            Assert.AreEqual(log2.ApplicationId, 2);
            sw.Start();
            success = db.BulkInsertData(logs);
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds <= 3000);
            

            Assert.IsTrue(success);
        }


        [TestMethod]
        public void CanGetDbInfoQuickly()
        {
            var success = true;
            //this really reads from the db.  It is disabled.  

            Stopwatch sw = new Stopwatch();

            SqlDataService db = new SqlDataService(ConfigurationManager.AppSettings["Sql_Con_String"], new FileLogger());
            /**/
            sw.Start();
            var logs = db.GetData<Log>();
            sw.Stop();
            
            Assert.IsTrue(sw.ElapsedMilliseconds <= 2000);
                       
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            var condensedLogs = db.GetData<Log>("WHERE ApplicationId = 2", "Logs");
            sw2.Stop();

            Assert.IsTrue(sw2.ElapsedMilliseconds <= 2000);
             
            Assert.IsTrue(success);
        }
        [TestMethod]
        public void AbleToGetColumnsFromObject()
        {
            SqlDataService dbService = new SqlDataService("fake", null);
            MethodInfo getColumns = dbService.GetType().GetMethod("GetColumns", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getColumns == null)
            {
                Assert.Fail("Could not find method");
            }
            getColumns = getColumns.MakeGenericMethod(typeof(Log));

            PropertyInfo[] columns = (PropertyInfo[])getColumns.Invoke(typeof(Log), new object[] { new Log() });

            Assert.IsTrue(columns.Length == 4);
        }
        [TestMethod]
        public void CanGetCustomAttributes()
        {
            SqlDataService dbService = new SqlDataService("fake", null);

            var row = new Log();
            var columns = (PropertyInfo[])PrivateMethod.InvokePrivateMethodWithReturnType(dbService, "GetColumns", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(Log) }, new object[] { row });

            PrivateType dbTester = new PrivateType(typeof(SqlDataService));

            int customCount = 0;

            foreach (var col in columns)
            {
                var attributes = (object[])PrivateMethod.InvokePrivateMethodWithReturnType(dbService, "GetAttributes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(Log) }, new object[] { row, col });

                if ((bool)dbTester.InvokeStatic("SkipPrimaryKeys", new object[] { attributes }))
                {
                    customCount++;
                }
                var value = row.GetType().GetProperty(col.Name).GetValue(row);
                if ((bool)dbTester.InvokeStatic("SkipNullDefaultKeys", new object[] { attributes, value }))
                {
                    customCount++;
                }
            }
            Assert.AreEqual(customCount, 2);

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void CanCreateSqlStatementIgnoringCustomKeys()
        {
            var row = new Log();
            string returnedSql = "";
            using (SqlCommand cmd = new SqlCommand())
            {
                var tableName = row.GetType().Name + "s";
                Type[] types = new Type[] { typeof(Log) };
                var parameters = new object[] { row, tableName, cmd };
                returnedSql = PrivateMethod.InvokePrivateMethodWithReturnType(new SqlDataService("fake", null), "GetInsertableRows", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance, types, parameters).ToString();
            }
            string expectedSql = "INSERT INTO Logs (Message,ApplicationId) VALUES (@Message,@ApplicationId)";

            Assert.AreEqual(returnedSql, expectedSql);
        }

    }
}