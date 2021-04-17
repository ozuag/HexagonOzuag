
using UnityEngine;

public class DontRotate : MonoBehaviour
{
    [SerializeField]
    private Transform targetTransform;

    float angleOffSet = 0f;
    Vector3 _angle = Vector3.zero;


    private void Start()
    {
        this.angleOffSet = this.transform.localEulerAngles.z;
    }


    // Update is called once per frame
    void Update()
    {
        if (this.targetTransform == null)
            return;

        if(this.targetTransform.hasChanged)
        {
            _angle.z = this.angleOffSet - this.targetTransform.localEulerAngles.z;
            this.transform.localEulerAngles = _angle;

        }
    }
}
