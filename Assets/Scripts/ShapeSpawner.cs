using System.Collections;
using System.Collections.Generic;
using Collisions.GPU;
using UnityEngine;
using UnityEngine.Serialization;

public enum Scenario { 
    LOW_NUMBER,
    MEDIUM_NUMBER,
    HIGH_NUMBER
};

public class ShapeSpawner : MonoBehaviour
{
    [FormerlySerializedAs("Shapes")] public Shape[] shapes;
    [FormerlySerializedAs("ShapeHolder")] public GameObject shapeHolder;
    [FormerlySerializedAs("ObjectSpawnCooldown")] public float objectSpawnCooldown = 0.5f;
    [FormerlySerializedAs("Details")] public DetailLevel details;
    private float _nextSpawn;
    private Vector3 boxCenter;
    private float xLength;
    private float zLength;


    [SerializeField]private GameObject boxBase;
    [SerializeField]private Scenario scenarioType;
    [SerializeField]private bool loadScenario = false;
    [SerializeField] private bool useTest = true;
    [SerializeField] private bool useGPU = true;

    // Start is called before the first frame update
    private void Start()
    {
        _nextSpawn = Time.time;

        if (boxBase == null)
        {
            Debug.LogError("No base of the cube set!");
            return;
        }

        ComputeBoxCenter();

        if (loadScenario)
        {
            LoadScenario();
        }

        gameObject.AddComponent<CollisionResolution>();
        if (useGPU)
        {
            gameObject.AddComponent<GPUCollideManager>();
        }
        else
        {
            gameObject.AddComponent<CollideManager>();
        }
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
        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-2.0f * xLength + 2.0f, 2.0f * xLength - 2.0f), Random.Range(2.0f, 2.0f * xLength - 2.0f), Random.Range(-2.0f * zLength + 2.0f, 2.0f * zLength - 2.0f));

        Shape newShape = Instantiate(shapes[index], spawnPosition, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
        newShape.transform.parent = shapeHolder.transform;
        RegisterCollider(newShape);
    }

    void ComputeBoxCenter()
    {
        MeshFilter meshFilter = boxBase.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError("No mesh filter attached to the box base");
            return;
        }

        Vector3[] vertices = meshFilter.mesh.vertices;
        float minX = vertices[0].x;
        float maxX = minX;
        float minZ = vertices[0].z;
        float maxZ = minZ;

        for (int i = 0; i < vertices.Length; i++)
        {
            minX = minX > vertices[i].x ? vertices[i].x : minX;
            maxX = maxX < vertices[i].x ? vertices[i].x : maxX;
            minZ = minZ > vertices[i].z ? vertices[i].z : minZ;
            maxZ = maxZ < vertices[i].z ? vertices[i].z : maxZ;
        }

        xLength = maxX - minX;
        zLength = maxZ - minZ;

        boxCenter = transform.position + new Vector3(xLength / 2.0f, xLength / 2.0f, zLength / 2.0f);
        
        Debug.Log($"X {xLength}");
        Debug.Log($"Z {zLength}");
    }

    private void LoadScenario()
    {
        switch (scenarioType)
        {
            case Scenario.LOW_NUMBER:
                SpawnScene(100, 250, 250, 0);
                break;
            case Scenario.MEDIUM_NUMBER:
                SpawnScene(250, 500, 500, 0);
                break;
            case Scenario.HIGH_NUMBER:
                SpawnScene(500, 1000, 650, 0);
                break;
            default:
                break;
        }
    }

    // TOOD: Spawn according objects when collision works
    private void SpawnScene(int sphereNo, int boxNo, int cylNo, int coneNo)
    {
        List<int> remainingSpawns = new List<int>();
        remainingSpawns.Add(boxNo);
        remainingSpawns.Add(sphereNo);
        remainingSpawns.Add(cylNo);

        List<int> spawnIndices = new List<int>();
        spawnIndices.Add(0);
        spawnIndices.Add(1);
        spawnIndices.Add(2);

        while (!useTest && spawnIndices.Count > 0)
        {
            int index = spawnIndices[Random.Range(0, spawnIndices.Count)];

            remainingSpawns[index]--;

            if (remainingSpawns[index] == 0)
            {
                spawnIndices.Remove(index);
            }

            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-2.0f * xLength + 2.0f, 2.0f * xLength - 2.0f), Random.Range(2.0f, 2.0f * xLength - 2.0f), Random.Range(-2.0f * zLength + 2.0f, 2.0f * zLength - 2.0f));

            Shape newShape = Instantiate(shapes[index], spawnPosition, Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
            newShape.transform.parent = shapeHolder.transform;
            RegisterCollider(newShape);
        }

        // TEST
        if (useTest)
        {
            for (int i = 0; i < 60; i++)
            {
                Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-2.0f * xLength + 2.0f, 2.0f * xLength - 2.0f), Random.Range(2.0f, 2.0f * xLength - 2.0f), Random.Range(-2.0f * zLength + 2.0f, 2.0f * zLength - 2.0f));

                Shape newShape = Instantiate(shapes[Random.Range(0,3)], spawnPosition, Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                newShape.transform.parent = shapeHolder.transform;
                RegisterCollider(newShape);
            }
        }
    }

    private static void RegisterCollider(Shape shape)
    {
        // if (CollideManager.Instance != null)
        // {
        //     CollideManager.Instance.AddCollider(shape);
        // }
        //
        // if (GPUCollideManager.Instance != null)
        // {
        //     GPUCollideManager.Instance.AddCollider(shape);
        // }
    }
}
