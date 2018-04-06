// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arctium.Core.Database
{
    public class DatabaseSettings
    {
        public string Host;
        public int Port;
        public string Database;
        public string User;
        public string Password;

        // Enabled pooling by default.
        public bool Pooling = true;
        public int MinPoolSize = 1;
        public int MaxPoolSize = 30;

        // Use the utf8 charset by default.
        public string CharSet = "utf8";
    }
}
