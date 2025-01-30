using UnityEngine;
using UnityEngine.UI;

public class ShootingHandler : MonoBehaviour
{
    [Header("Shooting Settings")]
    public int maxBullets = 30;
    public float fireRate = 0.3f;
    public Text bulletText;

    private int bullets;
    private float fireRateTimer;
    private Animator animator;
    private bool isReloading;
    private LaserAim laserAim;
    private UserInput userInput;
    private bool canShoot = true;
    private bool wasEmptyClick = false;

    public bool IsReloading => isReloading;

    public int Bullets
    {
        get => bullets;
        set
        {
            bullets = Mathf.Clamp(value, 0, maxBullets);
            UpdateBulletText();
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        animator = GetComponent<Animator>();
        laserAim = GetComponent<LaserAim>();
        userInput = GetComponent<UserInput>();

        Bullets = maxBullets;
        fireRateTimer = fireRate;
    }

    private void Update()
    {
        if (GameManager.Instance.isGamePaused)
            return;

        HandleFireRateTimer();
        HandleReloadInput();
        HandleShootingInput();
    }

    private void HandleFireRateTimer()
    {
        if (!canShoot)
        {
            fireRateTimer += Time.deltaTime;
            if (fireRateTimer >= fireRate)
            {
                canShoot = true;
            }
        }
    }

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && Bullets < maxBullets)
        {
            StartReload();
        }
    }

    private void HandleShootingInput()
    {
        if (isReloading) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (Bullets <= 0)
            {
                if (!wasEmptyClick)
                {
                    AudioManager.Instance.PlayEmptyGunSound();
                    wasEmptyClick = true;
                }
            }
            else if (canShoot)
            {
                Shoot();
                wasEmptyClick = false;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (Bullets > 0 && canShoot)
            {
                Shoot();
                wasEmptyClick = false;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            wasEmptyClick = false;
        }
    }

    private void Shoot()
    {
        if (laserAim != null)
        {
            laserAim.ShootLaser();
        }

        EmitShootingParticles();
        
        Bullets--;
        canShoot = false;
        fireRateTimer = 0f;
        AudioManager.Instance.PlayGunShot();
        UpdateBulletText();
    }

    private void EmitShootingParticles()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Emit(1);
        }
    }

    private void StartReload()
    {
        isReloading = true;
        animator.SetBool("Reload", true);
        if (userInput != null)
        {
            userInput.SetMovementEnabled(false);
        }
        AudioManager.Instance.PlayReload();
        Invoke(nameof(FinishReload), 1f);
    }

    private void FinishReload()
    {
        isReloading = false;
        animator.SetBool("Reload", false);
        if (userInput != null)
        {
            userInput.SetMovementEnabled(true);
        }
        Bullets = maxBullets;
        UpdateBulletText();
        wasEmptyClick = false;
    }

    private void UpdateBulletText()
    {
        if (bulletText != null)
        {
            bulletText.text = $"{Bullets} / {maxBullets}";
        }
    }
}