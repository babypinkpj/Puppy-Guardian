using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

namespace Match
{
    public class PlayerCameraSetup : NetworkBehaviour
    {
        [Tooltip("Optional: An empty child object used as the specific camera target (e.g., at shoulder/head height). If left empty, it will follow the player's feet.")]
        public Transform cameraTarget;

        public override void OnNetworkSpawn()
        {
            // Only set up the camera if we are the local player controlling this object
            if (IsOwner)
            {
                SetupCamera();
            }
        }

        private void SetupCamera()
        {
            // Find the active Cinemachine 3.0 camera in the scene
            CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();

            if (cam != null)
            {
                Transform target = cameraTarget != null ? cameraTarget : transform;
                
                // Assign this player as the target
                cam.Follow = target;
                cam.LookAt = target;
            }
            else
            {
                Debug.LogWarning("No CinemachineCamera found in the scene! Ensure you have a Cinemachine 3.0 Camera in your Match scene.");
            }
        }
    }
}
