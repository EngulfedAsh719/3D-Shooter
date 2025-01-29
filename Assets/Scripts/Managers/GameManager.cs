using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    public bool isGamePaused { get; private set; }
    public bool isGameOver { get; private set; }

    // События для оповещения других систем
    public event Action OnGameStarted;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action OnGameOver;
    public event Action OnGameWon;

    private void Awake()
    {
        // Реализация паттерна Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGame()
    {
        isGamePaused = false;
        isGameOver = false;

        // Находим игрока, если он не назначен через инспектор
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Make sure there is a player object with 'Player' tag in the scene.");
            }
        }
    }

    public Transform GetPlayer()
    {
        return playerTransform;
    }

    public void StartGame()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        OnGameStarted?.Invoke();
    }

    public void PauseGame()
    {
        if (!isGamePaused)
        {
            isGamePaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
    }

    public void ResumeGame()
    {
        if (isGamePaused)
        {
            isGamePaused = false;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
    }

    public void GameOver()
    {
        if (!isGameOver)
        {
            Debug.Log("Игра окончена");
        }
    }

    public void WinGame()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        OnGameWon?.Invoke();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Убедитесь, что сцена MainMenu существует
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    // Метод для проверки состояния игры
    public bool IsGameActive()
    {
        return !isGamePaused && !isGameOver;
    }

    // Обработка ввода для паузы (можно вызывать из Update)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // Дополнительные методы для управления игровыми системами
    public void PlayerDied()
    {
        GameOver();
    }

    public void AllWavesCompleted()
    {
        WinGame();
    }

    // Можно добавить систему очков
    private int score = 0;
    public int GetScore() => score;
    
    public void AddScore(int points)
    {
        score += points;
        // Здесь можно добавить событие для обновления UI
    }
}