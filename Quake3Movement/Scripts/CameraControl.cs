using UnityEngine;

namespace Q3Movement
{
    /// <summary>
    /// This script must be attached to a Camera that is a child of the player GameObject.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] private Vector2 m_MouseSensitivity = new Vector2(30, 30);

        private Transform m_Tran;
        private Transform m_PlayerTran;

        // Camera rotations
        private float rotX = 0.0f;
        private float rotY = 0.0f;

        private void Start()
        {
            // Get Camera transform.
            m_Tran = GetComponent<Transform>();
            // Get parent transform.
            m_PlayerTran = m_Tran.root;
           
            // Hide the cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            // Ensure that the cursor is locked.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            // Camera rotation
            rotX -= Input.GetAxisRaw("Mouse Y") * m_MouseSensitivity.y * 0.02f;
            rotY += Input.GetAxisRaw("Mouse X") * m_MouseSensitivity.x * 0.02f;

            // Clamp the X rotation
            if (rotX < -90)
            {
                rotX = -90;
            }
            else if (rotX > 90)
            {
                rotX = 90;
            }

            // Rotate player.
            m_PlayerTran.rotation = Quaternion.Euler(0, rotY, 0);

            // Rotate camera.
            m_Tran.rotation = Quaternion.Euler(rotX, rotY, 0);
        }
    }
}
