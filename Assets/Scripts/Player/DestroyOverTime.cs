using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    public float destroyDelay = 1.5f;

    private void Start()
    {
        Invoke(nameof(DestroyObject), destroyDelay);
    }

    private void DestroyObject()
    {
        Destroy(gameObject);
    }
}