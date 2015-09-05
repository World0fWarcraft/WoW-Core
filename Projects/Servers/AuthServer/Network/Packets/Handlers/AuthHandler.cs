﻿// Copyright (c) Multi-Emu.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using AuthServer.Attributes;
using AuthServer.Constants.Authentication;
using AuthServer.Constants.Net;
using AuthServer.Managers;
using AuthServer.Network.Sessions;
using Framework.Constants.Account;
using Framework.Cryptography.BNet;
using Framework.Misc;
using Framework.Network.Packets;

namespace AuthServer.Network.Packets.Handlers
{
    class AuthHandler
    {
        public static void SendProofRequest(Client client)
        {
            var session = client.Session;

            client.Modules = Manager.ModuleMgr.Modules.Where(m => m.System == client.OS);

            var thumbprintModule = client.Modules.SingleOrDefault(m => m.Name == "Thumbprint");
            var passwordModule = client.Modules.SingleOrDefault(m => m.Name == "Password");

            var proofRequest = new AuthPacket(AuthServerMessage.ProofRequest);

            // Send two modules (Thumbprint & Password).
            proofRequest.Write(2, 3);

            /// Thumbprint module
            Manager.ModuleMgr.WriteModuleHeader(client, proofRequest, thumbprintModule);

            // Data 
            proofRequest.Write(thumbprintModule.Data.ToByteArray());

            session.SecureRemotePassword = new SRP6a(session.Account.Salt, session.Account.Email, session.Account.PasswordVerifier);
            session.SecureRemotePassword.CalculateB();

            /// Password module
            Manager.ModuleMgr.WriteModuleHeader(client, proofRequest, passwordModule);

            // State
            proofRequest.Flush();
            proofRequest.Write(PasswordModuleState.ServerChallenge, 8);

            // Data
            proofRequest.Write(session.SecureRemotePassword.I);
            proofRequest.Write(session.SecureRemotePassword.S);
            proofRequest.Write(session.SecureRemotePassword.B);
            proofRequest.Write(session.SecureRemotePassword.S2);

            client.SendPacket(proofRequest);
        }

        [AuthMessage(AuthClientMessage.ProofResponse, AuthChannel.Authentication)]
        public static void OnProofResponse(AuthPacket packet, Client client)
        {
            var session = client.Session;

            var moduleCount = packet.Read<byte>(3);

            for (int i = 0; i < moduleCount; i++)
            {
                var dataSize = packet.Read<int>(10);
                var state = packet.Read<PasswordModuleState>(8);

                switch (state)
                {
                    case PasswordModuleState.ClientChallenge:
                        if (session.GameAccounts != null && session.GameAccount == null)
                        {
                            if (session.GameAccounts.Count > 1)
                            {
                                var region = packet.Read<Region>(8);
                                var gameLength = packet.Read<byte>(8);
                                var game = packet.ReadString(gameLength);

                                session.GameAccount = session.GameAccounts.SingleOrDefault(ga => ga.Game + ga.Index == game && ga.Region == region);

                                var riskFingerprint = new AuthPacket(AuthServerMessage.ProofRequest);

                                riskFingerprint.Write(1, 3);

                                Manager.ModuleMgr.WriteRiskFingerprint(client, riskFingerprint);

                                client.SendPacket(riskFingerprint);

                                return;
                            }
                            else
                                session.GameAccount = session.GameAccounts.FirstOrDefault();
                        }

                        if (session.GameAccount == null)
                            SendAuthComplete(true, AuthResult.NoGameAccount, client);
                        else if (!session.GameAccount.IsOnline)
                        {
                            SendAuthComplete(false, AuthResult.GlobalSuccess, client);

                            client.Session.GameAccount.IsOnline = true;
                        }

                        break;
                    case PasswordModuleState.ClientProof:
                        // Wrong password module data size
                        if (dataSize != 0x121)
                            return;

                        var a = packet.Read(0x80);
                        var m1 = packet.Read(0x20);
                        var clientChallenge = packet.Read(0x80);

                        session.SecureRemotePassword.CalculateU(a);
                        session.SecureRemotePassword.CalculateClientM(a);

                        if (session.SecureRemotePassword.ClientM.Compare(m1))
                        {
                            session.SecureRemotePassword.CalculateServerM(m1);

                            // Assign valid game accounts for the account
                            if (session.Account.GameAccounts != null)
                                session.GameAccounts = session.Account.GameAccounts.Where(ga => ga.Game.Equals(client.Game, StringComparison.OrdinalIgnoreCase)).ToList();

                            SendProofValidation(client, clientChallenge);
                        }
                        else
                            SendAuthComplete(true, AuthResult.BadLoginInformation, client);

                        break;
                    default:
                        break;
                }
            }
        }

