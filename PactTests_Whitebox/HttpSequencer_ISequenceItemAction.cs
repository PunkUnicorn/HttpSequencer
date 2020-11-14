using HttpSequencer;
using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
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
        public void SequenceItemActionRun_ExpectSuccess()
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

            Assert.Null(testSequenceItemAction.Exception);

            Assert.EndsWith(ExecutableExpectedResult, actual);
        }

        [Fact]
        public void SequenceItemActionRun_ExpectFail()
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

            Assert.NotNull(testSequenceItemAction.Exception);

            Assert.Null(actual);
        }

        [Fact]
        public void SequenceItemActionRun_ScribanSubstitute()
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

            Assert.Null(testSequenceItemAction.Exception);

            Assert.EndsWith(ExecutableExpectedResult, actual);
        }

        [Fact]
        public void SequenceItemActionCheck_ExpectSuccess()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "check-item",
                check = new SequenceCheck { pass_template = "true" }
            };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testModel = new { SomeData = "this is some data" };

            var testSequenceItemAction = new SequenceItemCheck(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.False(testSequenceItemAction.IsFail);

            Assert.Null(testSequenceItemAction.Exception);

            Assert.Equal(JsonConvert.SerializeObject(testModel), JsonConvert.SerializeObject(actual));
        }

        [Fact]
        public void SequenceItemActionCheck_ExpectFail()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "check-item-expect-fail",
                check = new SequenceCheck { pass_template = "false" }
            };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testModel = new { SomeData = "this is some data" };

            var testSequenceItemAction = new SequenceItemCheck(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.True(testSequenceItemAction.IsFail);

            Assert.Null(testSequenceItemAction.Exception);

            Assert.Equal(JsonConvert.SerializeObject(testModel), JsonConvert.SerializeObject(actual));
        }

        [Fact]
        public void SequenceItemActionCheck_ScribanSubstitute_CheckPasses()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "test-check-item-sriban-expect-pass",
                check = new SequenceCheck { pass_template = "{{if model.someData == 'the correct data'}}true{{else}}false{{end}}" }
            };

            var testModel = new { someData = "the correct data" };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemCheck(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.False(testSequenceItemAction.IsFail);

            Assert.Null(testSequenceItemAction.Exception);

            Assert.Equal(JsonConvert.SerializeObject(testModel), JsonConvert.SerializeObject(actual));
        }

        [Fact]
        public void SequenceItemActionCheck_ScribanSubstitute_CheckFails()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "some-check-sriban-expect-fail",
                check = new SequenceCheck { pass_template = "{{if model.someData == 'the correct data'}}true{{else}}false{{end}}" }
            };

            var testModel = new { someData = "this is NOT the correct data" };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemCheck(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.True(testSequenceItemAction.IsFail);

            Assert.Null(testSequenceItemAction.Exception);

            Assert.Equal(JsonConvert.SerializeObject(testModel), JsonConvert.SerializeObject(actual));
        }

        [Fact]
        public void SequenceItemActionCheck_ScribanSubstitute_InvalidScriban()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮... */
            var testSequenceItem = new SequenceItem
            {
                command = "some-check-sriban-expect-invalid-template",
                check = new SequenceCheck { pass_template = "{{this is not valid scriban!}}true{{else}}false{{end}}" }
            };

            var testModel = new { someData = "some data" };

            var dummyRunState = new RunState { YamlOptions = new YamlOptions { sequence_items = new[] { testSequenceItem }.ToList() } };

            var testSequenceItemAction = new SequenceItemCheck(dummyRunState, testSequenceItem, testModel, new List<SequenceItem>());


            /* 𝓐𝓬𝓽 */
            var actual = testSequenceItemAction.ActionAsync(new CancellationToken()).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
            Assert.True(testSequenceItemAction.IsFail);

            Assert.NotNull(testSequenceItemAction.Exception);

            Assert.Null(actual);
        }
    }
}
