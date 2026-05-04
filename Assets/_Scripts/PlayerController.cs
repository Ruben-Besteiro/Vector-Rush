using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float lateralSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -20f;

    [Header("Inputs")]
    [SerializeField] private InputActionProperty jumpAction;
    [SerializeField] private InputActionProperty sidestepAction;

    private CharacterController controller;
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
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

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
        // Actualizar estado de suelo (si la gravedad es invertida, miramos hacia arriba)
        isGrounded = (gravity < 0) ? controller.isGrounded : (controller.collisionFlags & CollisionFlags.Above) != 0;

        // Si estamos en el suelo y cayendo (o subiendo si la gravedad es invertida), mantenemos una pequeña fuerza
        float groundingForce = (gravity < 0) ? -2f : 2f;
        bool isMovingTowardsGround = (gravity < 0) ? (verticalVelocity < 0) : (verticalVelocity > 0);

        if (isGrounded && isMovingTowardsGround)
        {
            verticalVelocity = groundingForce;
        }

        // 1. Movimiento hacia adelante (Z)
        float moveZ = forwardSpeed;

        // 2. Movimiento lateral (X)
        float moveX = 0f;
        if (isSidestepping)
        {
            moveX = CalculateSidestepVelocity();
        }
        else
        {
            moveX = CalculateMouseLateralVelocity();
        }

        // 3. Gravedad (Y)
        verticalVelocity += gravity * Time.deltaTime;
        float moveY = verticalVelocity;

        // Combinar y mover
        Vector3 moveVector = new Vector3(moveX, moveY, moveZ);
        controller.Move(moveVector * Time.deltaTime);

        // Cooldown del sidestep
        if (sidestepCooldownTimer > 0f)
            sidestepCooldownTimer -= Time.deltaTime;

        // Si por ejemplo te chocas contra una pared, mueres
        if (controller.velocity.z < 0.5f)
            Destroy(gameObject);
    }

    private float CalculateSidestepVelocity()
    {
        sidestepTimer += Time.deltaTime;
        float t = Mathf.Clamp01(sidestepTimer / sidestepDuration);

        // EaseOut: el sidestep desacelera al final
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        float targetX = Mathf.Lerp(sidestepStartX, sidestepTargetX, easedT);
        float deltaX = targetX - transform.position.x;

        if (t >= 1f)
        {
            isSidestepping = false;
            sidestepCooldownTimer = sidestepCooldown;
        }

        // Retornamos la velocidad necesaria para este frame
        return deltaX / Time.deltaTime;
    }

    private float CalculateMouseLateralVelocity()
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return 0f;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // Creamos un plano a la altura actual del jugador para evitar que el raycast falle si no hay suelo o está en el aire
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldMousePos = ray.GetPoint(distance);
            float targetX = Mathf.Lerp(transform.position.x, worldMousePos.x, lateralSpeed * Time.deltaTime);
            float deltaX = targetX - transform.position.x;
            
            return deltaX / Time.deltaTime;
        }
        return 0f;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!isGrounded) return;
        verticalVelocity = jumpForce;
    }

    private void OnSidestep(InputAction.CallbackContext ctx)
    {
        if (isSidestepping || sidestepCooldownTimer > 0f) return;

        float input = ctx.ReadValue<float>();
        if (Mathf.Approximately(input, 0f)) return;

        sidestepDirection = Mathf.Sign(input);
        sidestepStartX = transform.position.x;
        sidestepTargetX = sidestepStartX + sidestepDirection * sidestepDistance;

        isSidestepping = true;
        sidestepTimer = 0f;
    }

    public void InvertGravity()
    {
        gravity *= -1f;
        jumpForce *= -1f;
        
        // Se le da la vuelta a la cámara
        transform.Rotate(Vector3.forward, 180f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Death"))
        {
            Destroy(gameObject);
        }
    }
}