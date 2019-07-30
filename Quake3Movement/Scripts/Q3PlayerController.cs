using UnityEngine;

namespace Q3Movement
{
    // Contains the command the user wishes upon the character.
    struct Movement
    {
        public float Forward;
        public float Right;
    }

    /// <summary>
    /// This script handles all Quake III CPM(A) mod style player movement logic.
    /// First-person Mouse look is handled by the CameraControl script.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Q3PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class MovementSettings
        {
            public float MaxSpeed;
            public float Acceleration;
            public float Deceleration;

            public MovementSettings(float maxSpeed, float accel, float decel)
            {
                MaxSpeed = maxSpeed;
                Acceleration = accel;
                Deceleration = decel;
            }
        }

        [Header("Aiming")]
        [SerializeField] private Camera m_Camera;
        [SerializeField] private MouseLook m_MouseLook = new MouseLook();

        [Header("Movement")]
        [SerializeField] private float m_Friction = 6;
        [SerializeField] private float m_Gravity = 20;
        [SerializeField] private float m_JumpForce = 8;
        [Tooltip("Automatically jump when holding jump button")]
        [SerializeField] private bool m_AutoBunnyHop = false;
        [Tooltip("How precise air control is")]
        [SerializeField] private float m_AirControl = 0.3f;
        [SerializeField]
        private MovementSettings m_GroundSettings = new MovementSettings(7, 14, 10);
        [SerializeField]
        private MovementSettings m_AirSettings = new MovementSettings(7, 2, 2);
        [SerializeField]
        private MovementSettings m_StrafeSettings = new MovementSettings(1, 50, 50);

        /// <summary>
        /// Returns player's current speed.
        /// </summary>
        public float Speed { get { return m_Character.velocity.magnitude; } }

        private CharacterController m_Character;
        private Vector3 moveDirectionNorm = Vector3.zero;
        private Vector3 playerVelocity = Vector3.zero;

        // Used to queue the next jump just before hitting the ground.
        private bool wishJump = false;

        // Used to display real time friction values.
        private float playerFriction = 0;

        // Player commands, stores wish commands that the player asks for (Forward, back, jump, etc)
        private Movement InputMove;
        private Transform m_Tran;
        private Transform m_CamTran;

        private void Start()
        {
            m_Tran = transform;

            // Hide the cursor.
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            m_Character = GetComponent<CharacterController>();
            InputMove = new Movement();

            if (!m_Camera)
                m_Camera = Camera.main;

            m_CamTran = m_Camera.transform;

            m_MouseLook.Init(m_Tran, m_CamTran);
        }

        private void Update()
        {
            m_MouseLook.UpdateCursorLock();

            QueueJump();

            // Handle player movement.
            if (m_Character.isGrounded)
            {
                GroundMove();
            }
            else
            {
                AirMove();
            }

            // Rotate the character and camera.
            m_MouseLook.LookRotation(m_Tran, m_CamTran);

            // Move the character.
            m_Character.Move(playerVelocity * Time.deltaTime);
        }

        /*******************************************************************************************************\
        |* MOVEMENT
        \*******************************************************************************************************/

        // Sets the movement direction based on player input.
        private void SetMovementDir()
        {
            InputMove.Forward = Input.GetAxisRaw("Vertical");
            InputMove.Right = Input.GetAxisRaw("Horizontal");
        }

        // Queues the next jump.
        private void QueueJump()
        {
            if (m_AutoBunnyHop)
            {
                wishJump = Input.GetButton("Jump");
                return;
            }

            if (Input.GetButtonDown("Jump") && !wishJump)
            {
                wishJump = true;
            }

            if (Input.GetButtonUp("Jump"))
            {
                wishJump = false;
            }
        }

