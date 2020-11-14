using HttpSequencer;
using PactNet.Mocks.MockHttpService.Models;
using PactTests;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PactTests_Blackbox
{
    /// <summary>
    /// Big text from:
    /// https://www.messletters.com/en/big-text/
    /// 
    /// Stylish text from:
    /// https://lingojam.com/StylishTextGenerator
    /// 
    /// Blocky text from:
    /// https://fsymbols.com/generators/carty/
    /// </summary>
    public class HttpSequencer_TypicalOperation_DoesntCrash
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(1000);

        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        public ConsumeHttpSequencerPact ConsumeTestYamlPact { get; }
        public int Port { get; }

        public HttpSequencer_TypicalOperation_DoesntCrash()
        {
            Port = GetAvailablePort();
            var consumerName = $"{nameof(HttpSequencer_TypicalOperation_DoesntCrash)}Consumer";
            ConsumeTestYamlPact = new ConsumeHttpSequencerPact(consumerName, Port);
            ConsumeTestYamlPact.MockProviderService.ClearInteractions();
        }

        [Fact]
        public void LoadYaml_ExpectSuccess()
        {
            int testPort = GetAvailablePort();

            string yamlContents = $@"---
sequence_items:
  - command: load-yaml-expect-success
    send:
      http_method: GET
      url: http://localhost:{testPort}";

            using (var consumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", testPort))
            using (var t = new TempFile())
            {
                consumeTestYamlPact.MockProviderService.ClearInteractions();

                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
                File.WriteAllText(t.Filename, yamlContents);

                consumeTestYamlPact.MockProviderService
                    .Given("There is an active endpoint")
                    .UponReceiving("A GET request to touch the endpoint")
                    .With(new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/",
                        Headers = new Dictionary<string, object> { { "Accept", "text/plain" } },
                    })
                    .WillRespondWith(new ProviderServiceResponse
                    {
                        Status = 200,
                        Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                        Body = { }
                    });

                var testOptions = new Options { YamlFile = t.Filename };

                /* 𝓐𝓬𝓽 */
                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequenceAsync(testOptions).Result;

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.Equal(0, result);
                consumeTestYamlPact.MockProviderService.VerifyInteractions();

            }
        }

        [Fact]
        public void OneSequence()
        {
            /*     _                                                 
                  / \     _ __   _ __    __ _   _ __     __ _    ___ 
                 / _ \   | '__| | '__|  / _` | | '_ \   / _` |  / _ \
                / ___ \  | |    | |    | (_| | | | | | | (_| | |  __/
               /_/   \_\ |_|    |_|     \__,_| |_| |_|  \__, |  \___|
                                                        |___/           */

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint")
                .UponReceiving("A GET request to the endpoint")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/",
                    Headers = new Dictionary<string, object> { { "Accept", "text/plain" } },
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "text/plain" } },
                    Body = ""
                });

            /*
            * One sequence, the simplest GET with no params
            */
            var testYamlSequence = new YamlOptions
            {
                sequence_items = new List<SequenceItem> {
                    new SequenceItem
                    {
                        command = "first-and-only",
                        send = new UrlRequest
                        {
                            http_method = "GET",
                            url = $"http://localhost:{Port}"
                        }
                    }
                }
            };

            var testOptions = new Options { YamlDirect = testYamlSequence };


            /*     _             _   
                  / \      ___  | |_ 
                 / _ \    / __| | __|
                / ___ \  | (__  | |_ 
               /_/   \_\  \___|  \__|   
                                        */

            var consumer = new HttpSequencer.HttpSequencer();

            var result = consumer.RunSequenceAsync(testOptions).Result;



            /*     _                                _   
                  / \     ___   ___    ___   _ __  | |_ 
                 / _ \   / __| / __|  / _ \ | '__| | __|
                / ___ \  \__ \ \__ \ |  __/ | |    | |_ 
               /_/   \_\ |___/ |___/  \___| |_|     \__|    
                                                                */

            Assert.Equal(0, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }

        [Fact]
        public void TwoSequences()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new YamlOptions
            {
                sequence_items = new List<SequenceItem> {
                    /* First
                     * Get a list of ids, which will be a list of one id. 
                     * For each of these (that is, for the list of one) use that id in the next request */
                    new SequenceItem
                    {
                        command = "one-of-two",
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{Port}/first"
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
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    }
                }
            };

            var testOptions = new Options { YamlDirect = testYamlSequence };


            /* 𝓐𝓬𝓽 */

            var consumer = new HttpSequencer.HttpSequencer();
            var result = consumer.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(0, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }

        [Fact]
        public void ThreeSequences_CheckPasses()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

            ConsumeTestYamlPact.MockProviderService
                .Given("There is an active endpoint that provides a list of one id")
                .UponReceiving("A GET request to retrieve the list of one id")
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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new YamlOptions
            {
                sequence_items = new List<SequenceItem> {
                    /* First */
                    new SequenceItem
                    {
                        command = "one-of-three-check-passes",
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        command = "two-of-three-check-passes",
                        is_model_array = true,
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    },
                    /* Check */
                    new SequenceItem
                    {
                        command = "three-of-three-check-passes",
                        check = new SequenceCheck
                        {
                            pass_template = "{{if model.detail=='expectedMoreDetailString'}}true{{else}}false{{end}}"
                        }
                    }
                }
            };

            var testOptions = new Options { YamlDirect = testYamlSequence };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();

            Assert.Equal(0, result);
        }

        [Fact]
        public void ThreeSequences_CheckFails()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

            const string expectedMoreDetailString = nameof(expectedMoreDetailString);

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
                    Body = new List<object> { new { Id = "00000001" } }
                });

            ConsumeTestYamlPact.MockProviderService
                .Given("Given there is more detail for item id 00000001")
                .UponReceiving("A GET request for more detail for item id 00000001")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/second/00000001",
                    Headers = new Dictionary<string, object> { { "Accept", "application/json" } },
                    Body = { }
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object> { { "Content-Type", "application/json; charset=utf-8" } },
                    Body = new { detail = expectedMoreDetailString }
                });

            var testYamlSequence = new YamlOptions
            {
                sequence_items = new List<SequenceItem> {
                    /* First */
                    new SequenceItem
                    {
                        command = "one-of-three-check-at-end-fails",
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{Port}/first"
                        }
                    },
                    /* Second */
                    new SequenceItem
                    {
                        command = "two-of-three-check-at-end-fails",
                        is_model_array = true,
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = $"http://localhost:{Port}/second/" + "{{model.Id}}"
                        }
                    },
                    /* Check */
                    new SequenceItem
                    {
                        command = "three-of-three-check-at-end-fails",
                        check = new SequenceCheck
                        {
                            pass_template = "{{if model.detail=='it will never be this'}}true{{else}}false{{end}}"
                        }
                    }
                }
            };

            var testOptions = new Options { YamlDirect = testYamlSequence };


            /* 𝓐𝓬𝓽 */

            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequenceAsync(testOptions).Result;


            /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

            Assert.Equal(1, result);

            ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
        }
    }
}
