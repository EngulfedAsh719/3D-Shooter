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
    [SerializeField] private float maxVerticalAngle = 45f; 
    [SerializeField] private float minVerticalAngle = -35f; 

    [Header("Aim Assist")]
    [SerializeField] private bool aimAssistEnabled = true;
    [SerializeField] private float assistRadius = 5f;
    [SerializeField] private float assistAngle = 30f;
    [SerializeField] private float assistStrength = 0.5f;
    [SerializeField] private LayerMask zombieLayer;
    [SerializeField] private float targetHeightOffset = 1.5f;

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
    private ShootingHandler shootingHandler;

    private void Start()
    {
        InitializeLaser();
        playerBody = transform.parent;
        shootingHandler = GetComponent<ShootingHandler>();
    }

    private void InitializeLaser()
    {
        GameObject laserObject = Instantiate(laserPrefab, transform.position, Quaternion.identity);
        laserObject.transform.parent = transform;
        lineRenderer = laserObject.GetComponent<LineRenderer>();
        
        if (laserFlarePrefab != null)
        {
            laserFlare = Instantiate(laserFlarePrefab, transform.position, Quaternion.identity);
        }

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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentRotationX += mouseX;
        currentRotationY = Mathf.Clamp(currentRotationY - mouseY, minVerticalAngle, maxVerticalAngle);

        Vector3 baseAimDirection = Quaternion.Euler(currentRotationY, currentRotationX, 0) * Vector3.forward;

        if (aimAssistEnabled)
        {
            Transform target = FindBestTarget(baseAimDirection);
            
            if (target != null)
            {
                if (target != currentTarget)
                {
                    currentTarget = target;
                    assistLerpFactor = 0f;
                }

                assistLerpFactor = Mathf.Min(assistLerpFactor + Time.deltaTime * 2f, 1f);

                Vector3 targetPoint = target.position + Vector3.up * targetHeightOffset;
                Vector3 directionToTarget = (targetPoint - laserStartingPoint.position).normalized;
                
                aimDirection = Vector3.Lerp(baseAimDirection, directionToTarget, 
                    assistLerpFactor * assistStrength);
            }
            else
            {
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

        aimDirection = aimDirection.normalized;
        
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0, currentRotationX, 0);
        }

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

            Vector3 targetPoint = collider.transform.position + Vector3.up * targetHeightOffset;
            Vector3 directionToTarget = (targetPoint - laserStartingPoint.position).normalized;
            float angleToTarget = Vector3.Angle(baseDirection, directionToTarget);

            if (angleToTarget <= assistAngle)
            {
                float distanceScore = 1f - (Vector3.Distance(transform.position, collider.transform.position) / assistRadius);
                float angleScore = 1f - (angleToTarget / assistAngle);
                float score = (distanceScore + angleScore) * 0.5f;

                if (collider.transform == currentTarget)
                {
                    score += 0.1f;
                }

                if (score > bestScore)
                {
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