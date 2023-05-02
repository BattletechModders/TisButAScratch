using System;
using System.IO;

namespace TisButAScratch.Framework
{
    internal class Logger
    {
        private static StreamWriter logStreamWriter;
        private bool enableLogging;

        public Logger(string modDir, string fileName, bool enableLogging)
        {
            string filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStreamWriter = File.AppendText(filePath);
            logStreamWriter.AutoFlush = true;

            this.enableLogging = enableLogging;
        }

        public void LogMessage(string message)
        {
            if (ModInit.modSettings.enableLogging)
            {
                string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"INFO: {ts} - {message}");
            }
        }

        public void LogTrace(string message)
        {
            if (ModInit.modSettings.enableTrace)
            {
                string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"INFO: {ts} - {message}");
            }
        }


        public static void LogError(string message)
        {
            string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"ERROR: {ts} - {message}");
        }

        public static void LogException(Exception exception)
        {
            string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"CRITICAL: {ts} - {exception}");
        }
    }
}