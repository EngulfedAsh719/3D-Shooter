using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

public class ZombieAI : MonoBehaviour 
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float damageAmount = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float[] attackTimings = new float[] { 0.57f, 1.52f };
    [SerializeField] private float attackAnimationLength = 2.479f;
    
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float pathUpdateInterval = 0.2f;
    [SerializeField] private float minimumStoppingDistance = 1.5f;
    [SerializeField] private float velocityThreshold = 0.1f;

    [Header("Attack Interruption")]
    [SerializeField] private float attackInterruptDistance = 2.5f;
    [SerializeField] private float reducedCooldownMultiplier = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform target;
    private PlayerHealth playerHealth;
    private Collider zombieCollider;
    
    private float attackTimer;
    private bool isAttacking;
    private bool isDead;
    private HashSet<int> dealtDamageForTimings;
    private float currentAttackTime;
    private float nextPathUpdate;
    private bool isAttackReady = true;

    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int DieHash = Animator.StringToHash("Die");

    public event Action<ZombieAI> OnZombieDeath;
    public event Action OnAttackPerformed;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        zombieCollider = GetComponent<Collider>();
        dealtDamageForTimings = new HashSet<int>();

        if (agent != null)
        {
            agent.stoppingDistance = minimumStoppingDistance;
            agent.updateRotation = false;
            agent.autoBraking = true;
        }
    }

    public void Initialize(Transform playerTransform)
    {
        if (playerTransform == null) return;

        target = playerTransform;
        playerHealth = target.GetComponent<PlayerHealth>();
        
        if (playerHealth == null)
        {
            enabled = false;
            return;
        }

        ResetState();
        enabled = true;
    }

    private void ResetState()
    {
        isAttacking = false;
        isDead = false;
        currentAttackTime = 0f;
        attackTimer = 0f;
        nextPathUpdate = 0f;
        isAttackReady = true;
        dealtDamageForTimings.Clear();

        UpdateAnimatorState(false, true);
    }

    private void Update()
    {
        if (ShouldSkipUpdate()) return;

        UpdateTimers();
        UpdateBehavior();
        UpdateRotation();
    }

    private void UpdateTimers()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                isAttackReady = true;
            }
        }

        if (isAttacking)
        {
            currentAttackTime += Time.deltaTime;
            
            // Check if we should interrupt the attack
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > attackInterruptDistance)
            {
                InterruptAttack();
                return;
            }

            CheckAttackTimings();

            if (currentAttackTime >= attackAnimationLength)
            {
                CompleteAttackAnimation();
            }
        }
    }

    private void InterruptAttack()
    {
        isAttacking = false;
        currentAttackTime = 0f;
        dealtDamageForTimings.Clear();
        
        // Set a reduced cooldown for interrupted attacks
        attackTimer = attackCooldown * reducedCooldownMultiplier;

        // Update animator and movement state
        UpdateAnimatorState(false, true);
        ResumeMovement();
    }

    private bool ShouldSkipUpdate()
    {
        return isDead || target == null || !enabled || GameManager.Instance.isGamePaused;
    }

    private void UpdateBehavior()
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        bool isInAttackRange = distanceToTarget <= attackDistance;
        bool hasStoppedMoving = agent.velocity.magnitude < velocityThreshold;

        if (isInAttackRange)
        {
            if (!isAttacking && isAttackReady && hasStoppedMoving)
            {
                StartAttack();
            }
            StopMovement();
        }
        else if (!isAttacking)
        {
            UpdatePathfinding();
            ResumeMovement();
        }
    }

    private void UpdatePathfinding()
    {
        if (Time.time >= nextPathUpdate)
        {
            agent.stoppingDistance = Mathf.Min(attackDistance * 0.8f, minimumStoppingDistance);
            agent.SetDestination(target.position);
            nextPathUpdate = Time.time + pathUpdateInterval;
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        isAttackReady = false;
        currentAttackTime = 0f;
        dealtDamageForTimings.Clear();
        
        UpdateAnimatorState(true, false);
        StopMovement();
    }

    private void CompleteAttackAnimation()
    {
        isAttacking = false;
        currentAttackTime = 0f;
        attackTimer = attackCooldown;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > attackDistance)
        {
            UpdateAnimatorState(false, true);
            ResumeMovement();
        }
    }

    private void UpdateAnimatorState(bool attacking, bool walking)
    {
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, attacking);
            animator.SetBool(IsWalkingHash, walking);
        }
    }

    private void StopMovement()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        UpdateAnimatorState(isAttacking, false);
    }

    private void ResumeMovement()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }
        UpdateAnimatorState(false, true);
    }

    private void CheckAttackTimings()
    {
        for (int i = 0; i < attackTimings.Length; i++)
        {
            if (!dealtDamageForTimings.Contains(i) && currentAttackTime >= attackTimings[i])
            {
                TryDealDamage();
                dealtDamageForTimings.Add(i);
                OnAttackPerformed?.Invoke();
            }
        }
    }

    private void TryDealDamage()
    {
        if (isDead || playerHealth == null) return;

        float currentDistance = Vector3.Distance(transform.position, target.position);
        if (currentDistance <= attackDistance)
        {
            playerHealth.TakeDamage(damageAmount);
        }
    }

    private void UpdateRotation()
    {
        if (target == null) return;

        Vector3 direction;
        if (isAttacking || agent.isStopped)
        {
            direction = (target.position - transform.position).normalized;
        }
        else
        {
            direction = agent.velocity.normalized;
        }

        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 
                Time.deltaTime * rotationSpeed);
        }
    }

    public void OnDeath()
    {
        if (isDead) return;

        isDead = true;
        enabled = false;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        UpdateAnimatorState(false, false);
        if (animator != null)
        {
            animator.SetInteger(DieHash, UnityEngine.Random.Range(1, 3));
        }

        if (zombieCollider != null)
        {
            zombieCollider.enabled = false;
        }

        OnZombieDeath?.Invoke(this);
    }

    public bool IsAttacking() => isAttacking;
    public bool IsDead() => isDead;
}