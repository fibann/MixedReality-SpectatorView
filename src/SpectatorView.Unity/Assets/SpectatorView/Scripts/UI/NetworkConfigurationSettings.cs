// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class NetworkConfigurationSettings : Singleton<NetworkConfigurationSettings>
    {
        [Tooltip("Specify a custom prefab containing an INetworkConnectionManager")]
        [SerializeField]
        private GameObject overrideConnectionManagerPrefab = null;
        
        [Tooltip("Check to enable a mobile network configuration visual to obtain the connection information.")]
        [SerializeField]
        private bool enableMobileNetworkConfigurationVisual = true;

        [Tooltip("Prefab for creating a mobile network configuration visual, which replaces the defaultMobileNetworkConfigurationVisualPrefab on the SpectatorView component if set.")]
        [SerializeField]
        private GameObject overrideMobileNetworkConfigurationVisualPrefab = null;

        [Tooltip("Enable automatic user discovery. If false, the User IP Address in the SpectatorView component will be used.")]
        [SerializeField]
        private bool enableMatchmaking = true;

        [Tooltip("Replaces the default Room Name on the SpectatorView component if not empty.")]
        [SerializeField]
        private string overrideRoomName = "";

        [Tooltip("Check to override the options on the MatchmakingService component.")]
        [SerializeField]
        private bool overrideMatchmakingOptions = false;

        [Tooltip("Replace the default options on the MatchmakingService component if 'Override Matchmaking Options' is set.")]
        [SerializeField]
        private MatchmakingService.Options matchmakingOptionsOverrideValues = new MatchmakingService.Options();

        /// <summary>
        /// Prefab for creating an INetworkConnectionManager.
        /// </summary>
        public GameObject OverrideConnectionManagerPrefab => overrideConnectionManagerPrefab;

        /// <summary>
        /// When true, a mobile network configuration visual is used to obtain the user IP Address.
        /// </summary>
        public bool EnableMobileNetworkConfigurationVisual => enableMobileNetworkConfigurationVisual;

        /// <summary>
        /// Prefab for creating a mobile network configuration visual.
        /// </summary>
        public GameObject OverrideMobileNetworkConfigurationVisualPrefab => overrideMobileNetworkConfigurationVisualPrefab;

        /// <summary>
        /// Enable automatic user discovery. If false, the User IP Address in the SpectatorView component will be used.
        /// </summary>
        public bool EnableMatchmaking => enableMatchmaking;

        /// <summary>
        /// Replaces the default Room Name on the SpectatorView component if not empty.
        /// </summary>
        public string OverrideRoomName => overrideRoomName;

        /// <summary>
        /// Check to override the options on the MatchmakingService component.
        /// </summary>
        public bool OverrideMatchmakingOptions => overrideMatchmakingOptions;

        /// <summary>
        /// Replace the default options on the MatchmakingService component if 'Override Matchmaking Options' is set.
        /// </summary>
        public MatchmakingService.Options MatchmakingOptionsOverrideValues => matchmakingOptionsOverrideValues;
    }
}
