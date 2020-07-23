using SqlRapper.CustomAttributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace SqlRapperTests.ExampleSqlModel
{
    public class Log
    {
        [PrimaryKey]
        public int? LogId { get; set; }
        public int ApplicationId { get; set; }
        [DefaultKey]
        public DateTime? Date { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionAsJson { get; set; }
    }
}
