// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arctium.Core.Configuration;
using Arctium.Core.Database;
using Lappa.ORM.Constants;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Arctium.API.Misc
{
    public class ApiConfig : ConfigBase<ApiConfig>
    {
        [ConfigEntry("API.Bind.Host", "")]
        public static string BindHost;

        [ConfigEntry("API.Bind.Port", 5543)]
        public static int BindPort;

        [ConfigEntry("API.Auth", false)]
        public static bool Auth;

        [ConfigEntry("API.Auth.Type", "")]
        public static string AuthType;

        [ConfigEntry("API.Protocol", HttpProtocols.Http2)]
        public static HttpProtocols Protocol;

        [ConfigEntry("API.Tls", false)]
        public static bool Tls;

        [ConfigEntry("API.Tls.Certificate", "")]
        public static string TlsCertificate;

        [ConfigEntry("Database.Type", DatabaseType.MySql)]
        public static DatabaseType DatabaseType;

        [ConfigEntry("Database.Aurora", default(DatabaseSettings))]
        public static DatabaseSettings AuroraDatabase;
    }
}
