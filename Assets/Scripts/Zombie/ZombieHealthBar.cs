using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Canvas healthBarCanvas;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float healthChangeSpeed = 2f; // Скорость изменения полоски здоровья
    
    private Camera mainCamera;
    private float currentDisplayedHealth; // Текущее отображаемое значение
    private float targetHealth; // Целевое значение здоровья
    private float maxHealth;
    private float hideTimer;
    private bool shouldBeVisible;
    private Coroutine healthUpdateCoroutine;

    private void Start()
    {
        mainCamera = Camera.main;
        healthBarCanvas.worldCamera = mainCamera;
        SetHealthBarVisible(false);
    }

    private void Update()
    {
        // Поворот к камере
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        // Проверка таймера видимости
        if (shouldBeVisible)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                SetHealthBarVisible(false);
                shouldBeVisible = false;
            }
        }
    }

    public void SetupHealthBar(float maxHealthValue)
    {
        maxHealth = maxHealthValue;
        currentDisplayedHealth = maxHealthValue;
        targetHealth = maxHealthValue;
        UpdateHealthBarFill();
    }

    public void UpdateHealth(float newHealth)
    {
        targetHealth = newHealth;
        ShowHealthBar();

        if (healthUpdateCoroutine != null)
        {
            StopCoroutine(healthUpdateCoroutine);
        }

        healthUpdateCoroutine = StartCoroutine(AnimateHealthChange());
    }

    private IEnumerator AnimateHealthChange()
    {
        while (!Mathf.Approximately(currentDisplayedHealth, targetHealth))
        {
            // Плавно меняем значение
            currentDisplayedHealth = Mathf.MoveTowards(
                currentDisplayedHealth, 
                targetHealth,
                healthChangeSpeed * maxHealth * Time.deltaTime
            );
            
            UpdateHealthBarFill();
            yield return null;
        }
    }

    private void UpdateHealthBarFill()
    {
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = currentDisplayedHealth / maxHealth;
        }
    }

    private void ShowHealthBar()
    {
        SetHealthBarVisible(true);
        hideTimer = hideDelay;
        shouldBeVisible = true;
    }

    private void SetHealthBarVisible(bool visible)
    {
        healthBarCanvas.enabled = visible;
    }
}