using UnityEngine;

public class GetDimensions : MonoBehaviour
{
    [SerializeField] Renderer renderer;
    [SerializeField] Vector3 dimensions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => dimensions = renderer.bounds.size;

    // Update is called once per frame
    void Update() => dimensions = renderer.bounds.size;

}
