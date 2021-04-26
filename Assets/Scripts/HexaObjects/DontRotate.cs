
using UnityEngine;

public class DontRotate : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
       if (this.transform.hasChanged)
            this.transform.up = Vector3.up;
    }
}
