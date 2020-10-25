using HttpSequencer;
using HttpSequencer.SequenceItemActions;
using PactTests;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static HttpSequencer.HttpSequencer;

namespace PactTests_Whitebox
{
    public class HttpSequencer_SequenceItemDispatcher
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(5000);
        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        private ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        private int Port { get; }

        private SequenceItem testSequenceItem { get; } = null;

        public HttpSequencer_SequenceItemDispatcher()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_SequenceItemDispatcher)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
            testSequenceItem = new SequenceItem
            {
                command = "some-request",
                max_delayed_retrys = 1,
                send = new UrlRequest
                {
                    header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json") },
                    http_method = "GET",
                    url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                }
            };
        }

        [Fact]
        public void ExpectFailAndRetry()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");

            var processorOptions = new ProcessSequenceItem.Options
            {
                state = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } },
                parent = null,
                model = new { Id = "00000001" },
                sequenceItem = testSequenceItem,
                breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
            };


            /* 𝓐𝓬𝓽 */
            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var processor = new ProcessSequenceItem(processorOptions);
            var actualResult = processor.ProcessSequenceItemAsync(actualRetryAfterResult).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.False(actualResult);

            Assert.Same(testSequenceItem, actualRetryAfterResult.Single().GetSequenceItem());

            Assert.Contains(actualRetryAfterResult,
                item => item.ActionExecuteCount == 2);
        }

        [Fact]
        public void ExpectSuccess()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");

            var processorOptions = new ProcessSequenceItem.Options
            {
                state = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } },
                parent = null,
                model = new { Id = "00000001" },
                sequenceItem = testSequenceItem,
                breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
            };

            /* 𝓐𝓬𝓽 */
            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var processor = new ProcessSequenceItem(processorOptions);
            var actualResult = processor.SequenceItemDispatcherAsync(actualRetryAfterResult).Result;

            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.True(actualResult.IsSuccess);

            Assert.Empty(actualRetryAfterResult);
        }
    }
}
