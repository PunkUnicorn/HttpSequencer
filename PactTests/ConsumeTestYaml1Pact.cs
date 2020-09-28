using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Mocks.MockHttpService;
using PactNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PactTests
{
    public class ConsumeTestYaml1Pact : IDisposable
    {
        public IPactBuilder PactBuilder { get; private set; }
        public IMockProviderService MockProviderService { get; private set; }

        //public int MockServerPort { get; set; } = 7878;
        //public string MockProviderServiceBaseUri { get; set; }

        //public string MakeMockProviderAddress() => $"{MockProviderServiceBaseUri}:{MockServerPort}";

        private class ConsoleOutputter : IOutput
        {
            public void WriteLine(string line)
            {
                Console.WriteLine(line);
            }
        }

        public ConsumeTestYaml1Pact(string consumerName, int port)
        {
            //PactBuilder = new PactBuilder(new PactConfig { Outputters = new [] { new ConsoleOutputter() } }); //Defaults to specification version 1.1.0, uses default directories. PactDir: ..\..\pacts and LogDir: ..\..\logs
                                             //or
            PactBuilder = new PactBuilder( new PactConfig { Outputters = new[] { new ConsoleOutputter() }, SpecificationVersion = "2.0.0" }); //Configures the Specification Version
            //                                                                                  //or
            //PactBuilder = new PactBuilder(new PactConfig { PactDir = @"..\pacts", LogDir = @"c:\temp\logs" }); //Configures the PactDir and/or LogDir.

            PactBuilder
              .ServiceConsumer(consumerName)
              .HasPactWith("Something API");


            //needs elevated, or port opening
            MockProviderService = PactBuilder.MockService(port, false, IPAddress.Any); //Configure the http mock server
                                                                           //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, true); //By passing true as the second param, you can enabled SSL. A self signed SSL cert will be provisioned by default.
            //                                                                     //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, true, sslCert: sslCert, sslKey: sslKey); //By passing true as the second param and an sslCert and sslKey, you can enabled SSL with a custom certificate. See "Using a Custom SSL Certificate" for more details.
            //                                                                                                       //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, new JsonSerializerSettings()); //You can also change the default Json serialization settings using this overload    
            //                                                                                             //or
            //MockProviderService = PactBuilder.MockService(MockServerPort, host: IPAddress.Any); //By passing host as IPAddress.Any, the mock provider service will bind and listen on all ip addresses

        }

        public void Dispose()
        {
            PactBuilder.Build(); //NOTE: Will save the pact file once finished
        }
    }
}
