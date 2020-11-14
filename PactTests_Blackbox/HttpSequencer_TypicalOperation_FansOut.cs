using HttpSequencer;
using PactNet.Mocks.MockHttpService.Models;
using PactTests;
using System;
using System.Collections.Generic;
using Xunit;

namespace PactTests_Blackbox
{
    public class HttpSequencer_TypicalOperation_FansOut
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(3000);

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        private ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        private int Port { get; }

        public HttpSequencer_TypicalOperation_FansOut()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_TypicalOperation_FansOut)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        private YamlOptions MakeYamlSequence(int port, string commandPostfix)
        {
            return new YamlOptions
            {
                sequence_items = new List<SequenceItem>
                {
                    /* First */
                    new SequenceItem
                    {
                        command = $"one-of-two-{commandPostfix}",
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        command = $"two-of-two-{commandPostfix}",
                        is_model_array = true,
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{port}/second/" + "{{model.Id}}"
                        }
                    },
                }
            };
        }

        [Fact]
        public void FansOutThree_ExpectedSuccess()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of ids")
                .UponReceiving("A GET request to retrieve the list")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/first",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new List<object> {
                        new { Id = "00000001" },
                        new { Id = "00000002" },
                        new { Id = "00000003" },
                    }
                });

            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");          

            var testOptions = new Options { YamlDirect = MakeYamlSequence(Port, "expect-success") };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(0, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }

        [Fact]
        public void FansOutThree_ExpectedFailOnOne()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of three ids")
                .UponReceiving("A GET request to retrieve the list of three ids")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/first",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new List<object> {
                        new { Id = "00000001" }, // <--- Expected success
                        new { Id = "00000002" }, // <--- Expected fail
                        new { Id = "00000003" }, // <--- Expected success
                    }
                });

            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");

            var testOptions = new Options { YamlDirect = MakeYamlSequence(Port, "expect-fail-on-one") };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(1, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }

        [Fact]
        public void FansOutThree_ExpectedFailOnTwo()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of three ids")
                .UponReceiving("A GET request to retrieve the list of three ids")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/first",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new List<object> {
                        new { Id = "00000001" }, // <--- Expected fail
                        new { Id = "00000002" }, // <--- Expected fail
                        new { Id = "00000003" }, // <--- Expected success
                    }
                });

            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");
            SharedPactScafolding.BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            SharedPactScafolding.BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");            

            var testOptions = new Options { YamlDirect = MakeYamlSequence(Port, "expect-fail-on-two") };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(1, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }
    }
}
