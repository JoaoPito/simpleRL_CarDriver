using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 1;
    public float upSpeed = 1;

    public Transform cam;

    UnityStandardAssets.Cameras.FreeLookCam camScr;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main.transform;
        camScr = GetComponent<UnityStandardAssets.Cameras.FreeLookCam>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.Translate((Vector3.forward * speed * Input.GetAxis("Vertical")) + (Vector3.right * speed * Input.GetAxis("Horizontal")));

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            transform.Translate(Vector3.up * speed);
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            transform.Translate(-Vector3.up * speed);
        }

        if(Input.GetAxis("Vertical")!=0 || Input.GetAxis("Horizontal") != 0)
        {
            camScr.SetTarget(null);
        }
    }
}
