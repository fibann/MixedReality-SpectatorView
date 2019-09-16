// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNITY_ANDROID && !UNITY_EDITOR
#   define ANDROID_DEVICE
#endif

using Microsoft.MixedReality.Sharing.Matchmaking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>Wraps a `PeerMatchmakingService` based on UDP broadcast.</summary>
    /// <remarks>
    /// Exposes the main functions and configuration options while handling platform-specific details.
    /// </remarks>
    public class MatchmakingService : MonoBehaviour
    {
        /// <summary>
        /// Configuration for the matchmaking service.
        /// </summary>
        [Serializable]
        public class Options
        {
            /// <summary>Port used to send and receive broadcast packets.</summary>
            [Tooltip("Port used to send and receive broadcast packets.")]
            public ushort BroadcastPort = 7411;

            /// <summary>
            /// Address used to send and receive broadcast packets. Should be an IP broadcast or multicast address.
            /// If null, 255.255.255.255 will be used.
            /// </summary>
            [Tooltip("Address used to send and receive broadcast packets. " +
                "Should be an IP broadcast or multicast address. If null, 255.255.255.255 will be used.")]
            public string BroadcastAddress;

            /// <summary>
            /// Local address to bind to. Can be null if the service should not bind to a specific address.
            /// </summary>
            [Tooltip("Local address to bind to. Can be null if the service should not bind to a specific address.")]
            public string LocalAddress;
        }

        /// <summary>
        /// Configuration for the matchmaking service.
        /// </summary>
        [Tooltip("Configuration for the matchmaking service.")]
        [SerializeField]
        private Options _options = new Options();

        private IMatchmakingService _mmService;

#if ANDROID_DEVICE
        /// Wrapper for discovery tasks that reference-counts a MulticastLock.
        private class DiscoveryTask : IDiscoveryTask
        {
            // Initialized on construction, set to null when object is disposed.
            private volatile IDiscoveryTask _task;

            private readonly AndroidJavaObject _mcastLock;
            private readonly List<DiscoveryTask> _discoveryTasks;

            public IEnumerable<IRoom> Rooms => _task.Rooms;

            public event Action<IDiscoveryTask> Updated
            {
                add
                {
                    _task.Updated += value;
                }

                remove
                {
                    _task.Updated -= value;
                }
            }

            public DiscoveryTask(IDiscoveryTask task, AndroidJavaObject mcastLock, List<DiscoveryTask> discoveryTasks)
            {
                _task = task;
                _mcastLock = mcastLock;
                _discoveryTasks = discoveryTasks;

                _mcastLock.Call("acquire");
                lock(discoveryTasks)
                {
                    discoveryTasks.Add(this);
                }
            }

            public void Dispose()
            {
                if (_task != null)
                {
                    lock (_discoveryTasks)
                    {
                        _discoveryTasks.Remove(this);
                    }
                    _task.Dispose();
                    _mcastLock.Call("release");
                    _task = null;
                }
            }
        }

        private AndroidJavaObject _mcastLock;
        private List<DiscoveryTask> _discoveryTasks = new List<DiscoveryTask>();
        private bool _hasRooms = false;
#endif

        // Starts the matchmaking service.
        private void OnEnable()
        {
            // Set broadcast address.
            IPAddress bcastAddress = IPAddress.Broadcast;
            if (!string.IsNullOrEmpty(_options.BroadcastAddress))
            {
                if (!IPAddress.TryParse(_options.BroadcastAddress, out bcastAddress))
                {
                    Debug.LogError($"{_options.BroadcastAddress} is not a valid address");
                    return;
                }
            }

            // Set local address.
            IPAddress localAddress = bcastAddress.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any;
            if (string.IsNullOrEmpty(_options.LocalAddress))
            {
#if UNITY_WSA
                // On UWP, sockets bound to INADDR_ANY won't be able to send messages to INADDR_BROADCAST,
                // so we need to always bind the socket to a specific address.
                var goodAddress = SocketerClient.GetLocalIPAddress();
                if (goodAddress == null)
                {
                    Debug.LogError($"Could not find a valid local address to bind");
                    return;
                }
                localAddress = IPAddress.Parse(goodAddress);
#endif
            }
            else
            {
                if (!IPAddress.TryParse(_options.LocalAddress, out localAddress))
                {
                    Debug.LogError($"{_options.LocalAddress} is not a valid local address");
                    return;
                }
            }


            Debug.Log($"Starting matchmaking service, binding to {localAddress}," +
                $" broadcasting to {bcastAddress}, on port {_options.BroadcastPort}");
            _mmService = new PeerMatchmakingService(
                new UdpPeerNetwork(bcastAddress, _options.BroadcastPort, localAddress));

#if ANDROID_DEVICE
            // Create the MulticastLock.
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
            {
                _mcastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "MMMulticastLock");
                _mcastLock.Call("setReferenceCounted", true);
            }
#endif
        }

        public Task<IRoom> CreateRoomAsync(
            string category,
            string connection,
            IReadOnlyDictionary<string, string> attributes = null,
            CancellationToken token = default)
        {
            if (_mmService == null)
            {
                Debug.LogError($"Cannot create room ({category}, {connection}): MatchmakingService is not initialized");
                return null;
            }

            Debug.Log($"Creating room ({category}, {connection})");
            return _mmService.CreateRoomAsync(category, connection, attributes, token)
                .ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        Debug.Log($"Room ({category}, {connection}) created");
#if ANDROID_DEVICE
                        if (!_hasRooms)
                        {
                            // Keep one reference if any room is created (need to listen for queries).
                            _mcastLock.Call("acquire");
                            _hasRooms = true;
                        }
#endif
                        return task.Result;
                    }
                    Debug.LogError($"Failed to create room ({category}, {connection}): {task.Exception.InnerException.Message}");
                    throw task.Exception.InnerException;
                }, token);
        }

        public IDiscoveryTask StartDiscovery(string category)
        {
            if (_mmService == null)
            {
                Debug.LogError($"Cannot start discovery of category {category}: MatchmakingService is not initialized");
                return null;
            }

            Debug.Log($"Start discovery of category {category}");
            var task = _mmService.StartDiscovery(category);
#if ANDROID_DEVICE
            return new DiscoveryTask(task, _mcastLock, _discoveryTasks);
#else
            return task;
#endif
        }


        private void OnDisable()
        {
#if ANDROID_DEVICE
            // Dispose the existing task wrappers and release references.
            DiscoveryTask[] tasks;
            lock (_discoveryTasks)
            {
                tasks = _discoveryTasks.ToArray();
                _discoveryTasks.Clear();
            }
            foreach (var task in tasks)
            {
                task.Dispose();
            }

            if (_hasRooms)
            {
                // Release the reference held for the local rooms.
                _mcastLock.Call("release");
                _hasRooms = false;
            }

            _mcastLock.Dispose();
            _mcastLock = null;
#endif
            _mmService.Dispose();
            _mmService = null;
        }
    }
}
