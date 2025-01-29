using UnityEngine;
using System;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    
    [Header("Components")]
    [SerializeField] private ZombieHealthBar healthBar;

    private int currentHealth;
    private Animator animator;
    private ZombieAI zombieAI;
    private bool isDead;

    public event Action OnDeath;
    public event Action OnDamageReceived;

    private void Start()
    {
        InitializeComponents();
        InitializeHealth();
    }

    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        zombieAI = GetComponent<ZombieAI>();

        if (animator == null)
            Debug.LogError($"Animator component missing on {gameObject.name}!");
            
        if (zombieAI == null)
            Debug.LogError($"ZombieAI component missing on {gameObject.name}!");
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (healthBar != null)
        {
            healthBar.SetupHealthBar(maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }

        OnDamageReceived?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        DisableComponents();
        PlayDeathAnimation();
        OnDeath?.Invoke();

        if (zombieAI != null)
        {
            zombieAI.OnDeath();
        }

        Destroy(gameObject, 2f);
    }

    private void DisableComponents()
    {
        // Отключаем все коллайдеры
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
        }

        // Скрываем полосу здоровья
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    private void PlayDeathAnimation()
    {
        if (animator != null)
        {
            int randomDeathAnimation = UnityEngine.Random.Range(1, 3);
            animator.SetInteger("Die", randomDeathAnimation);
        }
    }

    // Публичные методы для получения состояния
    public bool IsDead() => isDead;
    
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    
    public int GetCurrentHealth() => currentHealth;
    
    public int GetMaxHealth() => maxHealth;
}