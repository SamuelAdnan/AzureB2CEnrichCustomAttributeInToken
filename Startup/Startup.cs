using System;
using System.Collections.Generic;
using System.Text;
using B2CCustomPolicy.Helpers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

[assembly: FunctionsStartup(typeof(B2CCustomPolicy.Startup))]


namespace B2CCustomPolicy
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();


            //builder.Services.AddSingleton<IMyService>((s) =>
            //{
            //    return new MyService();
            //});

            //builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();

            
          

            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(Configsettings.ClientId)
                .WithTenantId(Configsettings.TenantId)
                .WithClientSecret(Configsettings.ClientSecret)
                .Build();

            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);

            //you can use a single client instance for the lifetime of the application
            builder.Services.AddSingleton<GraphServiceClient>(sp => {
                return new GraphServiceClient(authenticationProvider);
            });

        }
    }
}


//https://stackoverflow.com/questions/66530370/how-to-use-di-with-microsoft-graph
///https://stackoverflow.com/questions/66530370/how-to-use-di-with-microsoft-graph