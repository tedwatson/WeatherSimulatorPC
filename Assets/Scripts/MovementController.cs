using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

    // The camera's movement speed
    public float speed = 1f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        var x = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
        var y = Input.GetAxis("Vertical")   * Time.deltaTime * speed;

        transform.Rotate(y, x, 0);
    }
}
