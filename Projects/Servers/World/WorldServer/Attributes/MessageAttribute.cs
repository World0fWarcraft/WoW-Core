﻿// Copyright (c) Multi-Emu.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Framework.Constants.Account;
using System;
using WorldServer.Constants.Net;

namespace WorldServer.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MessageAttribute : Attribute
    {
        public ClientMessage Message { get; }
        public SessionState State;

        public MessageAttribute(ClientMessage message, SessionState session)
        {
            Message = message;
            State = session;
        }
    }
}
