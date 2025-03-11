using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Cache components
    private Camera mainCamera;
   
    private void Awake()
    {
        mainCamera = Camera.main;
       
        if (!mainCamera)
        {
            Debug.LogError("Main Camera not found!");
        }
    }
}