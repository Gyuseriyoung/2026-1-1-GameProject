using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [SerializeField] private float destroyTime = 1f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
