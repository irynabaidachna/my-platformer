
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField, Range(1f, 20f)]
    private float maxSpeed = 8f;

    [SerializeField, Range(5f, 200f)]
    private float acceleration = 70f;

    [SerializeField, Range(5f, 200f)]
    private float deceleration = 90f;

    private Rigidbody2D body;
    private PlayerControls controls;
    private float moveInput;

    [Header("Вигляд")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

  

    [Header("Земля")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Range(0.05f, 0.5f)]
    private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded;


    [Header("Прийоми тюнінгу")]
    [SerializeField, Range(0f, 0.4f)]
    private float coyoteTime = 0.12f;

    private float coyoteCounter;


    [SerializeField, Range(0f, 0.4f)]
    private float jumpBufferTime = 0.15f;

    private float jumpBufferCounter;


    [Header("Стрибок")]
    [SerializeField, Range(0.5f, 8f)]
    private float jumpHeight = 3f;

    [SerializeField, Range(0.5f, 8f)]
    private float baseGravity = 3f;

    [SerializeField, Range(1f, 6f)]
    private float fallMultiplier = 2.2f;

    [SerializeField, Range(1f, 8f)]
    private float lowJumpMultiplier = 3.5f;


    [Header("Зворотний зв'язок")]
    [SerializeField, Range(1f, 20f)]
    private float hardLandingSpeed = 9f;

    [SerializeField] private CameraShake cameraShake;

    private bool wasGrounded;
    private float fallSpeed;


    [Header("Звук")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] footsteps;

    [SerializeField, Range(0f, 0.3f)]
    private float pitchVariance = 0.12f;

    [SerializeField, Range(0.1f, 0.8f)]
    private float stepInterval = 0.32f;

    private float stepTimer;




    private float JumpVelocity =>
        Mathf.Sqrt(
            2f * Mathf.Abs(Physics2D.gravity.y)
            * baseGravity * jumpHeight
        );

    private void Jump()
    {
        body.gravityScale = baseGravity;

        body.linearVelocity = new Vector2(
            body.linearVelocity.x,
            JumpVelocity
        );
    }


    private bool CheckGround()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Application.isPlaying && isGrounded
            ? Color.green
            : Color.red;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }


    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void OnDestroy()
    {
        controls.Dispose();
    }


    private void Update()
    {
        isGrounded = CheckGround();
        CheckLanding();

        // Coyote Time
        bool canRefill =
            isGrounded && body.linearVelocity.y <= 0.01f;

        coyoteCounter = canRefill
            ? coyoteTime
            : coyoteCounter - Time.deltaTime;

        // Jump Buffer
        jumpBufferCounter =
            controls.Player.Jump.WasPressedThisFrame()
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        // Стрибок, коли обидва таймери активні
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Горизонтальний рух
        moveInput =
            controls.Player.Move.ReadValue<Vector2>().x;
        UpdateVisuals();
        UpdateFootsteps();
    }

    private void FixedUpdate()
    {
        // Горизонтальний рух
        float target = moveInput * maxSpeed;

        float rate = Mathf.Abs(target) > 0.01f
            ? acceleration
            : deceleration;

        float x = Mathf.MoveTowards(
            body.linearVelocity.x,
            target,
            rate * Time.fixedDeltaTime
        );

        body.linearVelocity = new Vector2(
            x,
            body.linearVelocity.y
        );

        // Змінна висота стрибка
        if (body.linearVelocity.y < 0f)
        {
            body.gravityScale = baseGravity * fallMultiplier;
        }
        else if (body.linearVelocity.y > 0f &&
                 !controls.Player.Jump.IsPressed())
        {
            body.gravityScale = baseGravity * lowJumpMultiplier;
        }
        else
        {
            body.gravityScale = baseGravity;
        }
    }

    private void UpdateVisuals()
    {
        // Анімація залежить від реальної швидкості
        animator.SetFloat(
            SpeedHash,
            Mathf.Abs(body.linearVelocity.x)
        );

        // Повертаємо героя в напрямку руху
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }
    }


    private void CheckLanding()
    {
        // Запам'ятовуємо найбільшу швидкість падіння
        if (!isGrounded)
        {
            fallSpeed = Mathf.Max(
                fallSpeed,
                -body.linearVelocity.y
            );
        }

        // Виявляємо момент приземлення
        if (isGrounded && !wasGrounded)
        {
            OnLanded(fallSpeed);
        }

        if (isGrounded)
            fallSpeed = 0f;

        wasGrounded = isGrounded;
    }

    private void OnLanded(float speed)
    {
        // Легкі приземлення ігноруємо
        if (speed < hardLandingSpeed)
            return;

        if (cameraShake != null)
            cameraShake.Shake(0.12f, 0.12f);
    }

    private void PlayFootstep()
    {
        if (footsteps == null || footsteps.Length == 0)
            return;

        // Випадково вибираємо один із трьох звуків
        AudioClip clip = footsteps[
            Random.Range(0, footsteps.Length)
        ];

        // Додаємо випадкову зміну висоти тону
        source.pitch = 1f + Random.Range(
            -pitchVariance,
            pitchVariance
        );

        source.PlayOneShot(clip);
    }

    private void UpdateFootsteps()
    {
        // Звук тільки під час руху по землі
        bool running = isGrounded &&
            Mathf.Abs(body.linearVelocity.x) > 0.5f;

        if (!running)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = stepInterval;
        }
    }

}
