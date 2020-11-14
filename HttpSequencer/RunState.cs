using HttpSequencer.SequenceItemActions;
using NLog;
using System;
using System.Collections.Generic;

namespace HttpSequencer
{
    public class RunState
    {
        public RunState()
        {
            //FailedSequenceActions = new List<ISequenceItemAction>();
        }

        public ILogger Log { get; set; }

        public string RunId { get; set; }
        public Options CommandLineOptions { get; set; }
        public YamlOptions YamlOptions { get; set; }
        public IProgressLog ProgressLog { get; set; }

        public ISequenceItemAction Top { get; set; }
    }
}
