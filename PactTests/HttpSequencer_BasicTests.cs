using HttpSequencer;
using PactNet.Mocks.MockHttpService.Models;
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
    /// </summary>
    public class HttpSequencer_BasicTests
    {
        // Fails with no yaml

        // Fails with invalid url

        // Fails with second invalid url

        // Fails with check fail

        // load yaml smoke test


        [Fact]
        public void HttpSequencer_OneSequenceDoesntCrash()
        {
            var testPort = 7878;

            using (var ConsumeTestYaml1Pact = new ConsumeTestYaml1Pact("TestConsumer", testPort))
            {
                ConsumeTestYaml1Pact.MockProviderService.ClearInteractions();

                /*     _                                                 
                      / \     _ __   _ __    __ _   _ __     __ _    ___ 
                     / _ \   | '__| | '__|  / _` | | '_ \   / _` |  / _ \
                    / ___ \  | |    | |    | (_| | | | | | | (_| | |  __/
                   /_/   \_\ |_|    |_|     \__,_| |_| |_|  \__, |  \___|
                                                            |___/           */

                ConsumeTestYaml1Pact.MockProviderService
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
                                url = $"http://localhost:{testPort}"
                            }
                        }
                    }
                };

                var testOptions = new HttpSequencer.Options { YamlDirect = testYamlSequence };


                /*     _             _   
                      / \      ___  | |_ 
                     / _ \    / __| | __|
                    / ___ \  | (__  | |_ 
                   /_/   \_\  \___|  \__|   
                                            */

                var consumer = new HttpSequencer.HttpSequencer();

                var result = consumer.RunSequence(testOptions);



                /*     _                                _   
                      / \     ___   ___    ___   _ __  | |_ 
                     / _ \   / __| / __|  / _ \ | '__| | __|
                    / ___ \  \__ \ \__ \ |  __/ | |    | |_ 
                   /_/   \_\ |___/ |___/  \___| |_|     \__|    
                                                                */

                Assert.Equal(0, result);

                ConsumeTestYaml1Pact.MockProviderService.VerifyInteractions();
            }
        }

        [Fact]
        public void HttpSequencer_TwoSequencesDoesntCrash()
        {
            const int firstTestPort = 7879;
            const int secondTestPort = 7880;

            using (var ConsumeTestYamlPact_First = new ConsumeTestYaml1Pact("FirstConsumer", firstTestPort)) 
            using (var ConsumeTestYamlPact_Second = new ConsumeTestYaml1Pact("SecondConsumer", secondTestPort))
            {
                ConsumeTestYamlPact_First.MockProviderService.ClearInteractions(); 
                ConsumeTestYamlPact_Second.MockProviderService.ClearInteractions(); 


                /*    :::     :::::::::  :::::::::      :::     ::::    :::  ::::::::  :::::::::: 
                    :+: :+:   :+:    :+: :+:    :+:   :+: :+:   :+:+:   :+: :+:    :+: :+:        
                   +:+   +:+  +:+    +:+ +:+    +:+  +:+   +:+  :+:+:+  +:+ +:+        +:+        
                  +#++:++#++: +#++:++#:  +#++:++#:  +#++:++#++: +#+ +:+ +#+ :#:        +#++:++#   
                  +#+     +#+ +#+    +#+ +#+    +#+ +#+     +#+ +#+  +#+#+# +#+   +#+# +#+        
                  #+#     #+# #+#    #+# #+#    #+# #+#     #+# #+#   #+#+# #+#    #+# #+#        
                  ###     ### ###    ### ###    ### ###     ### ###    ####  ########  ########## 
                                                                                                    */
                const string expectedMoreDetailString = nameof(expectedMoreDetailString);

                ConsumeTestYamlPact_First.MockProviderService
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

                ConsumeTestYamlPact_Second.MockProviderService
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
                                url = $"http://localhost:{secondTestPort}/second/" + "{{previous_response.Id}}"
                            }
                        }
                    }
                };

                var testOptions = new HttpSequencer.Options { YamlDirect = testYamlSequence };


                /*    :::      ::::::::  ::::::::::: 
                    :+: :+:   :+:    :+:     :+:     
                   +:+   +:+  +:+            +:+     
                  +#++:++#++: +#+            +#+     
                  +#+     +#+ +#+            +#+     
                  #+#     #+# #+#    #+#     #+#     
                  ###     ###  ########      ###     
                                                      */
                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);


                /*    :::      ::::::::   ::::::::  :::::::::: :::::::::  ::::::::::: 
                    :+: :+:   :+:    :+: :+:    :+: :+:        :+:    :+:     :+:     
                   +:+   +:+  +:+        +:+        +:+        +:+    +:+     +:+     
                  +#++:++#++: +#++:++#++ +#++:++#++ +#++:++#   +#++:++#:      +#+     
                  +#+     +#+        +#+        +#+ +#+        +#+    +#+     +#+     
                  #+#     #+# #+#    #+# #+#    #+# #+#        #+#    #+#     #+#     
                  ###     ###  ########   ########  ########## ###    ###     ###       
                                                                                        */
                Assert.Equal(0, result);

                ConsumeTestYamlPact_First.MockProviderService.VerifyInteractions();
                ConsumeTestYamlPact_Second.MockProviderService.VerifyInteractions(); 
            }
        }

        [Fact]
        public void HttpSequencer_TwoSequencesWithACheckDoesntCrash_CheckPasses()
        {
            const int firstTestPort = 7883;
            const int secondTestPort = 7884;

            using (var ConsumeTestYamlPact_First = new ConsumeTestYaml1Pact("FirstConsumer", firstTestPort))
            using (var ConsumeTestYamlPact_Second = new ConsumeTestYaml1Pact("SecondConsumer", secondTestPort))
            {
                ConsumeTestYamlPact_First.MockProviderService.ClearInteractions();
                ConsumeTestYamlPact_Second.MockProviderService.ClearInteractions();


                /* 🅰🆁🆁🅰🅽🅶🅴  */

                const string expectedMoreDetailString = nameof(expectedMoreDetailString);

                ConsumeTestYamlPact_First.MockProviderService
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

                ConsumeTestYamlPact_Second.MockProviderService
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
                                url = $"http://localhost:{secondTestPort}/second/" + "{{previous_response.Id}}"
                            }
                        },
                        /* Check */
                        new SequenceItem
                        {
                            command = "check",
                            check = new SequenceCheck
                            {
                                pass_template = "{{if previous_response.detail=='expectedMoreDetailString'}}true{{else}}false{{end}}"
                            }
                        }
                    }
                };

                var testOptions = new HttpSequencer.Options { YamlDirect = testYamlSequence };


                /* 🅰🅲🆃 */

                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);


                /* 🅰🆂🆂🅴🆁🆃 */

                Assert.Equal(0, result);

                ConsumeTestYamlPact_First.MockProviderService.VerifyInteractions();
                ConsumeTestYamlPact_Second.MockProviderService.VerifyInteractions();
            }
        }

        [Fact]
        public void HttpSequencer_TwoSequencesWithACheckDoesntCrash_CheckFails()
        {
            const int firstTestPort = 7883;
            const int secondTestPort = 7884;

            using (var ConsumeTestYamlPact_First = new ConsumeTestYaml1Pact("FirstConsumer", firstTestPort))
            using (var ConsumeTestYamlPact_Second = new ConsumeTestYaml1Pact("SecondConsumer", secondTestPort))
            {
                ConsumeTestYamlPact_First.MockProviderService.ClearInteractions();
                ConsumeTestYamlPact_Second.MockProviderService.ClearInteractions();


                /* 𝓐𝓻𝓻𝓪𝓷𝓰𝓮 */

                const string expectedMoreDetailString = nameof(expectedMoreDetailString);

                ConsumeTestYamlPact_First.MockProviderService
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

                ConsumeTestYamlPact_Second.MockProviderService
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
                                url = $"http://localhost:{secondTestPort}/second/" + "{{previous_response.Id}}"
                            }
                        },
                        /* Check */
                        new SequenceItem
                        {
                            command = "check",
                            check = new SequenceCheck
                            {
                                pass_template = "{{if previous_response.detail=='it will never be this'}}true{{else}}false{{end}}"
                            }
                        }
                    }
                };

                var testOptions = new HttpSequencer.Options { YamlDirect = testYamlSequence };


                /* 𝓐𝓬𝓽 */

                var consumer = new HttpSequencer.HttpSequencer();
                var result = consumer.RunSequence(testOptions);


                /* 𝓐𝓼𝓼𝓮𝓻𝓽 */

                Assert.Equal(1, result);

                ConsumeTestYamlPact_First.MockProviderService.VerifyInteractions();
                ConsumeTestYamlPact_Second.MockProviderService.VerifyInteractions();
            }
        }
    }
}
