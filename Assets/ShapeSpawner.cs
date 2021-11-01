using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeSpawner : MonoBehaviour
{
    public Shape[] Shapes;
    public GameObject ShapeHolder;
    public float ObjectSpawnCooldown = 0.5f;
    public DetailLevel Details;
    private float NextSpawn;


    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Started!");
        NextSpawn = Time.time;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && Time.time > NextSpawn)
        {
            SpawnShape(0);
        }
        if (Input.GetKeyDown(KeyCode.X) && Time.time > NextSpawn)
        {
            SpawnShape(1);
        }
        
        if (Input.GetKeyDown(KeyCode.C) && Time.time > NextSpawn)
        {
            SpawnShape(2);
        }
        
        if (Input.GetKeyDown(KeyCode.V) && Time.time > NextSpawn)
        {
            SpawnShape(3);
        }
    }

    private void SpawnShape(int index)
    {
        NextSpawn = Time.time + ObjectSpawnCooldown;

        // Generate random position
        Vector3 SpawnPosition = new Vector3(Random.Range(-3.0f, 3.0f), Random.Range(-3.0f, 3.0f), Random.Range(-3.0f, 3.0f));

        Shape NewShape = Instantiate(Shapes[index], SpawnPosition, Quaternion.identity);
        NewShape.transform.parent = ShapeHolder.transform;
        NewShape.SetInfo(Details, SpawnPosition);
        Debug.Log("Spawned shape!");
    }
}
