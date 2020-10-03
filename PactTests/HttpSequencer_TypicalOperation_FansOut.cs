using HttpSequencer;
using PactNet.Mocks.MockHttpService.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace PactTests
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
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        private YamlOptions MakeYamlSequence(int port)
        {
            return new YamlOptions
            {
                sequence_items = new List<SequenceItem>
                {
                    /* First */
                    new SequenceItem
                    {
                        command = "one-of-two",
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
                        command = "two-of-two",
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

        private static void BuildSuccessConsumerForId(ConsumeHttpSequencerPact consumer, string id)
        {
            consumer.MockProviderService
                .Given($"Given there is more detail for item id {id}")
                .UponReceiving($"A GET request for more detail for item id {id}")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = $"/second/{id}",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = $"More detail for id {id}" }
                });
        }

        private static void BuildFailConsumerForId(ConsumeHttpSequencerPact consumer, string id)
        {
            consumer.MockProviderService
                .Given($"Given there is more detail for item id {id}")
                .UponReceiving($"A GET request for more detail for item id {id}")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = $"/second/{id}",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 410,
                    Headers = new Dictionary<string, object> { { "Content-Type", "text/html" } },
                    Body = "<html><body>"
                });
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

            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000002");
            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");          

            var testOptions = new HttpSequencer.Options { YamlDirect = MakeYamlSequence(Port) };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequence(testOptions);


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

            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000001");
            BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");

            var testOptions = new HttpSequencer.Options { YamlDirect = MakeYamlSequence(Port) };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequence(testOptions);


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(0, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }

        [Fact]
        public void FansOutThree_ExpectedFailOnTwo()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

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

            BuildFailConsumerForId(ConsumeTestYamlPact, "00000001");
            BuildFailConsumerForId(ConsumeTestYamlPact, "00000002");
            BuildSuccessConsumerForId(ConsumeTestYamlPact, "00000003");            

            var testOptions = new HttpSequencer.Options { YamlDirect = MakeYamlSequence(Port) };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequence(testOptions);


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(0, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

        }

        [Fact]
        public void FansOutThree_ExpectedFailOnOne_ThenRetryOnce_FollwedBy_Success()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void FansOutThree_ExpectedFailOnOne_ThenRetryOnce_FollwedBy_Fail()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void FansOutThree_ExpectedFailOnOne_ThenRetryThreeTimes_FollwedBy_Fail()
        {
            throw new NotImplementedException();
        }
    }
}
