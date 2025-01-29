using UnityEngine;
using System.Collections.Generic;

public class LaserAim : MonoBehaviour
{
    [Header("Laser Settings")]
    public GameObject laserPrefab;
    public Transform laserStartingPoint;
    public GameObject laserFlarePrefab;
    public float maxDistance = 50f;
    public float mouseSensitivity = 2f;
    public int damage = 20;
    public LayerMask hitLayer;

    [Header("Rotation Limits")]
    [SerializeField] private float maxVerticalAngle = 60f; // Максимальный угол поворота вверх/вниз
    [SerializeField] private float minVerticalAngle = -60f; // Минимальный угол поворота вверх/вниз

    [Header("Aim Assist")]
    [SerializeField] private bool aimAssistEnabled = true;
    [SerializeField] private float assistRadius = 5f;
    [SerializeField] private float assistAngle = 30f;
    [SerializeField] private float assistStrength = 0.5f;
    [SerializeField] private LayerMask zombieLayer;
    [SerializeField] private float targetHeightOffset = 1.5f; // Высота прицеливания (примерно уровень груди)

    private GameObject laserFlare;
    private LineRenderer lineRenderer;
    private RaycastHit lastHit;
    private List<GameObject> activeEffects = new List<GameObject>();
    private Vector3 aimDirection;
    private float currentRotationX;
    private float currentRotationY;
    private Transform currentTarget;
    private float assistLerpFactor;
    private Transform playerBody;

    private void Start()
    {
        InitializeLaser();
        playerBody = transform.parent; // Предполагаем, что LaserAim находится на дочернем объекте игрока
    }

    private void InitializeLaser()
    {
        // Создаем лазер
        GameObject laserObject = Instantiate(laserPrefab, transform.position, Quaternion.identity);
        laserObject.transform.parent = transform;
        lineRenderer = laserObject.GetComponent<LineRenderer>();
        
        // Создаем вспышку лазера
        if (laserFlarePrefab != null)
        {
            laserFlare = Instantiate(laserFlarePrefab, transform.position, Quaternion.identity);
        }

        // Инициализируем направление прицеливания
        aimDirection = transform.forward;
    }

    private void Update()
    {
        HandleAiming();
        UpdateLaser();
        CleanupEffects();
    }

    private void HandleAiming()
    {
        // Получаем входные данные мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Обновляем углы поворота
        currentRotationX += mouseX;
        currentRotationY = Mathf.Clamp(currentRotationY - mouseY, minVerticalAngle, maxVerticalAngle);

        // Получаем базовое направление прицеливания
        Vector3 baseAimDirection = Quaternion.Euler(currentRotationY, currentRotationX, 0) * Vector3.forward;

        if (aimAssistEnabled)
        {
            // Ищем цель для автонаведения
            Transform target = FindBestTarget(baseAimDirection);
            
            if (target != null)
            {
                // Если нашли новую цель
                if (target != currentTarget)
                {
                    currentTarget = target;
                    assistLerpFactor = 0f;
                }

                // Увеличиваем влияние автонаведения
                assistLerpFactor = Mathf.Min(assistLerpFactor + Time.deltaTime * 2f, 1f);

                // Вычисляем точку прицеливания (на уровне груди)
                Vector3 targetPoint = target.position + Vector3.up * targetHeightOffset;
                Vector3 directionToTarget = (targetPoint - laserStartingPoint.position).normalized;
                
                // Плавно смешиваем базовое направление и направление к цели
                aimDirection = Vector3.Lerp(baseAimDirection, directionToTarget, 
                    assistLerpFactor * assistStrength);
            }
            else
            {
                // Если цель потеряна, плавно возвращаемся к базовому направлению
                currentTarget = null;
                assistLerpFactor = Mathf.Max(assistLerpFactor - Time.deltaTime * 2f, 0f);
                aimDirection = Vector3.Lerp(aimDirection, baseAimDirection, 
                    Time.deltaTime * 10f);
            }
        }
        else
        {
            aimDirection = baseAimDirection;
        }

        // Нормализуем направление
        aimDirection = aimDirection.normalized;
        
        // Поворачиваем тело игрока по горизонтали
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0, currentRotationX, 0);
        }

        // Обновляем поворот лазера
        transform.rotation = Quaternion.LookRotation(aimDirection);
    }

    private Transform FindBestTarget(Vector3 baseDirection)
    {
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, assistRadius, zombieLayer);
        Transform bestTarget = null;
        float bestScore = float.MinValue;

        foreach (Collider collider in potentialTargets)
        {
            if (!collider.TryGetComponent<ZombieHealth>(out var zombieHealth) || zombieHealth.IsDead())
                continue;

            // Вычисляем точку прицеливания на уровне груди
            Vector3 targetPoint = collider.transform.position + Vector3.up * targetHeightOffset;
            Vector3 directionToTarget = (targetPoint - laserStartingPoint.position).normalized;
            float angleToTarget = Vector3.Angle(baseDirection, directionToTarget);

            if (angleToTarget <= assistAngle)
            {
                // Вычисляем оценку цели на основе угла и расстояния
                float distanceScore = 1f - (Vector3.Distance(transform.position, collider.transform.position) / assistRadius);
                float angleScore = 1f - (angleToTarget / assistAngle);
                float score = (distanceScore + angleScore) * 0.5f;

                // Добавляем небольшой бонус текущей цели для стабильности
                if (collider.transform == currentTarget)
                {
                    score += 0.1f;
                }

                if (score > bestScore)
                {
                    // Проверяем видимость цели
                    if (!Physics.Linecast(laserStartingPoint.position, targetPoint, 
                        hitLayer & ~zombieLayer))
                    {
                        bestScore = score;
                        bestTarget = collider.transform;
                    }
                }
            }
        }

        return bestTarget;
    }

    private void UpdateLaser()
    {
        if (lineRenderer == null) return;

        lineRenderer.SetPosition(0, laserStartingPoint.position);
        
        Ray ray = new Ray(laserStartingPoint.position, aimDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayer))
        {
            lastHit = hit;
            lineRenderer.SetPosition(1, hit.point);
            if (laserFlare != null)
            {
                laserFlare.transform.position = hit.point;
                laserFlare.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        else
        {
            Vector3 endPoint = ray.GetPoint(maxDistance);
            lineRenderer.SetPosition(1, endPoint);
            if (laserFlare != null)
            {
                laserFlare.transform.position = endPoint;
                laserFlare.transform.rotation = Quaternion.LookRotation(-ray.direction);
            }
            lastHit = default;
        }
    }

    public Vector3 GetAimDirection()
    {
        return aimDirection;
    }

    public void ShootLaser()
    {
        if (lastHit.collider != null)
        {
            ZombieHealth zombieHealth = lastHit.collider.GetComponent<ZombieHealth>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);

                if (laserFlarePrefab != null)
                {
                    GameObject effect = Instantiate(laserFlarePrefab, lastHit.point, 
                        Quaternion.LookRotation(lastHit.normal));
                    activeEffects.Add(effect);
                    Destroy(effect, 0.1f);
                }
            }
        }
    }

    private void CleanupEffects()
    {
        activeEffects.RemoveAll(effect => effect == null);
        
        while (activeEffects.Count > 10)
        {
            if (activeEffects[0] != null)
            {
                Destroy(activeEffects[0]);
            }
            activeEffects.RemoveAt(0);
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject effect in activeEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeEffects.Clear();
    }
}