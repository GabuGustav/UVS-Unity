using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrailerController : MonoBehaviour
{
    public Transform trailerHitch;
    public Rigidbody TrailerBody { get; private set; }

    private void Awake()
    {
        TrailerBody = GetComponent<Rigidbody>();
    }
}
