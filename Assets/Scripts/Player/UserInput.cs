using UnityEngine;

public class UserInput : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    
    
    private Transform cam;
    private Vector3 camForward;
    private Vector3 move;
    private Vector3 moveInput;
    private Vector3 lookPos;
    
    private float forwardAmount;
    private float turnAmount;
    private bool isMoving;
    private bool movementEnabled = true;
    
    private Animator anim;
    private Rigidbody rigidBody;
    private Transform currentAimTarget;
    private float aimAssistInfluence;

    private void Start()
    {
        cam = Camera.main.transform;
        anim = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        SetUpAnimator();
        
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleRotation();
    }

    private void HandleRotation()
    {
        // Получаем базовое направление от мыши
        Vector3 mouseDirection = GetMouseAimDirection();
        Vector3 finalAimDirection = mouseDirection;

        currentAimTarget = null;
        aimAssistInfluence = 0f;

        ApplyRotation(finalAimDirection);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    private Vector3 GetMouseAimDirection()
{
    // Получаем позицию мыши в пространстве экрана
    Vector3 mousePosition = Input.mousePosition;
    
    // Создаем луч из камеры в точку мыши
    Ray ray = Camera.main.ScreenPointToRay(mousePosition);
    
    // Определяем плоскость на уровне игрока
    Plane plane = new Plane(Vector3.up, transform.position);
    
    // Находим точку пересечения луча с плоскостью
    if (plane.Raycast(ray, out float distance))
    {
        Vector3 targetPoint = ray.GetPoint(distance);
        return (targetPoint - transform.position).normalized;
    }
    
    return transform.forward;
}

    private void ApplyRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                Time.deltaTime * rotationSpeed);
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
        UpdateAnimations();
    }

    private void HandleMovement()
    {
        if (!movementEnabled)
        {
            // Останавливаем движение
            rigidBody.linearVelocity = new Vector3(0, rigidBody.linearVelocity.y, 0);
            moveInput = Vector3.zero;
            HandleFootstepSound(false);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (cam != null)
        {
            camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
            move = vertical * camForward + horizontal * cam.right;
        }
        else
        {
            move = vertical * Vector3.forward + horizontal * Vector3.right;
        }

        if (move.magnitude > 1)
            move.Normalize();

        moveInput = move;
        rigidBody.linearVelocity = move * speed + new Vector3(0, rigidBody.linearVelocity.y, 0);
        
        HandleFootstepSound(move.magnitude > 0);
    }

    private void UpdateAnimations()
    {
        Vector3 localMove = transform.InverseTransformDirection(moveInput);
        turnAmount = localMove.x;
        forwardAmount = Mathf.Abs(localMove.z);

        anim.SetFloat("Forward", forwardAmount, 0.1f, Time.deltaTime);
        anim.SetFloat("Sideways", turnAmount, 0.1f, Time.deltaTime);
    }

    private void SetUpAnimator()
    {
        foreach (var childAnimator in GetComponentsInChildren<Animator>())
        {
            if (childAnimator != anim)
            {
                anim.avatar = childAnimator.avatar;
                Destroy(childAnimator);
                break;
            }
        }
    }

    private void HandleFootstepSound(bool moving)
    {
        if (moving && !isMoving)
        {
            AudioManager.Instance.StartFootsteps();
            isMoving = true;
        }
        else if (!moving && isMoving)
        {
            AudioManager.Instance.StopFootsteps();
            isMoving = false;
        }
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}