using HttpSequencer;
using HttpSequencer.SequenceItemActions;
using PactTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using static HttpSequencer.HttpSequencer;

namespace PactTests_Whitebox
{
    public class HttpSequencer_SequenceItemRetryDispatcher
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(4000);

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        private ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        private int Port { get; }

        public HttpSequencer_SequenceItemRetryDispatcher()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_SequenceItemRetryDispatcher)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        [Fact]
        public void RetryOnce_EndingWith_Success()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */

            /* ...the Pact server */
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");

            /* ...the gubbins for the HttpSequencer component to work*/

            var testModel = new { Id = "00000001" };
            var testSequenceItem = new SequenceItem
            {
                command = "something-that-previously-failed",
                max_delayed_retrys = 1,
                send = new UrlRequest
                {
                    header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json") },
                    http_method = "GET",
                    url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                }
            };

            var dummyRunAfterList = new SequenceItem[] { };
            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testRetryList = new List<ISequenceItemAction>() { new SequenceItemSend(dummyRunState, testSequenceItem, testModel, dummyRunAfterList) };
            
            var processorOptions = new ProcessSequenceItem.Options
            {
                state = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } },
                parent = null,
                model = null,
                sequenceItem = null,
                breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
            };

            /* 𝓐𝓬𝓽 */
            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var actualResult = new ProcessSequenceItem(processorOptions).SequenceItemRetryDispatcherAsync(
                new CancellationToken(),
                false,
                testRetryList,
                dummyRunState,
                actualRetryAfterResult,
                new Stack<KeyValuePair<string, ISequenceItemAction>>()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.True(actualResult.IsSuccess);

            Assert.Empty(actualRetryAfterResult);
        }

        [Fact]
        public void RetryOnce_EndingWith_Fail()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */

            /* ...the Pact server */
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");

            /* ...the gubbins for the HttpSequencer component*/
            var dummyRunState = new RunState { };
            var dummyRunAfterList = new SequenceItem[] { };

            var testModel = new { Id = "00000001" };
            var testSequenceItem = new SequenceItem
            {
                command = "something-that-previously-failed",
                max_delayed_retrys = 1,
                send = new UrlRequest
                {
                    header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json") },
                    http_method = "GET",
                    url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                }
            };
            dummyRunState.YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() };
            var testRetryList = new List<ISequenceItemAction>() { new SequenceItemSend(dummyRunState, testSequenceItem, testModel, dummyRunAfterList) };

            var processorOptions = new ProcessSequenceItem.Options
            {
                state = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } },
                parent = null,
                model = null,
                sequenceItem = null,
                breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
            };

            /* 𝓐𝓬𝓽 */

            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var actualResult = new ProcessSequenceItem(processorOptions).SequenceItemRetryDispatcherAsync(
                new CancellationToken(),
                false,
                testRetryList,
                dummyRunState,
                actualRetryAfterResult,
                new Stack<KeyValuePair<string, ISequenceItemAction>>()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.False(actualResult.IsSuccess);

            Assert.Contains(actualRetryAfterResult,
                item => item.ActionExecuteCount == 1);
        }
    }
}
