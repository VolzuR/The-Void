using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Stamina")]
    public float stamina = 100f;
    public float maxStamina = 200f;          // потолок (для энергетиков в будущем)
    public float regenCap = 100f;            // до какого значения стамина восстанавливается сама
    public float runCostPerSecond = 10f;
    public float recoverTime = 7f;           // секунд, чтобы восстановить с 0 до regenCap, стоя на месте
    public float walkRecoverMultiplier = 0.4f; // на ходу восстановление медленнее (0.4 = 40%)

    [Header("Audio")]
    public AudioSource breathingSource;      // тяжёлое дыхание
    public float fadeDuration = 3f;          // за сколько "секунд стоя" до конца затихать
    float startVolume = 1f;

    CharacterController cc;
    float pitch;
    bool exhausted;
    float gravity = -9.81f, velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        if (breathingSource) startVolume = breathingSource.volume;
    }

    void Update()
    {
        Look();
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

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;
        bool moving = dir.sqrMagnitude > 0.01f;

        // бег только вперёд и если не выдохся
        bool wantsRun = Input.GetKey(KeyCode.LeftShift) && v > 0 && !exhausted;
        bool running = wantsRun && moving;
        float speed = running ? runSpeed : walkSpeed;

        float standRegen = regenCap / recoverTime; // скорость восстановления стоя (ед/сек)

        if (running)
        {
            // трата стамины
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
            // восстановление: стоя быстрее, на ходу медленнее
            float rate = moving ? standRegen * walkRecoverMultiplier : standRegen;
            stamina = Mathf.Min(stamina + rate * Time.deltaTime, regenCap);
        }

        // режим "выдохся": дыхание играет, пока стамина не восстановится
        if (exhausted)
        {
            // плавное затухание звука по мере восстановления
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

        // гравитация
        velY = cc.isGrounded ? -1f : velY + gravity * Time.deltaTime;
        Vector3 move = dir * speed + Vector3.up * velY;
        cc.Move(move * Time.deltaTime);
    }
}