        // Handle air movement.
        private void AirMove()
        {
            Vector3 wishdir;
            float wishvel = m_AirSettings.Acceleration;
            float accel;

            SetMovementDir();

            wishdir = new Vector3(InputMove.Right, 0, InputMove.Forward);
            wishdir = m_Tran.TransformDirection(wishdir);

            float wishspeed = wishdir.magnitude;
            wishspeed *= m_AirSettings.MaxSpeed;

            wishdir.Normalize();
            moveDirectionNorm = wishdir;

            // CPM Air control.
            float wishspeed2 = wishspeed;
            if (Vector3.Dot(playerVelocity, wishdir) < 0)
            {
                accel = m_AirSettings.Deceleration;
            }
            else
            {
                accel = m_AirSettings.Acceleration;
            }

            // If the player is ONLY strafing left or right
            if (InputMove.Forward == 0 && InputMove.Right != 0)
            {
                if (wishspeed > m_StrafeSettings.MaxSpeed)
                {
                    wishspeed = m_StrafeSettings.MaxSpeed;
                }

                accel = m_StrafeSettings.Acceleration;
            }

            Accelerate(wishdir, wishspeed, accel);
            if (m_AirControl > 0)
            {
                AirControl(wishdir, wishspeed2);
            }

            // Apply gravity
            playerVelocity.y -= m_Gravity * Time.deltaTime;
        }

        // Air control occurs when the player is in the air, it allows players to move side 
        // to side much faster rather than being 'sluggish' when it comes to cornering.
        private void AirControl(Vector3 wishdir, float wishspeed)
        {
            float zspeed;
            float speed;
            float dot;
            float k;

            // Only control air movement when moving forwards or backward.
            if (Mathf.Abs(InputMove.Forward) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
            {
                return;
            }

            zspeed = playerVelocity.y;
            playerVelocity.y = 0;
            /* Next two lines are equivalent to idTech's VectorNormalize() */
            speed = playerVelocity.magnitude;
            playerVelocity.Normalize();

            dot = Vector3.Dot(playerVelocity, wishdir);
            k = 32;
            k *= m_AirControl * dot * dot * Time.deltaTime;

            // Change direction while slowing down.
            if (dot > 0)
            {
                playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
                playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
                playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

                playerVelocity.Normalize();
                moveDirectionNorm = playerVelocity;
            }

            playerVelocity.x *= speed;
            playerVelocity.y = zspeed; // Note this line
            playerVelocity.z *= speed;
        }

        // Handle ground movement.
        private void GroundMove()
        {
            Vector3 wishdir;

            // Do not apply friction if the player is queueing up the next jump
            if (!wishJump)
            {
                ApplyFriction(1.0f);
            }
            else
            {
                ApplyFriction(0);
            }

            SetMovementDir();

            wishdir = new Vector3(InputMove.Right, 0, InputMove.Forward);
            wishdir = m_Tran.TransformDirection(wishdir);
            wishdir.Normalize();
            moveDirectionNorm = wishdir;

            var wishspeed = wishdir.magnitude;
            wishspeed *= m_GroundSettings.MaxSpeed;

            Accelerate(wishdir, wishspeed, m_GroundSettings.Acceleration);

            // Reset the gravity velocity
            playerVelocity.y = -m_Gravity * Time.deltaTime;

            if (wishJump)
            {
                playerVelocity.y = m_JumpForce;
                wishJump = false;
            }
        }

        private void ApplyFriction(float t)
        {
            Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
            float speed;
            float newspeed;
            float control;
            float drop;

            vec.y = 0;
            speed = vec.magnitude;
            drop = 0;

            // Only apply friction when grounded.
            if (m_Character.isGrounded)
            {
                control = speed < m_GroundSettings.Deceleration ? m_GroundSettings.Deceleration : speed;
                drop = control * m_Friction * Time.deltaTime * t;
            }

            newspeed = speed - drop;
            playerFriction = newspeed;
            if (newspeed < 0)
            {
                newspeed = 0;
            }

            if (speed > 0)
            {
                newspeed /= speed;
            }

            playerVelocity.x *= newspeed;
            // playerVelocity.y *= newspeed;
            playerVelocity.z *= newspeed;
        }

        // Calculates wish acceleration based on player's cmd wishes.
        private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
        {
            float addspeed;
            float accelspeed;
            float currentspeed;

            currentspeed = Vector3.Dot(playerVelocity, wishdir);
            addspeed = wishspeed - currentspeed;
            if (addspeed <= 0)
            {
                return;
            }

            accelspeed = accel * Time.deltaTime * wishspeed;
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            playerVelocity.x += accelspeed * wishdir.x;
            playerVelocity.z += accelspeed * wishdir.z;
        }
    }
}