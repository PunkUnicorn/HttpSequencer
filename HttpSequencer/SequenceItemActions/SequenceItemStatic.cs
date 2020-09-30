using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HttpSequencer.SequenceItemActions
{
    public static class SequenceItemStatic
    {
        public static dynamic GetResponseItems(RunState state, SequenceItem sequenceItem, string content)
        {
            dynamic responseModel;
            var item_quantity_cap = sequenceItem?.take_only_n;
            try
            {
                if (item_quantity_cap != null)
                    state.ProgressLog.Progress($" taking the first {item_quantity_cap} results only...");

                responseModel = item_quantity_cap != null
                    ? JsonConvert.DeserializeObject<List<dynamic>>(content).Take(item_quantity_cap.Value).ToList()
                    : JsonConvert.DeserializeObject<dynamic>(content);
            }
            catch
            {
                responseModel = content;
            }
            return responseModel;
        }

        public static object Clone(object model)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(model)) as dynamic;
        }

        public static T FailableRun<T>(ISequenceItemAction sia, Func<T> f)
        { 
            try { return f(); } 
            catch (Exception e) 
            { 
                sia.Exception = e; 
                sia.IsFail = true; 
            };

            return default(T);
        }
    }
}
