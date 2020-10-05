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
                max_retrys = 1,
                send = new UrlRequest
                {
                    header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json") },
                    http_method = "GET",
                    url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                }
            };
        }

        [Fact]
        public void ExpectRetry_BadUrl()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };
            var dummyToDoAfterList = new List<SequenceItem>();

            var testModel = new { Id = "00000001" };
            

            /* 𝓐𝓬𝓽 */
            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var actualResult = SequenceItemDispatcherAsync(
                dummyRunState,
                null, 
                testModel,
                testSequenceItem,
                dummyToDoAfterList,
                actualRetryAfterResult, 
                new Stack<ISequenceItemAction>()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.False(actualResult);

            Assert.Same(testSequenceItem, actualRetryAfterResult.Single().GetSequenceItem());

            Assert.Contains(actualRetryAfterResult, 
                item => item.ActionExecuteCount == 1);
        }

        [Fact]
        public void NoRetry()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };
            var dummyToDoAfterList = new List<SequenceItem>();

            var testModel = new { Id = "00000001" };


            /* 𝓐𝓬𝓽 */
            var actualRetryAfterResult = new Stack<ISequenceItemAction>();

            var actualResult = SequenceItemDispatcherAsync(
                dummyRunState,
                null,
                testModel,
                testSequenceItem,
                dummyToDoAfterList,
                actualRetryAfterResult,
                new Stack<ISequenceItemAction>()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.True(actualResult);

            Assert.Empty(actualRetryAfterResult);
        }
    }
}
