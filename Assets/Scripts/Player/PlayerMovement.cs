using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class JackMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform orientation;
    public float walkSpeed = 5f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 2.5f;
    public float airMultiplier = 0.4f;
    [SerializeField] private float currentSpeed;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float jumpCooldown = 0.25f;
    public float gravityMultiplier = 1.5f;
    private bool readyToJump = true;

    [Header("State Management")]
    public MentalState currentState;
    public float mentalStability = 100f;
    public float maxMentalStability = 100f;
    public TextMeshProUGUI stateDisplay;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.4f;
    public float playerHeight = 2f;
    private bool grounded;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float horizontalInput;
    private float verticalInput;
    private bool isCrouching;

    public enum MentalState
    {
        Stable,
        Unstable,
        Dreaming,
        Psychotic
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (orientation == null)
        {
            Debug.LogWarning("Orientation not assigned - using player transform");
            orientation = transform;
        }
    }

    private void Start()
    {
        rb.freezeRotation = true;
        ResetJump();
        currentState = MentalState.Stable;
        UpdateStateDisplay();
    }

    private void Update()
    {
        GetInput();
        GroundCheck();
        SpeedControl();
        HandleDrag();
        UpdateMentalState();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl)) ToggleCrouch();
    }

    private void MovePlayer()
    {
        if (orientation == null) return;

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        float speedMultiplier = grounded ? 10f : 10f * airMultiplier;
        
        if (isCrouching)
            rb.AddForce(moveDirection.normalized * crouchSpeed * speedMultiplier, ForceMode.Force);
        else if (Input.GetKey(KeyCode.LeftShift))
            rb.AddForce(moveDirection.normalized * sprintSpeed * speedMultiplier, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * walkSpeed * speedMultiplier, ForceMode.Force);

        // Apply additional gravity
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }

    private void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + groundCheckDistance, groundLayer);
        Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + groundCheckDistance), grounded ? Color.green : Color.red);
    }

    private void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float maxSpeed = isCrouching ? crouchSpeed : 
                        Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : 
                        walkSpeed;

        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        float newHeight = isCrouching ? playerHeight * 0.5f : playerHeight;
        transform.localScale = new Vector3(transform.localScale.x, newHeight, transform.localScale.z);
    }

    private void HandleDrag()
    {
        rb.linearDamping = grounded ? 5f : 0f;
    }

    private void UpdateMentalState()
    {
        // State transitions based on stability
        if (mentalStability > 70f) currentState = MentalState.Stable;
        else if (mentalStability > 40f) currentState = MentalState.Unstable;
        else if (mentalStability > 10f) currentState = MentalState.Dreaming;
        else currentState = MentalState.Psychotic;

        // Adjust movement based on state
        switch (currentState)
        {
            case MentalState.Stable:
                walkSpeed = 5f;
                break;
            case MentalState.Unstable:
                walkSpeed = 4f;
                break;
            case MentalState.Dreaming:
                walkSpeed = 3.5f;
                airMultiplier = 0.6f;
                break;
            case MentalState.Psychotic:
                walkSpeed = 6f;
                airMultiplier = 0.2f;
                break;
        }

        UpdateStateDisplay();
    }

    private void UpdateStateDisplay()
    {
        if (stateDisplay != null)
            stateDisplay.text = $"State: {currentState}\nStability: {mentalStability:F0}%";
    }

    public void ModifyStability(float amount)
    {
        mentalStability = Mathf.Clamp(mentalStability + amount, 0f, maxMentalStability);
    }

    // Add other systems (interactions, effects, etc) below...
}