using UnityEngine;
using UnityEngine.UI;

public class ZombieIndicatorManager : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float showDistance = 20f;
    [SerializeField] private float edgeOffset = 50f;
    [SerializeField] private float mergeDistance = 5f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("References")]
    private Camera mainCamera;
    private Transform player;
    private RectTransform indicatorRect;
    private Image indicatorImage;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        InitializeComponents();
        SetupInitialRotation();
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;
        player = GameManager.Instance.GetPlayer();
        indicatorRect = GetComponent<RectTransform>();
        indicatorImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (!canvas)
        {
            Debug.LogError("ZombieDirectionIndicator must be child of a Canvas!");
            enabled = false;
            return;
        }
    }

    private void SetupInitialRotation()
    {
        // Устанавливаем базовый поворот индикатора
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (!player || !mainCamera) return;

        Transform nearestZombie = FindNearestZombie();
        
        if (nearestZombie == null)
        {
            FadeOut();
            return;
        }

        UpdateIndicatorVisibility(nearestZombie);
    }

    private void UpdateIndicatorVisibility(Transform zombie)
    {
        float distanceToZombie = Vector3.Distance(player.position, zombie.position);
        
        if (distanceToZombie > showDistance)
        {
            FadeOut();
            return;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(zombie.position);
        bool isInFrontOfCamera = screenPoint.z > 0;

        // Если зомби перед камерой и в пределах экрана
        if (isInFrontOfCamera && IsPointOnScreen(screenPoint))
        {
            FadeOut();
            return;
        }

        // Если зомби видим - показываем индикатор
        UpdateIndicatorPosition(zombie.position, screenPoint, isInFrontOfCamera);
        FadeIn();
    }

    private void UpdateIndicatorPosition(Vector3 zombiePosition, Vector3 screenPoint, bool isInFrontOfCamera)
    {
        // Если зомби позади камеры, корректируем screenPoint
        if (!isInFrontOfCamera)
        {
            screenPoint *= -1;
        }

        // Получаем направление от центра экрана к зомби
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = new Vector2(screenPoint.x - screenCenter.x, screenPoint.y - screenCenter.y).normalized;

        // Находим точку на краю экрана
        Vector2 indicatorPosition = GetIndicatorPositionOnScreenEdge(screenCenter, direction);

        // Конвертируем в координаты канваса
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            indicatorPosition,
            canvas.worldCamera,
            out Vector2 localPoint))
        {
            indicatorRect.anchoredPosition = localPoint;
        }

        // Обновляем поворот индикатора
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle);

        // Обновляем масштаб в зависимости от расстояния
        float distanceNormalized = Vector3.Distance(player.position, zombiePosition) / showDistance;
        float scale = Mathf.Lerp(maxScale, minScale, distanceNormalized);
        transform.localScale = Vector3.one * scale;
    }

    private Vector2 GetIndicatorPositionOnScreenEdge(Vector2 screenCenter, Vector2 direction)
    {
        // Находим пересечение с краем экрана
        float screenWidth = Screen.width - edgeOffset * 2;
        float screenHeight = Screen.height - edgeOffset * 2;
        
        float absDirectionX = Mathf.Abs(direction.x);
        float absDirectionY = Mathf.Abs(direction.y);

        Vector2 screenBounds = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);

        // Определяем, с какой стороной экрана пересекается линия направления
        float scale;
        if (absDirectionX / screenBounds.x > absDirectionY / screenBounds.y)
        {
            scale = screenBounds.x / absDirectionX;
        }
        else
        {
            scale = screenBounds.y / absDirectionY;
        }

        return screenCenter + direction * scale;
    }

    private bool IsPointOnScreen(Vector3 screenPoint)
    {
        return screenPoint.x >= 0 && screenPoint.x <= Screen.width &&
               screenPoint.y >= 0 && screenPoint.y <= Screen.height;
    }

    private void FadeIn()
    {
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
    }

    private void FadeOut()
    {
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
    }

    private Transform FindNearestZombie()
    {
        ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (ZombieAI zombie in zombies)
        {
            if (zombie == null || zombie.GetComponent<ZombieHealth>().IsDead())
                continue;

            float distance = Vector3.Distance(player.position, zombie.transform.position);
            
            if (distance < nearestDistance)
            {
                bool shouldMerge = false;
                foreach (ZombieAI otherZombie in zombies)
                {
                    if (otherZombie != zombie && !otherZombie.GetComponent<ZombieHealth>().IsDead() &&
                        Vector3.Distance(zombie.transform.position, otherZombie.transform.position) < mergeDistance)
                    {
                        shouldMerge = true;
                        break;
                    }
                }

                if (!shouldMerge)
                {
                    nearestDistance = distance;
                    nearest = zombie.transform;
                }
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !player) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, showDistance);
    }
}