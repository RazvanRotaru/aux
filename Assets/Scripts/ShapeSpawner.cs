using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ShapeSpawner : MonoBehaviour
{
    [FormerlySerializedAs("Shapes")] public Shape[] shapes;
    [FormerlySerializedAs("ShapeHolder")] public GameObject shapeHolder;
    [FormerlySerializedAs("ObjectSpawnCooldown")] public float objectSpawnCooldown = 0.5f;
    [FormerlySerializedAs("Details")] public DetailLevel details;
    private float _nextSpawn;


    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Started!");
        _nextSpawn = Time.time;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && Time.time > _nextSpawn)
        {
            SpawnShape(0);
        }
        if (Input.GetKeyDown(KeyCode.X) && Time.time > _nextSpawn)
        {
            SpawnShape(1);
        }
        
        if (Input.GetKeyDown(KeyCode.C) && Time.time > _nextSpawn)
        {
            SpawnShape(2);
        }
        
        if (Input.GetKeyDown(KeyCode.V) && Time.time > _nextSpawn)
        {
            SpawnShape(3);
        }
    }

    private void SpawnShape(int index)
    {
        _nextSpawn = Time.time + objectSpawnCooldown;

        // Generate random position
        Vector3 spawnPosition = new Vector3(Random.Range(-3.0f, 3.0f), Random.Range(-3.0f, 3.0f), Random.Range(-3.0f, 3.0f));

        Shape newShape = Instantiate(shapes[index], spawnPosition, Quaternion.identity);
        newShape.transform.parent = shapeHolder.transform;
        newShape.SetInfo(details, spawnPosition);
    }
}
