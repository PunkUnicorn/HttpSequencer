using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

    }
}
