using UnityEngine;

public class testscript : MonoBehaviour
{
    [SerializeField]
    private Vector3 force;
    [SerializeField]
    Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        force = CRManager.Truncate(new Vector3(10, 0, 0));
        rb.AddForce(force);
    }
}
