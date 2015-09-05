﻿// Copyright (c) Multi-Emu.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CharacterServer.Constants.Net;
using CharacterServer.Packets;
using Framework.Constants.Account;
using Framework.Database.Auth.Entities;
using Framework.Logging;
using Framework.Logging.IO;
using Framework.Misc;
using Framework.Network;
using Framework.Network.Packets;
using ClientPacket = Framework.Packets.Client.Authentication;
using ServerPacket = Framework.Packets.Server.Authentication;

namespace CharacterServer.Network
{
    class CharacterSession : SessionBase
    {
        public Realm Realm { get; set; }
        public Account Account { get; set; }
        public GameAccount GameAccount { get; set; }
        public uint Challenge { get; private set; }

        public CharacterSession(Socket clientSocket) : base(clientSocket) { }

        public override async void OnConnection(object sender, SocketAsyncEventArgs e)
        {
            var recievedBytes = e.BytesTransferred;

            if (recievedBytes == 0x32 && !isTransferInitiated[1])
            {
                var clientToServer = "WORLD OF WARCRAFT CONNECTION - CLIENT TO SERVER\0";
                var transferInit = new ClientPacket.TransferInitiate { Packet = new Packet(dataBuffer, 2) } as ClientPacket.TransferInitiate;

                transferInit.Read();

                if (transferInit.Msg == clientToServer)
                {
                    State = SessionState.Initiated;

                    isTransferInitiated[1] = true;

                    e.Completed -= OnConnection;
                    e.Completed += Process;

                    Log.Debug($"Initial packet transfer for Client '{GetClientInfo()}' successfully initialized.");

                    client.ReceiveAsync(e);

                    // Assign server challenge for auth digest calculations
                    Challenge = BitConverter.ToUInt32(new byte[0].GenerateRandomKey(4), 0);

                    await Send(new ServerPacket.AuthChallenge { Challenge = Challenge });
                }
                else
                {
                    Log.Debug($"Wrong initial packet transfer data for Client '{GetClientInfo()}'.");

                    Dispose();
                }
            }
            else
                Dispose();
        }

        public override async void Process(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                var socket = e.UserToken as Socket;
                var recievedBytes = e.BytesTransferred;

                if (recievedBytes != 0)
                {
                    if (Crypt != null && Crypt.IsInitialized)
                    {
                        while (recievedBytes > 0)
                        {
                            Decrypt(dataBuffer);

                            var length = BitConverter.ToUInt16(dataBuffer, 0) + 4;
                            var packetData = new byte[length];

                            Buffer.BlockCopy(dataBuffer, 0, packetData, 0, length);

                            var packet = new Packet(dataBuffer, 4);

                            if (length > recievedBytes)
                                packetQueue.Enqueue(packet);

                            await ProcessPacket(packet);

                            recievedBytes -= length;

                            Buffer.BlockCopy(dataBuffer, length, dataBuffer, 0, recievedBytes);
                        }
                    }
                    else
                    {
                        var packet = new Packet(dataBuffer);

                        await ProcessPacket(packet);
                    }

                    client.ReceiveAsync(e);
                }
            }
            catch (Exception ex)
            {
                Dispose();

                ExceptionLog.Write(ex);

                Log.Error(ex.ToString());
            }
        }

        public override async Task ProcessPacket(Packet packet)
        {
            if (packetQueue.Count > 0)
                packetQueue.TryDequeue(out packet);

            PacketLog.Write<ClientMessage>(packet.Header.Message, packet.Data, client.RemoteEndPoint);

            await PacketManager.InvokeHandler<ClientMessage>(packet, this);
        }

        public override async Task Send(Framework.Network.Packets.ServerPacket packet)
        {
            try
            {
                packet.Write();
                packet.Packet.Finish();

                if (packet.Packet.Header != null)
                {
                    if (packet.Packet.Header.Size > 0x100)
                        packet = await Compress(packet);

                    PacketLog.Write<ServerMessage>(packet.Packet.Header.Message, packet.Packet.Data, client.RemoteEndPoint);
                }

                if (Crypt != null && Crypt.IsInitialized)
                    Encrypt(packet.Packet);

                var socketEventargs = new SocketAsyncEventArgs();

                socketEventargs.SetBuffer(packet.Packet.Data, 0, packet.Packet.Data.Length);

                socketEventargs.Completed += SendCompleted;
                socketEventargs.UserToken = packet;
                socketEventargs.RemoteEndPoint = client.RemoteEndPoint;
                socketEventargs.SocketFlags = SocketFlags.None;

                client.SendAsync(socketEventargs);
            }
            catch (Exception ex)
            {
                Dispose();

                ExceptionLog.Write(ex);

                Log.Error(ex.ToString());
            }
        }

        void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
        }
    }
}
