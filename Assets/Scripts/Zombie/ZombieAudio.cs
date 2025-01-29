using UnityEngine;

[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(ZombieHealth))]
public class ZombieAudio : MonoBehaviour
{
    [Header("Idle Sound Settings")]
    [SerializeField] private float minIdleInterval = 5f;
    [SerializeField] private float maxIdleInterval = 15f;
    [SerializeField] private float playerDetectionRange = 15f;

    private ZombieAI zombieAI;
    private ZombieHealth zombieHealth;
    private Transform player;
    private float nextIdleTime;
    private bool isDead;

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

        if (Time.time >= nextIdleTime && IsPlayerInRange())
        {
            TryPlayIdleSound();
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
        SetNextIdleTime(); // Откладываем следующий idle звук
    }

    private void HandleAttack()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayZombieAttack(transform.position);
        }
        SetNextIdleTime(); // Откладываем следующий idle звук
    }

    private void TryPlayIdleSound()
    {
        if (AudioManager.Instance != null && 
            AudioManager.Instance.TryPlayZombieIdle(transform.position))
        {
            SetNextIdleTime();
        }
    }

    private void SetNextIdleTime()
    {
        nextIdleTime = Time.time + Random.Range(minIdleInterval, maxIdleInterval);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
    }
#endif
}