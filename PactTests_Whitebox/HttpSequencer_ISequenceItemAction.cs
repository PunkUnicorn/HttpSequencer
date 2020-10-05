using HttpSequencer;
using HttpSequencer.SequenceItemActions;
using PactTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace PactTests_Whitebox
{
    public class HttpSequencer_ISequenceItemAction
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(6000);

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        private ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        private int Port { get; }

        private string ExecutableFilename = @".\DosAndLinuxBashScript.cmd";
        private string ExecutableExpectedResult = "EXPECTED RESULT";

        public HttpSequencer_ISequenceItemAction()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_ISequenceItemAction)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        [Fact]
        public void SequenceItemActionRun_BasicallyWorks_ExpectSuccess()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "test-cmd-script",
                run = new Run { exec = ExecutableFilename }
            };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemRun(dummyRunState, testSequenceItem, null, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = (string) testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.False(testSequenceItemAction.IsFail);

            Assert.EndsWith(ExecutableExpectedResult, actual);
        }

        [Fact]
        public void SequenceItemActionRun_BasicallyWorks_ExpectFail()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "some-run-command",
                run = new Run { exec = "this is an invalid command" }
            };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemRun(dummyRunState, testSequenceItem, null, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.True(testSequenceItemAction.IsFail);

            Assert.Null(actual);
        }

        [Fact]
        public void SequenceItemActionRun_TypicalOperation_ScribanSubstitute()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "test-cmd-script",
                run = new Run { exec = "{{model.param1}}" }
            };

            var testModel = new { param1 = ExecutableFilename };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemRun(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = (string)testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.False(testSequenceItemAction.IsFail);

            Assert.EndsWith(ExecutableExpectedResult, actual);
        }
    }
}
