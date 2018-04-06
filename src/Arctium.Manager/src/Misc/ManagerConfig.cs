// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arctium.Core.Configuration;
using Arctium.Core.Logging;

namespace Arctium.Manager.Misc
{
    public class ManagerConfig : ConfigBase<ManagerConfig>
    {
        [ConfigEntry("Log.Level", LogTypes.All)]
        public static LogTypes LogLevel;

        [ConfigEntry("Log.Directory", "logs/manager")]
        public static string LogDirectory;

        [ConfigEntry("Log.Console.File", "Console.log")]
        public static string LogConsoleFile;
    }
}
