// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using Arctium.API.Misc;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Arctium.API
{
    public class APIServer
    {
        public static void Main(string[] args)
        {
            // Initialize the API server configuration file.
            ApiConfig.Initialize("configs/API.conf");

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Parse(ApiConfig.BindHost), ApiConfig.BindPort, listenOptions =>
                    {
                        // Disable nagle algorithm.
                        listenOptions.NoDelay = false;

                        // Enable Https if enabled in config & the given certificate exists.
                        if (ApiConfig.Tls && File.Exists(ApiConfig.TlsCertificate))
                            listenOptions.UseHttps(ApiConfig.TlsCertificate);
                    });
                })
                .Build();
        }
    }
}
