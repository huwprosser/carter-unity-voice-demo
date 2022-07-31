using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialController : MonoBehaviour
{

    bool blue = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {   
        if (Input.GetKey("space") && !blue)
        {
            blue = true;
           
        } 

        if (!Input.GetKey("space") && blue)
        {
            blue = false;
            GameObject.Find("Sphere").GetComponent<Renderer>().material.color = new Color(25, 255, 255);
        } 
    }
}
