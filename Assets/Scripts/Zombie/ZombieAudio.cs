using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(ZombieHealth))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private float minIdleInterval = 8f;
    [SerializeField] private float maxIdleInterval = 15f;
    [SerializeField] private float playerDetectionRange = 15f;
    [SerializeField] private float increasedChanceRange = 8f;
    [SerializeField] private float idleProbabilityInRange = 0.7f;

    private ZombieAI zombieAI;
    private ZombieHealth zombieHealth;
    private Transform player;
    private float nextIdleTime;
    private bool isDead;
    private float lastDistanceToPlayer;

    private void Start()
    {
        InitializeComponents();
        SubscribeToEvents();
        SetNextIdleTime();
    }

    private void InitializeComponents()
    {
        zombieAI = GetComponent<ZombieAI>();
        zombieHealth = GetComponent<ZombieHealth>();
        player = GameManager.Instance.GetPlayer();

        if (zombieAI == null || zombieHealth == null || player == null)
        {
            Debug.LogError($"Missing required components on {gameObject.name}!");
            enabled = false;
        }
    }

    private void SubscribeToEvents()
    {
        if (zombieHealth != null)
        {
            zombieHealth.OnDeath += HandleDeath;
            zombieHealth.OnDamageReceived += HandleDamageReceived;
        }

        if (zombieAI != null)
        {
            zombieAI.OnAttackPerformed += HandleAttack;
        }
    }

    private void Update()
    {
        if (isDead || GameManager.Instance.isGamePaused) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Проверяем, подошел ли зомби ближе к игроку
        bool movedCloser = distanceToPlayer < lastDistanceToPlayer;
        lastDistanceToPlayer = distanceToPlayer;

        if (Time.time >= nextIdleTime && IsPlayerInRange())
        {
            bool shouldTryPlay = false;

            // Увеличиваем шанс воя, если зомби приближается к игроку
            if (distanceToPlayer <= increasedChanceRange && movedCloser)
            {
                shouldTryPlay = Random.value < idleProbabilityInRange;
            }
            // Обычный шанс воя для зомби в пределах слышимости
            else if (distanceToPlayer <= playerDetectionRange)
            {
                shouldTryPlay = Random.value < 0.3f;
            }

            if (shouldTryPlay)
            {
                TryPlayIdleSound();
            }
            else
            {
                // Если звук не проигрался, установим следующее время попытки
                SetNextIdleTime(true);
            }
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayZombieDeath(transform.position);
        }
        UnsubscribeFromEvents();
    }

    private void HandleDamageReceived()
    {
        // При получении урона сбрасываем таймер воя
        SetNextIdleTime();
    }

    private void HandleAttack()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayZombieAttack(transform.position);
        }
        // После атаки тоже сбрасываем таймер воя
        SetNextIdleTime();
    }

    private void TryPlayIdleSound()
    {
        if (AudioManager.Instance != null && 
            AudioManager.Instance.TryPlayZombieIdle(transform.position))
        {
            SetNextIdleTime();
        }
    }

    private void SetNextIdleTime(bool shortInterval = false)
    {
        float minInterval = shortInterval ? minIdleInterval * 0.5f : minIdleInterval;
        float maxInterval = shortInterval ? maxIdleInterval * 0.5f : maxIdleInterval;
        nextIdleTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= playerDetectionRange;
    }

    private void UnsubscribeFromEvents()
    {
        if (zombieHealth != null)
        {
            zombieHealth.OnDeath -= HandleDeath;
            zombieHealth.OnDamageReceived -= HandleDamageReceived;
        }

        if (zombieAI != null)
        {
            zombieAI.OnAttackPerformed -= HandleAttack;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Показываем общий радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        
        // Показываем радиус повышенного шанса воя
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, increasedChanceRange);
    }
#endif
}