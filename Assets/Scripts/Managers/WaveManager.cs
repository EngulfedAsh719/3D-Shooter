using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text zombieCountText;
    [SerializeField] private Animator waveAnimator;
    
    [Header("Animation Settings")]
    [SerializeField] private string showWaveAnimTrigger = "Wave";
    [SerializeField] private float textUpdateDelay = 0.5f; 
    
    private void Start()
    {
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (waveText == null)
            Debug.LogError($"Wave Text component not assigned on {gameObject.name}!");
            
        if (zombieCountText == null)
            Debug.LogError($"Zombie Count Text component not assigned on {gameObject.name}!");
            
        if (waveAnimator == null)
            Debug.LogError($"Animator component not assigned on {gameObject.name}!");
    }

    public void ShowNewWave(int waveNumber)
    {
        // Обновляем текст волны
        waveText.text = $"Волна {waveNumber}";
        
        // Запускаем анимацию
        if (waveAnimator != null)
        {
            waveAnimator.SetTrigger(showWaveAnimTrigger);
        }
        
        AudioManager.Instance.PlayNewWaveSound();
    }

    public void UpdateWaveProgress(int killedZombies, int totalZombies)
    {
        zombieCountText.text = $"{killedZombies} / {totalZombies}";
    }

    public void ShowWaveCooldown(float remainingTime)
    {
        zombieCountText.text = $"Следующая волна через: {Mathf.CeilToInt(remainingTime)} сек.";
    }

    public void SetUIVisibility(bool visible)
    {
        waveText.gameObject.SetActive(visible);
        zombieCountText.gameObject.SetActive(visible);
    }
}