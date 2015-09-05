﻿// Copyright (c) Multi-Emu.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WorldServer.Constants.Net
{
    // Value '0x2000' means not updated/implemented
    public enum ClientMessage : ushort
    {
        #region Account
        RequestAccountData              = 0x09F9,
        #endregion

        #region ChatEntity
        #endregion

        #region Global
        #endregion

        #region UserClient
        QueuedMessagesEnd               = 0x0130,

        UITimeRequest                   = 0x0339,
        #endregion
    }
}
