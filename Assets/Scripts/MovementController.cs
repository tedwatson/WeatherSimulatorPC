using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    public GameObject cameraParent;

    // The camera's movement speed
    public float rotateSpeed = 1f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        var x = -Input.GetAxis("Vertical") * Time.deltaTime * rotateSpeed;
        var y = Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed;

        cameraParent.transform.Rotate(x, y, 0);

        var temp = transform.rotation;

        cameraParent.transform.rotation = Quaternion.Euler(new Vector3(0, Quaternion.LookRotation(transform.forward).eulerAngles.y, 0));

        transform.rotation = temp;

        /*
        var x = Input.GetAxis("Vertical") * Time.deltaTime * speed;
        var y = Input.GetAxis("Horizontal") * Time.deltaTime * speed;

        print(Input.GetAxis("Horizontal"));

        // Get and store current x and y rotation
        var oldx = transform.rotation.x;
        var oldy = transform.rotation.y;

        // Move x and y rotation to neutral
        transform.Rotate(-oldx, -oldy, 0);

        // Apply the transformation
        transform.Rotate(x, y, 0);

        // Add back the old x and y rotation values
        transform.Rotate(oldx, oldy, 0);
        */
    }
}
