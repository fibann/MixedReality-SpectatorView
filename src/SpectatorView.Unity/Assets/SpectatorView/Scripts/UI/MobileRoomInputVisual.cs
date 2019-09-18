// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Dialog taking a room name to connect as input. Finds the given room and fires
    /// <see cref="NetworkConfigurationUpdated"/> passing the connection string.
    /// </summary>
    public class MobileRoomInputVisual : MonoBehaviour, INetworkConfigurationVisual
    {
        [Tooltip("The InputField used to specify a room name by the user.")]
        [SerializeField]
        private InputField _roomNameField = null;

        [Tooltip("The Button used to start a network connection by the user.")]
        [SerializeField]
        private Button _connectButton = null;

        [Tooltip("Check to enable debug logging.")]
        [SerializeField]
        private bool _debugLogging = false;

        [Tooltip("Matchmaking service used for room discovery.")]
        [SerializeField]
        private MatchmakingService _matchmakingService = null;

        private string _roomName = "SpectatorView";
        private RoomDiscovery _roomDiscovery;
        private static readonly string _roomNamePlayerPrefKey = $"{nameof(MobileRoomInputVisual)}.{nameof(_roomName)}";

        /// <summary>
        /// Matchmaking service used for room discovery.
        /// </summary>
        public MatchmakingService MatchmakingService
        {
            get => _matchmakingService;
            set => _matchmakingService = value;
        }

        public event Action<INetworkConfigurationVisual, string> NetworkConfigurationUpdated;

        public void Show()
        {
            this.gameObject.SetActive(true);
        }

        public void Hide()
        {
            this.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(OnConnectButtonClick);
            }

            // Restore the field contents.
            _roomName = PlayerPrefs.GetString(_roomNamePlayerPrefKey, _roomName);
            _roomNameField.text = _roomName;
        }

        private void OnDisable()
        {
            // Interrupts any discovery in progress.
            if (_roomDiscovery != null)
            {
                Destroy(_roomDiscovery);
                _roomDiscovery = null;
            }

            // Save the fields contents.
            PlayerPrefs.SetString(_roomNamePlayerPrefKey, _roomName);
            PlayerPrefs.Save();
        }

        private void OnConnectButtonClick()
        {
            DebugLog("Connect was pressed!");
            if (_roomNameField == null)
            {
                DebugLog("Unable to obtain room name from field.");
                return;
            }

            if (_matchmakingService == null)
            {
                DebugLog("A MatchmakingService is needed to connect to a room.");
                return;
            }

            // Starts the discovery.
            _roomName = _roomNameField.text;
            _roomDiscovery = gameObject.AddComponent<RoomDiscovery>();
            _roomDiscovery.MatchmakingService = _matchmakingService;
            _roomDiscovery.Category = "SpectatorView";
            _roomDiscovery.RoomsFound += rooms =>
            {
                // Find a room with the specified name
                var found = rooms.FirstOrDefault(room =>
                    room.Attributes.TryGetValue("name", out string name) ?
                        name == _roomName : false);
                if (found != null)
                {
                    Debug.Log($"Found room {_roomName} at {found.Connection}");
                    NetworkConfigurationUpdated?.Invoke(this, found.Connection);
                }
                Destroy(_roomDiscovery);
            };
            _roomDiscovery.StartDiscovery();
        }

        private void DebugLog(string message)
        {
            if (_debugLogging)
            {
                Debug.Log($"{nameof(MobileRoomInputVisual)}: {message}");
            }
        }
    }
}
