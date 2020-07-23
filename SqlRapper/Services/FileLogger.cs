using System;
using System.Configuration;
using System.IO;

namespace SqlRapper.Services
{
    public class FileLogger : IFileLogger
    {
        protected readonly object lockObj = new object();
        public string Title { get; set; } = "File Logger for MyBusiness.App";
        public string DirectoryPath { get; set; } = ConfigurationManager.AppSettings["File_Logger_Path"];
        public string FileName { get; set; } = ConfigurationManager.AppSettings["File_Logger_File"];

        /// <summary>
        /// Warning this is get only, set in constructor.
        /// </summary>
        public string Location { get { return DirectoryPath + FileName; } set { throw new NotImplementedException(); } }

        public FileLogger()
        {
        }

        public FileLogger(string directoryPath, string fileName)
        {
            DirectoryPath = directoryPath;
            FileName = fileName;
        }

        public bool Log(string message, Exception exception = null)
        {
            bool success = CreateLibrary(DirectoryPath);
            success = CreateFile();
            if (success)
            {
                success = WriteToFile(message, exception);
            }
            return success;
        }

        public bool CreateLibrary(string directoryPath) {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        private bool CreateFile() {
            try
            {
                if (!File.Exists(Location))
                {
                    using (StreamWriter sw = File.CreateText(Location))
                    {
                        sw.WriteLine(Title);
                        sw.WriteLine();
                    }
                }
            } catch {
                return false;
            }
            return true;

        }

        private bool WriteToFile(string message, Exception exception = null) {
            try
            {
                lock (lockObj)
                {

                    using (StreamWriter sw = File.AppendText(Location))
                    {
                        sw.WriteLine($@"{DateTime.Now} : {message}");
                        if (exception != null)
                        {
                            sw.WriteLine($@"EXCEPTION MESSAGE : {exception?.Message}");
                            sw.WriteLine($@"STACKTRACE : {exception?.StackTrace}");
                            sw.WriteLine($@"INNER EXCEPTION : {exception?.InnerException?.Message}");
                        }
                        sw.WriteLine(); //spacer
                        sw.Close();
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}