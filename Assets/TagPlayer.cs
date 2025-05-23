using UnityEngine;

public class TagPlayer : MonoBehaviour
{
    [SerializeField] Light light;
    [SerializeField] public GameObject target;
    [SerializeField] public float intensity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => intensity = light.intensity;

    // Update is called once per frame
    void Update() => light.intensity = intensity;

    private void OnDrawGizmos()
    {
        var heading = target.transform.position - transform.position;
        var distance = heading.magnitude;
        var direction = heading / distance; // This is now the normalized direction.
        direction *= intensity;
        Debug.DrawRay(transform.position, direction);
    }
}
