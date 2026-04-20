using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float lateralSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravity;

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
    [SerializeField] private float sidestepDistance = 3f;
    [SerializeField] private float sidestepDuration = 0.15f;
    [SerializeField] private float sidestepCooldown = 0.5f;

    // Gravedad / salto
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    float groundY = 1;


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
        float t = Mathf.Clamp01(sidestepTimer / sidestepDuration);

        // EaseOut: el sidestep desacelera al final
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        float newX = Mathf.Lerp(sidestepStartX, sidestepTargetX, easedT);
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

            float targetX = Mathf.Lerp(transform.position.x, worldMousePos.x, lateralSpeed * Time.deltaTime);

            transform.position = Vector3.MoveTowards(transform.position, new Vector3(targetX, transform.position.y, transform.position.z), lateralSpeed * Time.deltaTime);
        }
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        print("Le diste al espacio");
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
        print("Le diste a la A o a la D");
        if (isSidestepping || sidestepCooldownTimer > 0f) return;
        print("Hola buenos días");

        float input = ctx.ReadValue<float>();
        if (Mathf.Approximately(input, 0f)) return;

        sidestepDirection = Mathf.Sign(input);
        sidestepStartX = transform.position.x;
        sidestepTargetX = sidestepStartX + sidestepDirection * sidestepDistance;

        isSidestepping = true;
        sidestepTimer = 0f;
    }
}