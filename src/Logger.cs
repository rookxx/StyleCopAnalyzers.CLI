namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Diagnostics;

    public enum LogLevel
    {
        Silent,
        Infomation,
        Verbose,
    }

    public interface ILogger
    {
        void SetLogLevel(LogLevel level);
        void LogInformation(object message);
        void LogDebug(object message);
        void LogVerbose(object message);
    }

    public class SilentLogger : ILogger
    {
        void ILogger.LogDebug(object message) { }
        void ILogger.LogInformation(object message) { }
        void ILogger.LogVerbose(object message) { }
        void ILogger.SetLogLevel(LogLevel level) { }
    }

    public class SimpleConsoleLogger : ILogger
    {
        private LogLevel logLevel;

        public SimpleConsoleLogger()
        {
            logLevel = LogLevel.Infomation;
        }

        void ILogger.SetLogLevel(LogLevel level)
        {
            logLevel = level;
        }

        void ILogger.LogInformation(object message)
        {
            if (logLevel < LogLevel.Infomation) { return; }
            Console.WriteLine(message);
        }

        void ILogger.LogDebug(object message)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }

        void ILogger.LogVerbose(object message)
        {
            if (logLevel < LogLevel.Verbose) { return; }
            Console.WriteLine(message);
        }
    }
}