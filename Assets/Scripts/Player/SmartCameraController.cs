using UnityEngine;

public class SmartCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(1f, 2f, 0f);
    
    [Header("Camera Settings")]
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 5f;
    
    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.15f;
    [SerializeField] private float positionSmoothTime = 0.1f;
    
    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.2f;

    [Header("Aim Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimFOV = 45f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float aimDistance = 2.5f; // Дистанция камеры при прицеливании
    
    private Camera mainCamera;
    private Vector3 currentVelocity;
    private Vector3 currentRotationVelocity;
    private LaserAim laserAim;
    private float targetDistance;
    private float currentFOV;
    private float targetFOV;
    private bool isAiming;
    
    private void Start()
    {
        InitializeComponents();
        SetupInitialValues();
    }

    private void InitializeComponents()
    {
        if (target == null && GameManager.Instance != null)
        {
            target = GameManager.Instance.GetPlayer();
        }

        if (target != null)
        {
            laserAim = target.GetComponent<LaserAim>();
        }

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void SetupInitialValues()
    {
        currentFOV = normalFOV;
        targetFOV = normalFOV;
        targetDistance = distance;
        
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = normalFOV;
        }
    }

    private void Update()
    {
        HandleAiming();
    }

    private void HandleAiming()
    {
        bool wasAiming = isAiming;
        isAiming = Input.GetMouseButton(1); // ПКМ для прицеливания

        // Устанавливаем целевые значения
        targetFOV = isAiming ? aimFOV : normalFOV;
        targetDistance = isAiming ? aimDistance : distance;

        // Плавно меняем FOV
        if (mainCamera != null)
        {
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
            mainCamera.fieldOfView = currentFOV;
        }
    }

    private void LateUpdate()
    {
        if (target == null || GameManager.Instance.isGamePaused)
            return;

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        // Получаем желаемую позицию камеры относительно игрока
        Vector3 targetPosition = target.position + offset;

        // Получаем направление лазера
        Vector3 laserDirection = laserAim != null ? laserAim.GetAimDirection() : target.forward;

        // Вычисляем желаемую позицию камеры с учетом текущей целевой дистанции
        Vector3 desiredPosition = targetPosition - laserDirection * targetDistance;

        // Проверяем коллизии
        RaycastHit hit;
        Vector3 directionToCamera = (desiredPosition - targetPosition).normalized;
        float actualDistance = targetDistance;

        if (Physics.SphereCast(targetPosition, collisionRadius, directionToCamera, out hit, 
            targetDistance, collisionMask))
        {
            actualDistance = Mathf.Clamp(hit.distance, minDistance, targetDistance);
            desiredPosition = targetPosition + directionToCamera * actualDistance;
        }

        // Плавно перемещаем камеру
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, 
            ref currentVelocity, positionSmoothTime);

        // Плавно поворачиваем камеру в направлении лазера
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            Time.deltaTime / rotationSmoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position + offset, collisionRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(target.position + offset, transform.position);
    }

    // Публичные методы для внешнего доступа
    public bool IsAiming() => isAiming;
    
    public float GetCurrentFOV() => currentFOV;
}