﻿// Copyright (c) Multi-Emu.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Framework.Misc;
using Framework.Network.Packets;
using Framework.Objects;
using System;
using World.Shared.Game.Entities.Object.Guid;
using WorldServer.Constants.Net;
using WorldServer.Packets.Structures.Account;

namespace WorldServer.Packets.Server.Misc
{
    class UpdateAccountData : ServerPacket
    {
        public PlayerGuid PlayerGuid { get; set; }
        public UInt32 Time { get; set; }
        //public UInt32 DecompressedSize { get; set; }
        public DataType Type { get; set; }
        //public UInt32 CompressedSize { get; set; }
        public string Data { get; set; }

        public UpdateAccountData() : base(ServerMessage.UpdateAccountData) { }

        public override void Write()
        {
            Packet.Write(PlayerGuid);
            Packet.Write(Helper.GetUnixTime());

        }
    }
}
