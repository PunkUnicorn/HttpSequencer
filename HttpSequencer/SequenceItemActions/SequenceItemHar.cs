using HarSharp;
using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static HttpSequencer.SequenceItemActions.SequenceItemStatic;

namespace HttpSequencer
{
    internal class SequenceItemHar : SequenceItemAbstract, ISequenceItemAction
    {
        public SequenceItemHar(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems)
            : base(state, sequenceItem, model, nextSequenceItems) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

        public async Task<object> ActionAsync(CancellationToken cancelToken)
        {
            return await FailableRun<Task<object>>(this, async delegate 
            {
                ++this.ActionExecuteCount;

                if (this.sequenceItem.har == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.har)} missing");

                var contents = await GetHarContentsAsync();

                var results = ProcessEntries(contents.Log);

                var options = new ProcessSequenceItem.Options 
                {
                    state = base.state, 
                    parent = this, 
                    model = model, 
                    sequenceItem = results.First(), 
                    nextSequenceItems = results.Skip(1), 
                    breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
                };

                var retryAfter = new Stack<ISequenceItemAction>();
                var result = await new ProcessSequenceItem(options).SequenceItemDispatcherAsync(retryAfter);
                
                if (!result.IsSuccess)
                    Fail();

                return result.Model;
                //result = result && !retryAfter.Any();
                //return result ? 0 : 1;
            });
        }

        //private class HarEntry
        //{
        //    public int Index { get; set; }
        //    public Entry Entry { get; set; }
        //    public object Result { get; set; }
        //}

        private IEnumerable<SequenceItem> ProcessEntries(Log log)
        {
            // convert entries to sequence item -> send



            var tasks = new List<SequenceItem>();
            var index = 0;
            foreach (var entry in log.Entries)
                tasks.Add(ProcessEntry(index++, log, entry));

            //var results = await Task.WhenAll(tasks.ToArray());

            return tasks;
        }

        /// <summary>
        /// This function converts a HarSharp POCO into a series of SequenceItemSend instances
        /// </summary>
        private SequenceItem ProcessEntry(int index, Log log, Entry entry)
        {
            var clone = JsonConvert.SerializeObject(base.sequenceItem);            
            var ret = JsonConvert.DeserializeObject<SequenceItem> (clone);

            ret.command = $"{ret.command}-har-log-entries[{index}]";
            ret.har = null;
            ret.is_abort_on_exception = true;
            ret.send = new UrlRequest
            { 
                url = entry.Request.Url.AbsoluteUri,
                http_method = entry.Request.Method,
                body = entry.Request.PostData?.Text,
                content_type = entry.Request.PostData?.MimeType,
                query = new KeyValueList(entry.Request.QueryString?.Select(q => new KeyValuePair<string, string>(q.Name, q.Value)).ToList()),
                header = new KeyValueList(entry.Request.Headers?.Select(q => new KeyValuePair<string, string>(q.Name, q.Value)).ToList())                
            };

            //var retval = new HarEntry { Index = index, Entry = entry };
            //var sent = new UrlRequest { url = entry. };
            //retval.Result = HttpSequencer.SequenceItemDispatcherAsync(this.state, this.Parent, this.model, new SequenceItem { }, this.nextSequenceItems, );

            //return retval;
            return ret;
        }

        private async Task<Har> GetHarContentsAsync()
        {
            if (this.sequenceItem.har.direct != null)
                return this.sequenceItem.har.direct;

            if (this.sequenceItem.har.file != null)
                return JsonConvert.DeserializeObject<Har>(File.ReadAllText(this.sequenceItem.har.file));
            
            if (this.sequenceItem.har.json != null)
                return JsonConvert.DeserializeObject<Har>(this.sequenceItem.har.json);

            if (this.sequenceItem.har.url == null)
                throw new InvalidOperationException($"invalid {nameof(this.sequenceItem)} in {nameof(GetHarContentsAsync)}");

            using (var client = new HttpClient()) 
            {
                var resp = await client.GetAsync(this.sequenceItem.har.url);
                var content = await resp?.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Har>(content);
            }
        }
    }
}