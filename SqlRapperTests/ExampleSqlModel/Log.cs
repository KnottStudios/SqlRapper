using SqlRapper.CustomAttributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace SqlRapperTests.ExampleSqlModel
{
    public class Log
    {
        public int ApplicationId { get; set; }
        [PrimaryKey]
        public int? LogId { get; set; }
        [DefaultKey]
        public DateTime? Date { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionAsJson { get; set; }
    }
}
