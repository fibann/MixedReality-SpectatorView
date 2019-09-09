// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Sharing.Matchmaking;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using UnityEngine;

namespace Assets.SpectatorView.Scripts.Matchmaking
{
    public class RoomClient : MonoBehaviour
    {
        private IMatchmakingService _mmService;

#if UNITY_ANDROID
        private AndroidJavaObject _mcLock;
#endif
        public event EventHandler<string> OnIpDiscovered;

        private void Start()
        {
            string roomName = NetworkConfigurationSettings.IsInitialized ? NetworkConfigurationSettings.Instance.RoomName : null;
            if (roomName != null)
            {
                if (IPAddress.TryParse(NetworkConfigurationSettings.Instance.BroadcastIPAddress, out IPAddress broadcastAddr))
                {
                    var mode = NetworkConfigurationSettings.Instance.JoinMulticastGroup ?
                        UdpPeerNetwork.JoinMulticastGroup.Join : UdpPeerNetwork.JoinMulticastGroup.DoNotJoin;
                    _mmService = new PeerMatchmakingService(
                        new UdpPeerNetwork(new IPEndPoint(broadcastAddr, NetworkConfigurationSettings.Instance.BroadcastPort),
                        new IPEndPoint(IPAddress.Any /* TODO IPv6 */, NetworkConfigurationSettings.Instance.BroadcastPort), mode));
                    Debug.Log($"Searching for room {roomName}");
                    Debug.Log($"Listening on {broadcastAddr}");

                    AcquireAndroidMulticastLock();

                    var discovery = _mmService.StartDiscovery(roomName);
                    discovery.Updated +=
                        (disc) =>
                        {
                            Debug.Log($"Rooms updated");
                            var found = disc.Rooms.FirstOrDefault();
                            if (found != null)
                            {
                                Debug.Log($"Found room {roomName}");
                                OnIpDiscovered?.Invoke(this, found.Connection);
                                disc.Dispose();
                                ReleaseAndroidMulticastLockOnMainThread();
                            }
                        };
                }
            }
        }

        private void Stop()
        {
            _mmService.Dispose();
            _mmService = null;
            ReleaseAndroidMulticastLock();
        }

        private void AcquireAndroidMulticastLock()
        {
#if UNITY_ANDROID
            if (_mcLock == null)
            {
                using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
                using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    _mcLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "MMMulticastLock");
                    _mcLock.Call("acquire");
                }
            }
#endif
        }

        private void ReleaseAndroidMulticastLock()
        {
#if UNITY_ANDROID
            if (_mcLock != null)
            {
                _mcLock.Call("release");
                _mcLock.Dispose();
                _mcLock = null;
            }
#endif
        }

        private IEnumerator ReleaseAndroidMulticastLockCoroutine()
        {
            yield return null;
            ReleaseAndroidMulticastLock();
        }

        private void ReleaseAndroidMulticastLockOnMainThread()
        {
#if UNITY_ANDROID
            StartCoroutine(ReleaseAndroidMulticastLockCoroutine());
#endif
        }
    }
}
