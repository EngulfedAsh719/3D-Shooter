using UnityEngine;
using UnityEngine.AI;
using System;

public class ZombieAI : MonoBehaviour 
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float damageAmount = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float damageDelay = 0.5f;

    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;
    private PlayerHealth playerHealth;
    
    private float attackTimer;
    private bool isAttacking;
    private bool isDead;

    // Строковые идентификаторы для анимаций
    private const string IS_WALKING = "isWalking";
    private const string IS_ATTACKING = "isAttacking";
    private const string DIE = "Die";

    public event Action<ZombieAI> OnZombieDeath;
    public event Action OnAttackPerformed;

    public void Initialize(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogError($"Player Transform is null in {gameObject.name}!");
            return;
        }

        target = playerTransform;
        InitializeComponents();
        enabled = true;
    }

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
        }

        if (agent == null || animator == null || playerHealth == null)
        {
            Debug.LogError($"Missing required components on {gameObject.name}!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (isDead || target == null || !enabled) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        if (distanceToTarget <= attackDistance)
        {
            HandleAttack();
        }
        else
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        if (agent == null) return;

        isAttacking = false;
        attackTimer = 0f;
        
        animator.SetBool(IS_WALKING, true);
        animator.SetBool(IS_ATTACKING, false);
        
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private void HandleAttack()
    {
        if (!isAttacking)
        {
            StartAttack();
        }

        UpdateAttack();
        RotateTowardsTarget();
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = 0f;
        
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_ATTACKING, true);
        
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    private void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= damageDelay && attackTimer < damageDelay + 0.02f)
        {
            TryDealDamage();
        }

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
        }
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 
                Time.deltaTime * rotationSpeed);
        }
    }

    private void TryDealDamage()
    {
        if (isDead || playerHealth == null) return;

        float currentDistance = Vector3.Distance(transform.position, target.position);
        if (currentDistance <= attackDistance)
        {
            playerHealth.TakeDamage(damageAmount);
            OnAttackPerformed?.Invoke();
        }
    }

    public void OnDeath()
    {
        isDead = true;
        enabled = false;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null)
        {
            int randomDeathAnim = UnityEngine.Random.Range(1, 3);
            animator.SetInteger(DIE, randomDeathAnim);
        }

        OnZombieDeath?.Invoke(this);
    }

    // Unity Editor Helpers
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    // Public API
    public bool IsAttacking() => isAttacking;
    public bool IsDead() => isDead;
    public float GetAttackDistance() => attackDistance;
}