using UnityEngine;

public class CrouchingSystem : MonoBehaviour
{
    [Header("Crouching settings")]
    //when the player is standing aka normal position
    public Transform standingPoint;
    //when the player crouches to any positions the players wants
    public Transform crouchPoint;
    public float crouchSpeed = 5f;
    
    
    // * Leo: I added this to stop clipping.
    [Tooltip("Maximum crouch level (0-1) to prevent ground clipping")]
    [Range(0f, 1f)]
    public float maxCrouchLevel = 0.8f;
    
    private Transform playerCam;
    //0 being standing and 1 is fully crouched, so like (0.1 ,0.2 ,0.9 ,1.0)
    private float crouchLevel = 0.2f;
    private bool isCrouching = false;

    //for movemnt script to acces it
    public bool IsCrouching => isCrouching;
    

    private void Start()
    {
        playerCam = Camera.main?.transform;  
    }

    private void Update()
    {
        HandleCrouch();
    }

    void HandleCrouch()
    {
        //toogle crouch mode with left crtl
        if (Input.GetKey(KeyCode.LeftControl)) 
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        //adjust crouching using the wheel scroll
        if(isCrouching) 
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            crouchLevel = Mathf.Clamp(crouchLevel + scroll, 0f, maxCrouchLevel);
            
        }

        //the transiton between crouchign and standing
        playerCam.localPosition = Vector3.Lerp(standingPoint.position, crouchPoint.position, crouchLevel);
    }
}
