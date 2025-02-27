namespace Unity.Netcode
{
    internal struct ConnectionRequestMessage : INetworkMessage
    {
        public ulong ConfigHash;

        public byte[] ConnectionData;

        public bool ShouldSendConnectionData;

        public void Serialize(FastBufferWriter writer)
        {
            if (ShouldSendConnectionData)
            {
                writer.WriteValueSafe(ConfigHash);
                writer.WriteValueSafe(ConnectionData);
            }
            else
            {
                writer.WriteValueSafe(ConfigHash);
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsServer)
            {
                return false;
            }

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(ConfigHash) +
                                         FastBufferWriter.GetWriteSize<int>()))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request message given config - possible {nameof(NetworkConfig)} mismatch.");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }
                reader.ReadValue(out ConfigHash);

                if (!networkManager.NetworkConfig.CompareConfig(ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }

                reader.ReadValueSafe(out ConnectionData);
            }
            else
            {
                if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(ConfigHash)))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Incomplete connection request ");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }
                reader.ReadValue(out ConfigHash);

                if (!networkManager.NetworkConfig.CompareConfig(ConfigHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    networkManager.DisconnectClient(context.SenderId);
                    return false;
                }
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var senderId = context.SenderId;

            if (networkManager.PendingClients.TryGetValue(senderId, out PendingClient client))
            {
                // Set to pending approval to prevent future connection requests from being approved
                client.ConnectionState = PendingClient.State.PendingApproval;
            }

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                // Note: Delegate creation allocates.
                // Note: ToArray() also allocates. :(
                networkManager.InvokeConnectionApproval(ConnectionData, senderId,
                    (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                    {
                        var localCreatePlayerObject = createPlayerObject;
                        networkManager.HandleApproval(senderId, localCreatePlayerObject, playerPrefabHash, approved,
                            position, rotation);
                    });
            }
            else
            {
                networkManager.HandleApproval(senderId, networkManager.NetworkConfig.PlayerPrefab != null, null, true, null, null);
            }
        }
    }
}
