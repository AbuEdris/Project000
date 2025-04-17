using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplifiedMovementController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float groundDrag = 5f;

    [Header("Jumping")]
    public float jumpForce = 12f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.4f;
    bool readyToJump = true;
    private float jumpCooldownTimer = 0f;

    [Header("Crouching")]
    public float crouchSpeed = 3.5f;
    public float crouchYScale = 0.5f;
    private float startYScale;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 40f;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        // Process player input
        MyInput();
        SpeedControl();

        // Handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
            
        // Handle jump cooldown
        if (!readyToJump)
        {
            jumpCooldownTimer -= Time.deltaTime;
            if (jumpCooldownTimer <= 0)
            {
                readyToJump = true;
                exitingSlope = false;
            }
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        // Use direct key checks for WASD
        horizontalInput = 0;
        verticalInput = 0;

        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;
        if (Input.GetKey(KeyCode.A)) horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontalInput += 1f;

        // When to jump
        if (Input.GetKey(KeyCode.Space) && readyToJump && grounded)
        {
            readyToJump = false;
            jumpCooldownTimer = jumpCooldown;
            Jump();
        }

        // Start crouch
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Stop crouch
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // On slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        // On ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        // In air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        // Turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        // Limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        // Limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            // Limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // Reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}