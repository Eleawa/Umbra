using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController2D controller;

    [Header("Input Settings")]
    [SerializeField] private bool useNewInputSystem = true;

    private Vector2 moveInput;
    private bool sprintInput;
    private bool dodgeInputPressed;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController2D>();
    }

    private void Update()
    {
        HandleInput();

        // Handle dodge input (triggered once per press)
        if (dodgeInputPressed)
        {
            controller.Dodge();
            dodgeInputPressed = false;
        }
    }

    private void FixedUpdate()
    {
        // Send movement input to controller
        controller.Move(moveInput, sprintInput);
    }

    public void HandleInput()
    {
        if (useNewInputSystem)
        {
            HandleNewInputSystem();
        }
        else
        {
            HandleLegacyInput();
        }
    }

    private void HandleNewInputSystem()
    {
        if (Keyboard.current == null)
            return;

        // Movement input
        Vector2 movement = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) movement.x = -1;
        if (Keyboard.current.dKey.isPressed) movement.x = 1;
        if (Keyboard.current.wKey.isPressed) movement.y = 1;
        if (Keyboard.current.sKey.isPressed) movement.y = -1;
        
        moveInput = movement.normalized;

        // Sprint input (hold Shift)
        sprintInput = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        // Dodge input (press Space)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            dodgeInputPressed = true;
        }
    }

    private void HandleLegacyInput()
    {
        // Movement input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;

        // Sprint input
        sprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Dodge input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dodgeInputPressed = true;
        }
    }

    // Public method to get movement input (useful for animations)
    public Vector2 GetMovementInput() => moveInput;
    public bool IsSprinting() => sprintInput;
}