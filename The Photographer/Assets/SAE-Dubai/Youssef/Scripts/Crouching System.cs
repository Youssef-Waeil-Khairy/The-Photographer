using UnityEngine;

public class CrouchingSystem : MonoBehaviour
{
    [Header("Crouching settings")]
    //when the player is standing aka normal position
    public Transform standingPoint;
    //when the player crouches to any positions the players wants
    public Transform crouchPOint;
    public float crouchSpeed = 5f;

    private Camera playerCam;
    //0 being standing and 1 is fully crouched, so like (0.1 ,0.2 ,0.9 ,1.0)
    private float crouchLevel = 0f;
    private bool isCrouching = false;

    private void Start()
    {
        playerCam = Camera.main;    
    }

    private void Update()
    {
        
    }

    void HandleCrouch()
    {
        //toogle crouch mode with left crtl
        //adjust crouching using the wheel scroll
        //the transiton between crouchign and standing
    }
}
