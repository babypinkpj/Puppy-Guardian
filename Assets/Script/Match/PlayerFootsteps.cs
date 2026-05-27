using UnityEngine;

namespace Match
{
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Audio Source")]
        public AudioSource footstepSource;

        [Header("Clips")]
        public AudioClip[] concreteClips;
        public AudioClip[] grassClips;

        [Header("Settings")]
        public float stepIntervalWalk = 0.5f;
        public float stepIntervalSprint = 0.3f;
        
        [Range(0f, 1f)]
        public float dogVolume = 0.3f; // Quiet
        
        [Range(0f, 1f)]
        public float robberVolume = 1.0f; // Normal

        private MatchPlayer _player;
        private CharacterController _controller;
        private float _stepTimer;

        private void Awake()
        {
            _player = GetComponent<MatchPlayer>();
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_player == null || _controller == null || footstepSource == null) return;

            // Check if player is moving
            if (_controller.velocity.magnitude > 0.1f && _controller.isGrounded)
            {
                // Determine step interval based on speed (simple threshold)
                float currentInterval = (_controller.velocity.magnitude > _player.baseSpeed) ? stepIntervalSprint : stepIntervalWalk;

                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    PlayFootstep();
                    _stepTimer = currentInterval;
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        private void PlayFootstep()
        {
            bool isRobber = _player.IsRobber.Value;

            if (isRobber)
            {
                RobberController robber = _player as RobberController;
                if (robber != null && robber.IsMuteShoeActive)
                {
                    return; // Mute shoe cancels footsteps
                }
            }

            AudioClip[] clipsToPlay = concreteClips;

            // Raycast to check ground material
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
            {
                // We avoid CompareTag completely because Unity prints an error to the console even if we try-catch it!
                bool isGrass = hit.collider.gameObject.name.ToLower().Contains("grass");

                if (isGrass)
                {
                    clipsToPlay = grassClips;
                }
                else
                {
                    clipsToPlay = concreteClips; // Default to concrete
                }
            }

            if (clipsToPlay != null && clipsToPlay.Length > 0)
            {
                AudioClip clip = clipsToPlay[Random.Range(0, clipsToPlay.Length)];
                footstepSource.volume = isRobber ? robberVolume : dogVolume;
                footstepSource.PlayOneShot(clip);
            }
        }
    }
}
