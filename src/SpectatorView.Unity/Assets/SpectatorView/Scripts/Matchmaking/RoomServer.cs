// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Sharing.Matchmaking;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.SpectatorView.Scripts.Matchmaking
{
    public class RoomServer : MonoBehaviour
    {
        public string LocalIPAddress;

        private IMatchmakingService _mmService;

        private void Start()
        {
            string roomName = NetworkConfigurationSettings.IsInitialized ? NetworkConfigurationSettings.Instance.RoomName : null;
            if (roomName.Length > 0)
            {
                string localAddressStr = LocalIPAddress.Length > 0 ? LocalIPAddress : SocketerClient.GetLocalIPAddress();

                if (IPAddress.TryParse(NetworkConfigurationSettings.Instance.BroadcastIPAddress, out IPAddress broadcastAddr) &&
                    IPAddress.TryParse(localAddressStr, out IPAddress localAddr))
                {
                    var mode = NetworkConfigurationSettings.Instance.JoinMulticastGroup ?
                        UdpPeerNetwork.JoinMulticastGroup.Join : UdpPeerNetwork.JoinMulticastGroup.DoNotJoin;
                    _mmService = new PeerMatchmakingService(
                        new UdpPeerNetwork(new IPEndPoint(broadcastAddr, NetworkConfigurationSettings.Instance.BroadcastPort),
                        new IPEndPoint(localAddr, NetworkConfigurationSettings.Instance.BroadcastPort),
                        mode));
                    Debug.Log($"Creating room {roomName}");
                    Debug.Log($"Multicasting to {broadcastAddr}");
                    _mmService.CreateRoomAsync(roomName, localAddressStr)
                        .ContinueWith(task =>
                        {
                            if (task.IsCompleted)
                            {
                                Debug.Log($"Room {roomName} created");
                            }
                            else
                            {
                                Debug.LogError($"Could not create room {roomName}");
                            }
                        });
                }
                else
                {
                    Debug.LogError($"Invalid IP address: {NetworkConfigurationSettings.Instance.BroadcastIPAddress}, {localAddressStr}");
                }

            }
        }

        private void Stop()
        {
            _mmService.Dispose();
            _mmService = null;
        }
    }
}
