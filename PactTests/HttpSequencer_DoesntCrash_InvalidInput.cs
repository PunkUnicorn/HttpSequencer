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
    public class HttpSequencer_DoesntCrash_InvalidInput
    {
        private readonly PortAllocationFixture mrPorty = new PortAllocationFixture(2000);
        public Func<int> GetAvailablePort => mrPorty.GetAvailablePort;

        [Fact]
        public void HttpSequencer_DoesntCrash_NoYaml()
        {
            /* Arrange */
            var testOptions = new Options();

            /* Act */
            var consumer = new HttpSequencer.HttpSequencer();
            var result = consumer.RunSequence(testOptions);

            /* Assert */
            Assert.Equal(2, result);
        }

        [Fact]
        public void HttpSequencer_DoesntCrash_InvalidUrl()
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
                            header = new CurtNameList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                            http_method = "GET",
                            url = "http://doesnt-even-exist-7djemd/totally-invalid-url"
                        }
                    }
                }
            };
            var testOptions = new Options { YamlDirect = testYamlSequence };

            /* Act */
            var consumer = new HttpSequencer.HttpSequencer();
            var result = consumer.RunSequence(testOptions);

            /* Assert */
            Assert.Equal(1, result);
        }

        [Fact]
        public void HttpSequencer_DoesntCrash_InvalidUrlForSecondSequenceItem()
        {
            int firstTestPort = GetAvailablePort();
            int secondTestPort = GetAvailablePort();

            using (var ConsumeTestYamlPact = new ConsumeHttpSequencerPact("FirstConsumer", firstTestPort))
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
                                header = new CurtNameList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                http_method = "GET",
                                url = $"http://localhost:{firstTestPort}/first"
                            }
                        },
                        /* Second */
                        new SequenceItem
                        {
                            command = "two-of-two",
                            send = new UrlRequest
                            {
                                header = new CurtNameList { new KeyValuePair<string, string>("Accept", "application/json" ) },
                                http_method = "GET",
                                url = "http://doesnt-even-exist-7djemd/totally-invalid-url/{{previous_response.Id}}"
                            }
                        },
                        ///* Check */
                        //new SequenceItem
                        //{
                        //    command = "check",
                        //    check = new SequenceCheck
                        //    {
                        //        pass_template = "{{if previous_response.detail=='expectedMoreDetailString'}}true{{else}}false{{end}}"
                        //    }
                        //}
                    }
                };

                var testOptions = new Options { YamlDirect = testYamlSequence };

                /* 🅰🅲🆃 */
                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);

                /* 🅰🆂🆂🅴🆁🆃 */
                Assert.Equal(1, result);
                ConsumeTestYamlPact.MockProviderService.VerifyInteractions();
            }
        }

        public class TempFile : IDisposable
        {
            public TempFile()
            {
                Filename = Path.GetTempFileName();
            }

            public string Filename { get; }

            public void Dispose()
            {
                File.Delete(Filename);
            }
        }

        [Fact]
        public void HttpSequencer_DoesntCrash_LoadYaml_ExpectSuccess()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
            const string contents = @"
---
sequence_items:
  - command: start
    send:
      http_method: GET
      url: www.google.com
";

            using (var t = new TempFile())
            {
                File.WriteAllText(t.Filename, contents);

                var testOptions = new Options { YamlFile = t.Filename };

                /* 𝓐𝓬𝓽 */
                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public void HttpSequencer_DoesntCrash_LoadYaml_ExpectFail()
        {
            /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */
            const string contents = @"
---
sequence_items:
  - command: start
    send:
      http_m"; // Incomplete yaml

            using (var t = new TempFile())
            {
                File.WriteAllText(t.Filename, contents);

                var testOptions = new Options { YamlFile = t.Filename };


                /* 𝓐𝓬𝓽 */
                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);

                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */
                Assert.Equal(2, result);
            }
        }
    }
}
