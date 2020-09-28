using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HttpSequencer.SequenceItemActions
{
    public class SequenceItemSend : ISequenceItemAction
    {
        private readonly SequenceItem sequenceItem;
        private readonly RunState state;

        //public bool IsFail { get; private set; }

        public string workingUri { get; internal set; }
        public string workingBody { get; private set; }

        public SequenceItemSend(RunState state, SequenceItem sequenceItem)
        {
            this.sequenceItem = sequenceItem;
            this.state = state;
        }

        public async Task<object> Action(object model)
        {
            if (this.sequenceItem.send == null)
                return null;

            var modelUrl = this.sequenceItem.send.url ?? "";
            var scribanModel = new
            {
                run_id = this.state.RunId,
                command_args = this.state.CommandLineOptions,
                previous_response = model,
                sequence_item = this.sequenceItem
            };
            this.workingUri = ScribanUtil.ScribanParse(this.sequenceItem.send.url, scribanModel);

            this.state.Log.Info($"Processing {this.workingUri}...");

            dynamic responseModel = null;
            using (var client = MakeClientWithHeaders(this.state.CommandLineOptions, this.state.YamlOptions, this.sequenceItem))
            try
            {
                var http_response = await DoSendAction(client, scribanModel);

                var responseContentLength = http_response?.Content?.Headers?.ContentLength ?? 0;
                var responseContent = responseContentLength > 0
                    ? (await http_response?.Content?.ReadAsStringAsync())
                    : string.Empty;

                this.state.ProgressLog.Progress($" received {responseContentLength} bytes...");
                responseModel = SequenceItemStatic.GetResponseItems(this.state, this.sequenceItem, responseContent);

                SavingContentsEtc(Guid.NewGuid().ToString(), responseModel, http_response, responseContentLength, responseContent);
            }
            catch (Exception e)
            {
                //GenericExceptionHandler(e, o?.Command ?? ("-" + nameof(ProcessCommandItem)));

                state.Exceptions.Add( (e.Message, e) );
                if (this.sequenceItem.is_abort_on_exception)
                    throw;
            }

            return responseModel;
        }

        private async Task<HttpResponseMessage> DoSendAction(HttpClient client, object scribanModel)
        {
            this.state.Log.Info($" using method '{this.sequenceItem.send.http_method}'...");

            if (this.sequenceItem.send.query != null)
                workingUri = AppendSendActionQuery(this.workingUri, scribanModel);

            if (this.sequenceItem.send?.body != null)
            {
                this.workingBody = ScribanUtil.ScribanParse(this.sequenceItem.send.body, scribanModel);
                this.state.Log.Info($" using content body '{this.workingBody}'...");

                if (this.sequenceItem.send.save_body_filename != null)
                    SaveSendActionBody(scribanModel, workingBody);
            }

            this.state.Log.Info($" using url '{this.workingUri}'...");

            // Process the response content
            var contenType = this.sequenceItem.send.content_type ?? "text/plain";
            return await SortOutHttpMethodAndReturnResult(this.sequenceItem.max_retrys, client, this.sequenceItem.send.http_method, contenType, workingBody);
        }

        private string AppendSendActionQuery(string workingUri, object scribanModel)
        {
            foreach (var query in this.sequenceItem.send.query)
            {
                var templatedValue = ScribanUtil.ScribanParse(query.Value, scribanModel);
                workingUri = QueryHelpers.AddQueryString(workingUri, query.Key, templatedValue);
            }

            return workingUri;
        }

        private void SaveSendActionBody(object scribanModel, string workingBody)
        {
            var saveBodyFilename = ScribanUtil.ScribanParse(this.sequenceItem.send.save_body_filename, scribanModel);
            var saveBodyPath = Path.GetDirectoryName(saveBodyFilename);
            if (!Directory.Exists(saveBodyPath))
                Directory.CreateDirectory(saveBodyPath);

            File.WriteAllText(saveBodyFilename, workingBody);
        }

        private void SavingContentsEtc(string itemUniqueThing, dynamic responseModel, HttpResponseMessage httpResponse, long responseContentLength, string responseContent)
        {
            if (this.sequenceItem?.run?.save == null)
                return;

            var saveModel = new { run_id = this.state.RunId, command_args = this.state.CommandLineOptions, sequence_item = this.sequenceItem, response = responseModel, unique_string = itemUniqueThing };
            var folderSaveName = this.sequenceItem.run.save?.folder ?? "";
            var saveTo = ScribanUtil.ScribanParse(folderSaveName, saveModel);

            var contentSaveName = this.sequenceItem.run.save?.content_filename ?? "";
            if (contentSaveName.Trim().Length > 0)
            {
                var contentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(contentSaveName, saveModel));
                this.state.Log.Info($" saving content to '{contentFn }'...");

                Directory.CreateDirectory(Path.GetDirectoryName(contentFn));

                if (this.sequenceItem.run.save.content_is_binary)
                {
                    if (httpResponse != null) using (Stream output = File.OpenWrite(contentFn)) { Task.WaitAll(httpResponse.Content.CopyToAsync(output)); }
                }
                else
                    File.WriteAllText(contentFn, responseContent);
            }

            var responseSaveName = this.sequenceItem.run.save?.response_filename ?? "";
            if (httpResponse != null && responseSaveName.Trim().Length > 0)
            {
                var nonContentFn = Path.Combine(saveTo, ScribanUtil.ScribanParse(responseSaveName, saveModel));
                this.state.Log.Info($" saving non content response to '{nonContentFn}'...");

                var nonContent = new
                {
                    httpResponse.StatusCode,
                    httpResponse.Headers,
                    httpResponse.ReasonPhrase,
                    httpResponse.IsSuccessStatusCode,
                    //httpResponse.TrailingHeaders,
                    httpResponse.RequestMessage,
                    ContentLength = responseContentLength
                };

                Directory.CreateDirectory(Path.GetDirectoryName(nonContentFn));
                File.WriteAllText(nonContentFn, JsonConvert.SerializeObject(nonContent));
            }
        }

        private async Task<HttpResponseMessage> SortOutHttpMethodAndReturnResult(int? retryCount, HttpClient client, string method, string mediaType, string workingBody)
        {
            try
            {
                HttpResponseMessage methodResult;

                var policy = Policy
                  .Handle<Exception>()
                  .Retry(retryCount ?? 1);

                var ret = policy.Execute<Task<HttpResponseMessage>>(async () => 
                { 
                    var url = this.workingUri;
                    switch (method.ToUpper())
                    {
                        case "GET":
                            methodResult = await client.GetAsync(url);
                            break;

                        case "PUT":
                            /*UNTESTED*/
                            methodResult = await client.PutAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        case "POST":
                            /*UNTESTED*/
                            methodResult = await client.PostAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        case "PATCH":
                            /*UNTESTED*/
                            methodResult = await client.PatchAsync(url, new StringContent(workingBody, Encoding.UTF8, mediaType));
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown {nameof(method)}: '{method}'");
                    }
                    return methodResult;
                    //var retval = await methodResult;
                    //return retval;
                });

                return await ret;
            }
            catch (Exception e)
            {
                throw;
                //GenericExceptionHandler(e, method + "," + url);
            }
        }

        private static HttpClient MakeClientWithHeaders(Options o, YamlOptions yaml, SequenceItem entry = null)
        {
            var client = new HttpClient();

            const int defaultClientTimeoutSeconds = 90;
            client.Timeout = TimeSpan.FromSeconds(yaml.client_timeout_seconds ?? defaultClientTimeoutSeconds);

            var scribanModel = new { yaml.run_id, command_args = o, previous_response = new { } };
            client.DefaultRequestHeaders.Accept.Clear();
            if (entry?.send?.header != null)
            {

                /* UN TESTED  - for POST*/

                //Add customer headers
                foreach (var addHeader in entry.send.header)
                    if (addHeader.Key.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ScribanUtil.ScribanParse(addHeader.Value, scribanModel)));
                    else
                        client.DefaultRequestHeaders.Add(addHeader.Key, ScribanUtil.ScribanParse(addHeader.Value, scribanModel));
            }

            if (client.DefaultRequestHeaders.Accept.Count == 0)
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            //if (!client.DefaultRequestHeaders.Contains("toc-code"))
            //    client.DefaultRequestHeaders.Add("toc-code", ScribanUtil.ScribanParse(yaml.header.toc_code, scribanModel));

            //if (!client.DefaultRequestHeaders.Contains("fleet-code"))
            //    client.DefaultRequestHeaders.Add("fleet-code", ScribanUtil.ScribanParse(yaml.header.fleet_code, scribanModel));

            //if (!client.DefaultRequestHeaders.Contains("Authorization"))
            //    client.DefaultRequestHeaders.Add("Authorization", ScribanUtil.ScribanParse(yaml.header.auth_code, scribanModel));

            return client;
        }
    }
}
