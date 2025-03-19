using UnityEngine;

public class TestColliders : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("me: " + gameObject+ ", other: " + other.gameObject);   
    }
}
