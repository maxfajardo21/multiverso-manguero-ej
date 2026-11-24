using UnityEngine;

[DefaultExecutionOrder(200)]   
public class ParallaxLayer : MonoBehaviour
{
    public float factorX = 0.5f;
    public float factorY = 0.5f;

    Transform cam;
    Vector3 startPos;
    Vector3 camStart;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        startPos = transform.position;
        if (cam != null) camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 d = cam.position - camStart;
        transform.position = new Vector3(startPos.x + d.x * factorX, startPos.y + d.y * factorY, startPos.z);
    }
}
