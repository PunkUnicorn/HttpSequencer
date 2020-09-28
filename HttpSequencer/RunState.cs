using NLog;
using System;
using System.Collections.Generic;

namespace HttpSequencer
{
    public class RunState
    {
        public List<(string/*hint*/, Exception)> Exceptions = new List<(string, Exception)>();

        public ILogger Log { get; set; }

        public string RunId { get; set; }
        public Options CommandLineOptions { get; set; }
        public YamlOptions YamlOptions { get; set; }
        public IProgressLog ProgressLog { get; internal set; }
        //public bool IsFail { get; internal set; }
    }
}
