using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float forwardSpeed = 7f;
    [SerializeField] private float lateralSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -20f;

    [Header("Inputs")]
    [SerializeField] private InputActionProperty jumpAction;
    [SerializeField] private InputActionProperty sidestepAction;
    [SerializeField] private InputActionProperty dashAction;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float dashStartZ;
    private float dashTargetZ;

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

    [SerializeField] private float maxPhysicalLateralVelocity;


    private Vector3 startPos;

    // Gravedad / salto
    private float verticalVelocity = 0f;
    private bool isGrounded;
    private float defaultGravity;
    private float defaultJumpForce;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        defaultGravity = gravity;
        defaultJumpForce = jumpForce;
    }

    void OnEnable()
    {
        jumpAction.action.Enable();
        sidestepAction.action.Enable();
        dashAction.action.Enable();

        jumpAction.action.performed += OnJump;
        sidestepAction.action.performed += OnSidestep;
        dashAction.action.performed += OnDash;
    }

    void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
        sidestepAction.action.performed -= OnSidestep;
        dashAction.action.performed -= OnDash;

        jumpAction.action.Disable();
        sidestepAction.action.Disable();
        dashAction.action.Disable();
    }

    void Start()
    {
        mainCamera = Camera.main;
        startPos = transform.position;
    }

    void Update()
    {
        // Actualizar estado de suelo (si la gravedad es invertida, miramos hacia arriba)
        isGrounded = (gravity < 0) ? controller.isGrounded : (controller.collisionFlags & CollisionFlags.Above) != 0;

        // Si estamos en el suelo y cayendo (o subiendo si la gravedad es invertida), mantenemos una pequeña fuerza
        float groundingForce = (gravity < 0) ? -2f : 2f;
        bool isMovingTowardsGround = (gravity < 0) ? (verticalVelocity < 0) : (verticalVelocity > 0);

        if (isGrounded && isMovingTowardsGround)
            verticalVelocity = groundingForce;

        // 1. Movimiento hacia adelante (Z)
        float moveZ = forwardSpeed;
        if (isDashing)
            moveZ = CalculateDashVelocity();

        // 2. Movimiento lateral (X)
        float moveX = 0f;
        if (isSidestepping)
            moveX = CalculateSidestepVelocity();
        else
            moveX = CalculateMouseLateralVelocity();

        // 3. Gravedad (Y)
        verticalVelocity += gravity * Time.deltaTime;
        float moveY = verticalVelocity;

        // Combinar y mover
        Vector3 moveVector = new Vector3(moveX, moveY, moveZ);
        controller.Move(moveVector * Time.deltaTime);

        // Cooldown del sidestep
        if (sidestepCooldownTimer > 0f)
            sidestepCooldownTimer -= Time.deltaTime;

        // Cooldown del dash
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
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

    private float CalculateDashVelocity()
    {
        dashTimer += Time.deltaTime;
        float t = Mathf.Clamp01(dashTimer / dashDuration);

        // EaseOut: el dash desacelera al final
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        float targetZ = Mathf.Lerp(dashStartZ, dashTargetZ, easedT);
        float deltaZ = targetZ - transform.position.z;

        if (t >= 1f)
        {
            isDashing = false;
            dashCooldownTimer = dashCooldown;
            // Al terminar, devolvemos al menos la velocidad normal para evitar morir por el check de velocidad
            return Mathf.Max(deltaZ / Time.deltaTime, forwardSpeed);
        }

        return deltaZ / Time.deltaTime;
    }

    private float CalculateMouseLateralVelocity()
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return 0f;
        }

        // Movimiento lateral basado en la posición del ratón


        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // Creamos un plano a la altura actual del jugador para evitar que el raycast falle si no hay suelo o está en el aire
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldMousePos = ray.GetPoint(distance);
            float targetX = Mathf.Lerp(transform.position.x, worldMousePos.x, lateralSpeed * Time.deltaTime);

            float deltaX = targetX - transform.position.x;
            
            // Calculamos la velocidad y la limitamos para tratar de evitar que se salga
            float velocityX = deltaX / Time.deltaTime;
            velocityX = Mathf.Clamp(velocityX, -maxPhysicalLateralVelocity, maxPhysicalLateralVelocity);
            
            return velocityX;
        }
        return 0f;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        print("Saltando");
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

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        verticalVelocity = 0f;

        dashStartZ = transform.position.z;
        dashTargetZ = dashStartZ + dashDistance;

        isDashing = true;
        dashTimer = 0f;
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
        if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
            isGrounded = true;

        if (hit.gameObject.CompareTag("GG"))
        {
            controller.enabled = false;
            GameManager.Instance.GG();
        }

        // Choque frontal (paredes solo)
        if (hit.normal.z < -0.7f && controller.velocity.z < 0.1f)
            if (isDashing && hit.gameObject.CompareTag("DestructibleBlock"))
                Destroy(hit.gameObject);
            else
                Die();
        
        if (hit.gameObject.CompareTag("Death"))
            Die();
    }

    private void Die()
    {
        transform.position = startPos;
        isDashing = false;
        isSidestepping = false;
        verticalVelocity = 0f;
        foreach (Coin coin in GameManager.Instance.coinList)
            coin.gameObject.SetActive(true);
        GameManager.Instance.coinsCollected = 0;

        
        if (gravity != defaultGravity) InvertGravity();
        MusicManager.Instance.RestartMusic();

        GameManager.Instance.IncreaseDeaths();
    }
}