        public static void SendProofValidation(Client client, byte[] clientChallenge)
        {
            var passwordModule = client.Modules.SingleOrDefault(m => m.Name == "Password");
            var selectedGameAccountModule = client.Modules.SingleOrDefault(m => m.Name == "SelectGameAccount");

            var proofValidation = new AuthPacket(AuthServerMessage.ProofRequest);

            var moduleCount = 2;

            proofValidation.Write(moduleCount, 3);

            /// Password module
            Manager.ModuleMgr.WriteModuleHeader(client, proofValidation, passwordModule, 161);

            // State
            proofValidation.Flush();
            proofValidation.Write(PasswordModuleState.ValidateProof, 8);

            // Data
            proofValidation.Write(client.Session.SecureRemotePassword.ServerM);
            proofValidation.Write(client.Session.SecureRemotePassword.S2);

            /// SelectGameAccount module
            if (client.Session.GameAccounts != null && client.Session.GameAccounts.Count > 1)
            {
                var gameAccountBuffer = new AuthPacket();

                gameAccountBuffer.Write(0, 8);
                gameAccountBuffer.Write(client.Session.GameAccounts.Count, 8);

                client.Session.GameAccounts.ForEach(ga =>
                {
                    gameAccountBuffer.Write(ga.Region, 8);
                    gameAccountBuffer.WriteString(ga.Game + ga.Index, 8, false);
                });

                gameAccountBuffer.Finish();

                Manager.ModuleMgr.WriteModuleHeader(client, proofValidation, selectedGameAccountModule, gameAccountBuffer.Data.Length);

                // Data
                proofValidation.Write(gameAccountBuffer.Data);
            }
            else
                Manager.ModuleMgr.WriteRiskFingerprint(client, proofValidation);

            client.SendPacket(proofValidation);
        }

        public static void SendAuthComplete(bool failed, AuthResult result, Client client)
        {
            var complete = new AuthPacket(AuthServerMessage.Complete);

            complete.Write(failed, 1);

            if (failed)
            {
                complete.Write(false, 1);       // false - disable optional modules
                complete.Write(1, 2);           // 1 - enable AuthResults
                complete.Write(result, 16);     // AuthResults (Error codes)
                complete.Write(0x80000000, 32); // Unknown
            }
            else
            {
                // No modules supported here.
                complete.Write(0, 3);

                var pingTimeout = 0x80005000;
                var hasRegulatorRules = true;

                complete.Write(pingTimeout, 32);
                complete.Write(hasRegulatorRules, 1);

                if (hasRegulatorRules)
                {
                    var hasRegulatorInfo = true;

                    complete.Write(hasRegulatorInfo, 1);

                    if (hasRegulatorInfo)
                    {
                        var threshold = 25000000;
                        var rate = 1000;

                        complete.Write(threshold, 32);
                        complete.Write(rate, 32);
                    }
                }

                var haslogonInfo = true;
                var account = client.Session.Account;
                var gameAccount = client.Session.GameAccount;

                complete.Write(!haslogonInfo, 1);

                complete.WriteString(account.GivenName, 8, false);
                complete.WriteString(account.Surname, 8, false);

                complete.Write(account.Id, 32);
                complete.Write((byte)account.Region, 8);
                complete.Write((ulong)account.Flags, 64);

                complete.Write((byte)gameAccount.Region, 8);
                complete.WriteString(gameAccount.AccountId + "#" + gameAccount.Index, 5, false, -1);
                complete.Write((ulong)gameAccount.Flags, 64);

                complete.Write(account.LoginFailures, 32);
                complete.Write(0, 8);
            }

            client.SendPacket(complete);
        }
    }
}
