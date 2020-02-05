﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Sharing.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
     /// Dialog showing the active rooms on the local network. When a room is selected and Connect is
     /// pressed, finds the given room and fires <see cref="NetworkConfigurationUpdated"/> passing the connection string.
     /// </summary>
    public class MobileRoomSelectionVisual : MonoBehaviour, INetworkConfigurationVisual
    {
        [Tooltip("The Dropdown used to show available rooms.")]
        [SerializeField]
        private Dropdown _roomNameField = null;

        [Tooltip("The Button used to start a network connection by the user.")]
        [SerializeField]
        private Button _connectButton = null;

        [Tooltip("Check to enable debug logging.")]
        [SerializeField]
        private bool _debugLogging = false;

        [Tooltip("Matchmaking service used for room discovery.")]
        [SerializeField]
        private MatchmakingService _matchmakingService = null;

        //private string _roomName = "";
        private IDiscoveryResource[] _currentRooms = Array.Empty<IDiscoveryResource>();
        private RoomDiscovery _discovery;

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
            if (!_discovery)
            {
                // Start discovery now.
                StartDiscovery();
            }
        }

        public void Hide()
        {
            this.gameObject.SetActive(false);
        }

        private void StartDiscovery()
        {
            _discovery = gameObject.AddComponent<RoomDiscovery>();
            _discovery.MatchmakingService = _matchmakingService;
            _discovery.Category = SpectatorView.MatchmakingUsersCategory;
            _discovery.RoomsFound += rooms =>
            {
                int previouslySelectedIndex = _roomNameField.value;
                IDiscoveryResource previouslySelectedRoom = previouslySelectedIndex < _currentRooms.Length ? _currentRooms[previouslySelectedIndex] : null;

                // Sort by name so the order is stable.
                _currentRooms = rooms.Where(r => r.Attributes.ContainsKey("name")).OrderBy(r => r.Attributes["name"]).ToArray();
                _roomNameField.ClearOptions();
                _roomNameField.AddOptions(_currentRooms.Select(r => r.Attributes["name"]).ToList());

                // Select the option which was previously selected if still there.
                int newIndex = previouslySelectedRoom != null ?
                    Array.FindIndex(_currentRooms, r => r.UniqueId == previouslySelectedRoom.UniqueId) : -1;
                _roomNameField.value = newIndex >= 0 ? newIndex : 0;
            };
            _discovery.StartDiscovery();
        }

        private void StopDiscovery()
        {
            if (_discovery != null)
            {
                Destroy(_discovery);
                _discovery = null;
            }
        }

        private void OnEnable()
        {
            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(OnConnectButtonClick);
            }

            _roomNameField.ClearOptions();

            // Start discovery here if the service is set, otherwise delay until Show() is called.
            if (_matchmakingService)
            {
                StartDiscovery();
            }
        }

        private void OnDisable()
        {
            StopDiscovery();
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

            if (_roomNameField.options.Any())
            {
                var room = _currentRooms.ElementAt(_roomNameField.value);
                Debug.Assert(_roomNameField.options[_roomNameField.value].text == room.Attributes["name"]);

                DebugLog($"Connecting to room {room.Attributes["name"]}");

                NetworkConfigurationUpdated?.Invoke(this, room.Connection);
                StopDiscovery();
            }
            else
            {
                DebugLog("No rooms to connect to");
            }
        }

        private void DebugLog(string message)
        {
            if (_debugLogging)
            {
                Debug.Log($"{nameof(MobileRoomSelectionVisual)}: {message}");
            }
        }
    }
}
