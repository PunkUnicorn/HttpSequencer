using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

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
                var parsedOptions = Parser.Default.ParseArguments<Options>(args);
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
        void Started(string runid, SequenceItem successWith);

        void Progress(string message);

        void Trace(string message);

        void Success(SequenceItem successWith);

        void Fail(Stack<KeyValuePair<string, SequenceItemActions.ISequenceItemAction>> breadcrumbs, SequenceItem failWith);
    }

    public class ConsoleProgressLog : IProgressLog
    {
        public void Started(string runid, SequenceItem message)
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

        public void Fail(System.Collections.Generic.Stack<System.Collections.Generic.KeyValuePair<string, SequenceItemActions.ISequenceItemAction>> breadcrumbs, SequenceItem failWith)
        {
            Console.Error.WriteLine("Fail:" + failWith.command);
        }
    }

    public class DebugProgressLog : IProgressLog
    {
        public void Started(string runid, SequenceItem message)
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

        public void Fail(System.Collections.Generic.Stack<System.Collections.Generic.KeyValuePair<string, SequenceItemActions.ISequenceItemAction>> breadcrumbs, SequenceItem failWith)
        {
            Debug.WriteLine("Fail:" + failWith.command);
        }
    }


    public class NLogProgressLog : IProgressLog
    {
        private const int padWidth = 50;
        private StringBuilder ProgressLog { get; set; } = new StringBuilder();
        public NLog.Logger Logger { get; }  = LogManager.GetLogger(nameof(Program));
        private string runid;

        public void Started(string runid, SequenceItem sequenceItem)
        {
            this.runid = runid;
            Logger.Info($"{runid,padWidth} - {nameof(Started)}:{sequenceItem.command}");
        }

        public void Fail(Stack<KeyValuePair<string, SequenceItemActions.ISequenceItemAction>> breadcrumbs, SequenceItem failWith)
        {
            //if (breadcrumbs.Any())
            //    ProgressLog.Append($"{nameof(breadcrumbs)}>-->{string.Join(">-->", string.Join(">-->", breadcrumbs.Select(s => s.Value.GetSequenceItem().command)))}");

            if (ProgressLog.Length > 0)
                Logger.Info($"{runid,padWidth} - {ProgressLog}... Failed at {failWith.command}. ");

            ProgressLog.Clear();
            Logger.Error($"{runid,padWidth} - Fail:{failWith.command}");
        }

        public void Progress(string message)
        {
            ProgressLog.Append(message);
            //Logger.Info(message);
        }

        public void Success(SequenceItem successWith)
        {
            //if (ProgressLog.Length > 0)
            //    Logger.Info($"{runid,padWidth} - {ProgressLog}... Success!");

            ProgressLog.Clear();
            Logger.Info($"{runid,padWidth} - Success:{successWith.command}");
        }

        public void Trace(string message)
        {
            Logger.Debug(message);
            Logger.Trace($"{runid,padWidth} - Trace:{message}");
        }
    }
}
