using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private Image healthBarImage;
    
    [Header("Health Bar Animation")]
    [SerializeField] private float healthChangeSpeed = 5f;
    
    private float targetHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        targetHealth = maxHealth;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (!Mathf.Approximately(currentHealth, targetHealth))
        {
            currentHealth = Mathf.MoveTowards(currentHealth, targetHealth, healthChangeSpeed * Time.deltaTime);
            UpdateHealthBar();
        }
    }

    public void TakeDamage(float damage)
    {
        targetHealth = Mathf.Max(0, targetHealth - damage);

        if (targetHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        GameManager.Instance.PlayerDied();
    }
}