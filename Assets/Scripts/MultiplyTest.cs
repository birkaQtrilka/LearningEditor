using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class MultiplyTest : MonoBehaviour
{
    [SerializeField] Vector3 pos;
    [SerializeField] float scale = 3;
    
    void Update()
    {
        pos = transform.position * scale;
    }
}
