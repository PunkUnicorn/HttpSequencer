using HttpSequencer;
using NLog.LayoutRenderers;
using PactNet.Mocks.MockHttpService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PactTests
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
    public class HttpSequencer_InvalidInput_DoesntCrash
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(2000);
        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        [Fact]
        public void NoYaml()
        {
            /* Arrange */
            var testOptions = new Options();

            /* Act */
            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequence(testOptions);

            /* Assert */
            Assert.Equal(2, result);
        }

        [Fact]
        public void InvalidUrl()
        {
            /* Arrange */
            var testYamlSequence = new YamlOptions
            {
                sequence_items = new List<SequenceItem> {
                    new SequenceItem
                    {
                        command = "one-and-only",
                        send = new UrlRequest
                        {
                            header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = "http://doesnt-even-exist-7djemd/totally-invalid-url"
                        }
                    }
                }
            };
            var testOptions = new Options { YamlDirect = testYamlSequence };

            /* Act */
            var provider = new HttpSequencer.HttpSequencer();
            var result = provider.RunSequence(testOptions);

            /* Assert */
            Assert.Equal(1, result);
        }

        [Fact]
        public void InvalidUrlForSecondSequenceItem()
        {
            int testPort = GetAvailablePort();

            using (var ConsumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", testPort))
            {
                ConsumeTestYamlPact.MockProviderService.ClearInteractions();


                /* 🅰🆁🆁🅰🅽🅶🅴  */
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
                        Body = new Dictionary<string, object> { { "Id", "00000001" } }
                    });

                var testYamlSequence = new YamlOptions
                {
                    sequence_items = new List<SequenceItem> {
                        /* First */
                        new SequenceItem
                        {
                            command = "one-of-two",
                            send = new UrlRequest
                            {
                                header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                http_method = "GET",
                                url = $"http://localhost:{testPort}/first"
                            }
                        },
                        /* Second */
                        new SequenceItem
                        {
                            command = "two-of-two",
                            send = new UrlRequest
                            {
                                header = new KeyValueList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                http_method = "GET",
                                url = "http://doesnt-even-exist-7djemd/totally-invalid-url/{{model.Id}}"
                            }
                        }
                    }
                };

                var testOptions = new Options { YamlDirect = testYamlSequence };

                /* 🅰🅲🆃 */
                var provider = new HttpSequencer.HttpSequencer();
                var result = provider.RunSequence(testOptions);

                /* 🅰🆂🆂🅴🆁🆃 */
                Assert.Equal(1, result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
            }
        }

        [Fact]
        public void LoadYaml_ExpectSuccess()
        {
            int testPort = GetAvailablePort();

            string yamlContents = $@"---
sequence_items:
  - command: start
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
                var result = consumer.RunSequence(testOptions);

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.Equal(0, result);
                consumeTestYamlPact.MockProviderService.VerifyInteractions();
                
            }
        }

        [Fact]
        public void LoadYaml_ExpectFail()
        {
            const string invalidYamlContents = @"---
sequence_items:
  - command: start
    send:
      http_m.... <!SYSTEM ERRORFZzzxsh he҉͇͈͎̞ ̙̫̖̻͖͞co̙͙̖̠̟̯̙m̫̦̹͔e͚̦͓̖̝s̘͖̣̼̫̠̙̀";

            using (var t = new TempFile())
            {
                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
                File.WriteAllText(t.Filename, invalidYamlContents);

                var testOptions = new Options { YamlFile = t.Filename };

                /* 𝓐𝓬𝓽 */
                var provider = new HttpSequencer.HttpSequencer();
                var result = provider.RunSequence(testOptions);

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.Equal(2, result);
            }
        }
    }
}
