using CommandLine;
using NLog;
using System;
using System.Diagnostics;
using System.Net;

namespace HttpSequencer
{
    public class Program
    {        
        static Program()
        {
            try
            { 
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static int Main(string[] args)
        {
            try
            {
                var parsedOptions = Parser.Default.ParseArguments<Options, int>(args);
                var options = parsedOptions.MapResult((o) => o as Options, (o) => null as Options);
                return new HttpSequencer().RunSequenceAsync(options).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 3;
        }
    }

    public interface IProgressLog
    {
        void Started(SequenceItem successWith);

        void Progress(string message);

        void Trace(string message);

        void Success(SequenceItem successWith);

        void Fail(SequenceItem failWith);
    }

    public class ConsoleProgressLog : IProgressLog
    {
        public void Started(SequenceItem message)
        {
            Console.WriteLine(message);
        }

        public void Progress(string message)
        {
            Console.WriteLine(message);
        }

        public void Trace(string message)
        {
            Debug.WriteLine(message);
        }

        public void Success(SequenceItem successWith)
        {
            Console.WriteLine("Success:" + successWith.command);
        }

        public void Fail(SequenceItem failWith)
        {
            Console.Error.WriteLine("Fail:" + failWith.command);
        }
    }

    public class DebugProgressLog : IProgressLog
    {
        public void Started(SequenceItem message)
        {
            Debug.WriteLine(message);
        }

        public void Progress(string message)
        {
            Debug.WriteLine(message);
        }

        public void Trace(string message)
        {
            Debug.WriteLine("Debug: " +message);
        }

        public void Success(SequenceItem successWith)
        {
            Debug.WriteLine("Success:" + successWith.command);
        }

        public void Fail(SequenceItem failWith)
        {
            Debug.WriteLine("Fail:" + failWith.command);
        }
    }


    public class NLogProgressLog : IProgressLog
    {
        public NLog.Logger Logger { get; }

        public NLogProgressLog()
        {
            Logger = LogManager.GetLogger(nameof(Program));
        }

        public void Started(SequenceItem message)
        {
            Logger.Info(message);
        }

        public void Fail(SequenceItem failWith)
        {
            Logger.Error("Success:" + failWith.command);
        }

        public void Progress(string message)
        {
            Logger.Info(message);
        }

        public void Success(SequenceItem successWith)
        {
            Logger.Info("Success:" + successWith.command);
        }

        public void Trace(string message)
        {
            Logger.Debug(message);
        }
    }
}
