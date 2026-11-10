using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Jump")]
    public float jumpHeight = 1f;

    [Header("Crouch")]
    public float standHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public float standCameraY = 1.6f;
    public float crouchCameraY = 0.8f;
    public float crouchTransitionSpeed = 8f;

    [Header("Stamina")]
    public float stamina = 100f;
    public float maxStamina = 200f;
    public float regenCap = 100f;
    public float runCostPerSecond = 10f;
    public float recoverTime = 7f;
    public float walkRecoverMultiplier = 0.4f;

    [Header("Audio")]
    public AudioSource breathingSource;
    public float fadeDuration = 3f;
    float startVolume = 1f;

    CharacterController cc;
    float pitch;
    bool exhausted;
    bool crouching;
    float gravity = -9.81f, velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        cc.height = standHeight;
        cc.center = new Vector3(0, standHeight / 2f, 0);

        if (breathingSource)
        {
            startVolume = breathingSource.volume;
            breathingSource.Stop();
        }
    }

    void Update()
    {
        Look();
        HandleCrouch();
        Move();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
        transform.Rotate(Vector3.up * mx);
    }

    bool CanStandUp()
    {
        // проверяем, нет ли над головой препятствия
        float extra = standHeight - cc.height;
        if (extra <= 0.01f) return true;
        Vector3 origin = transform.position + Vector3.up * (cc.height - cc.radius);
        return !Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.up, out _,
            extra, ~0, QueryTriggerInteraction.Ignore);
    }

    void HandleCrouch()
    {
        bool wantsCrouch = Input.GetKey(KeyCode.LeftControl);

        if (wantsCrouch) crouching = true;
        else if (crouching && CanStandUp()) crouching = false;

        float targetHeight = crouching ? crouchHeight : standHeight;
        float targetCamY = crouching ? crouchCameraY : standCameraY;

        cc.height = Mathf.MoveTowards(cc.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        cc.center = new Vector3(0, cc.height / 2f, 0);

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = Mathf.MoveTowards(camPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
        cameraTransform.localPosition = camPos;
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        bool moving = dir.sqrMagnitude > 0.01f;

        // бег: только вперёд, не в приседе и не выдохшись
        bool wantsRun = Input.GetKey(KeyCode.LeftShift) && v > 0 && !exhausted && !crouching;
        bool running = wantsRun && moving;

        float speed = crouching ? crouchSpeed : (running ? runSpeed : walkSpeed);

        float standRegen = regenCap / recoverTime;

        if (running)
        {
            stamina -= runCostPerSecond * Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;
                if (breathingSource)
                {
                    breathingSource.volume = startVolume;
                    breathingSource.Play();
                }
            }
        }
        else if (stamina < regenCap)
        {
            float rate = moving ? standRegen * walkRecoverMultiplier : standRegen;
            stamina = Mathf.Min(stamina + rate * Time.deltaTime, regenCap);
        }

        if (exhausted)
        {
            float fadeRange = standRegen * fadeDuration;
            if (breathingSource && fadeRange > 0f)
            {
                breathingSource.volume = startVolume * Mathf.Clamp01((regenCap - stamina) / fadeRange);
            }

            if (stamina >= regenCap)
            {
                exhausted = false;
                if (breathingSource)
                {
                    breathingSource.Stop();
                    breathingSource.volume = startVolume;
                }
            }
        }

        // гравитация и прыжок
        if (cc.isGrounded && velY < 0f)
        {
            velY = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !crouching)
                velY = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velY += gravity * Time.deltaTime;

        Vector3 move = dir * speed + Vector3.up * velY;
        cc.Move(move * Time.deltaTime);
    }
}