using UnityEngine;

public class Medkit : MonoBehaviour
{
    [Header("Healing Settings")]
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobHeight = 0.5f;
    
    private Vector3 startPosition;
    private float bobTime;

    private void Start()
    {
        startPosition = transform.position;
        bobTime = Random.Range(0f, 2f * Mathf.PI); // Рандомная начальная фаза для разной высоты аптечек
    }

    private void Update()
    {
        // Вращение аптечки
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Плавное движение вверх-вниз
        bobTime += Time.deltaTime * bobSpeed;
        float newY = startPosition.y + Mathf.Sin(bobTime) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
                AudioManager.Instance.PlayMedkitPickup(transform.position);
                Destroy(gameObject);
            }
        }
    }
}