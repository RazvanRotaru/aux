using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [SerializeField] private GameObject debugPoint;
    public GameObject DebugPoint => debugPoint;

    public static DebugManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Reset()
    {
        debugPoint = Resources.Load("Prefabs/DebugPoint") as GameObject;
        print(debugPoint);
    }

    
}
