using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float forwardSpeed = 5f;
    [SerializeField] private float lateralSpeed = 5f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Límites laterales")]
    [SerializeField] private float maxXMovement = 10f;

    [Header("Inputs")]
    [SerializeField] private InputActionProperty jumpAction;
    [SerializeField] private InputActionProperty sidestepAction;

    private Camera mainCamera;

    private bool isSidestepping = false;
    private float sidestepTimer = 0f;
    private float sidestepCooldownTimer = 0f;
    private float sidestepDirection = 0f;
    private float sidestepStartX;
    private float sidestepTargetX;

    // Gravedad / salto
    private float verticalVelocity = 0f;
    private bool isGrounded = true;

    void OnEnable()
    {
        jumpAction.action.Enable();
        sidestepAction.action.Enable();

        jumpAction.action.performed += OnJump;
        sidestepAction.action.performed += OnSidestep;
    }

    void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
        sidestepAction.action.performed -= OnSidestep;

        jumpAction.action.Disable();
        sidestepAction.action.Disable();
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Movimiento hacia delante en Z
        transform.Translate(0f, 0f, forwardSpeed * Time.deltaTime, Space.World);

        // Solo se puede hacer el movimiento diagonal con el ratón
        // si no se está haciendo el sidestep
        if (isSidestepping)
            UpdateSidestep();
        else
            MoveLateralToMouse();

        ApplyGravity();

        if (sidestepCooldownTimer > 0f)
            sidestepCooldownTimer -= Time.deltaTime;
    }

    private void UpdateSidestep()
    {
        sidestepTimer += Time.deltaTime;
        float t = Mathf.Clamp01(sidestepTimer / dashDuration);

        // EaseOut: el dash desacelera al final
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        float newX = Mathf.Lerp(dashStartX, dashTargetX, easedT);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (t >= 1f)
        {
            isSidestepping = false;
            sidestepCooldownTimer = sidestepCooldown;
        }
    }

    private void MoveLateralToMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldMousePos = ray.GetPoint(distance);

            float targetX = Mathf.Clamp(worldMousePos.x, -maxXMovement, maxXMovement);

            float newX = Mathf.Lerp(transform.position.x, targetX, lateralSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!isGrounded) return;

        verticalVelocity = jumpForce;
        isGrounded = false;
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = 0f;

        verticalVelocity += gravity * Time.deltaTime;

        float newY = transform.position.y + verticalVelocity * Time.deltaTime;

        if (newY <= groundY)
        {
            newY = groundY;
            verticalVelocity = 0f;
            isGrounded = true;
        }

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnSidestep(InputAction.CallbackContext ctx)
    {
        if (isSidestepping || sidestepCooldownTimer > 0f) return;

        float input = ctx.ReadValue<float>();
        if (Mathf.Approximately(input, 0f)) return;

        sidestepDirection = Mathf.Sign(input);
        sidestepStartX = transform.position.x;
        sidestepTargetX = Mathf.Clamp(sidestepStartX + sidestepDirection * dashDistance, -maxXMovement, maxXMovement);

        isSidestepping = true;
        sidestepTimer = 0f;
    }
