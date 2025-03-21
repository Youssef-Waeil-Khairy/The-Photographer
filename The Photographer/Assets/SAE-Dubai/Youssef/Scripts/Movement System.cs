using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    //Adjustments
    [Header("Movemnt Settings")]

    public float walkSpeed = 5f;
    public float runningSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = 10f;

    [Header("Moused Setings")]
    public float mouseSens = 2f;
    public float maxAngleRotation = 80f;
    public float minAngleRotation = -80f;

    private Rigidbody rb;
    private Transform playerCamera;
    private float rotationX = 0f;

    //refrecne crouching script , to acces the same camera
    private CrouchingSystem crouchingSystem;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = Camera.main?.transform;

        //For mouse pointer to disapper
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //to prevent roatations
        rb.freezeRotation = true;

        //refrence
        crouchingSystem = GetComponent<CrouchingSystem>();
    }

    void Update()
    {
        Move();
        MouseLook();

        if (playerCamera != null)
        {
            Debug.Log("Camera Position: " + playerCamera.position);
        }
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        //Calculate movement direction
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        //Detrmine speed
        float speed = Input.GetKey(KeyCode.LeftShift) ? runningSpeed : walkSpeed;

        //if crouching then reduce the speed by 50
        if (crouchingSystem != null && crouchingSystem.IsCrouching) 
        {
            speed *= 0.5f;
        }


        rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

        if(Input.GetKey(KeyCode.Space) && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSens;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSens;

        //Adjust vericla roattion 
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minAngleRotation,maxAngleRotation);

        playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    //checks ground
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

}
