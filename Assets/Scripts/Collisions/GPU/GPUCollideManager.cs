using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Shapes;
using UnityEngine;
using Plane = Shapes.Plane;

namespace Collisions.GPU
{
    #region Structures

    struct HalfEdgeInfo
    {
        Vector3 vertex;
        Vector3 edge;

        Vector3 local_vertex;

        Vector3 n1;
        Vector3 n2;

        public HalfEdgeInfo(HalfEdge halfEdge)
        {
            vertex = halfEdge.VertexLocal;
            local_vertex = halfEdge.Vertex - halfEdge.Transform.position;
            edge = halfEdge.EdgeLocal;
            n1 = halfEdge.Face.NormalLocal;
            n2 = halfEdge.Twin.Face.NormalLocal;
        }
    }

    struct FaceInfo
    {
        Vector3 normal;
        Vector3 center;

        public FaceInfo(Face face)
        {
            normal = face.NormalLocal;
            center = face.CenterLocal;
        }

        public FaceInfo(Vector3 n, Vector3 p)
        {
            normal = n;
            center = p;
        }
    }

    struct ShapeInfo
    {
        Vector2Int pointInfo;
        Vector2Int halfEdgeInfo;
        Vector2Int faceInfo;
        int type;
        float radius;


        public ShapeInfo(Collider s, Vector2Int p, Vector2Int he, Vector2Int f, float r)
        {
            pointInfo = p;
            halfEdgeInfo = he;
            faceInfo = f;
            radius = 1.1f;//r;
            type = s switch
            {
                SphereCollider _ => 1,
                HalfPlaneCollider _ => 2,
                _ => 0
            };
        }
    }

    struct TransformMat
    {
        Matrix4x4 R;
        Vector3 T;

        public TransformMat(Transform t)
        {
            R = Matrix4x4.Rotate(t.rotation);
            T = t.position;
        }
    }

    #endregion

    public class GPUCollideManager : MonoBehaviour
    {
        #region Buffers

        private ComputeBuffer _halfEdgesBuffer;
        private ComputeBuffer _worldHalfEdgesBuffer;
        private ComputeBuffer _facesBuffer;
        private ComputeBuffer _worldFacesBuffer;
        private ComputeBuffer _shapesBuffer;
        private ComputeBuffer _pointsBuffer;
        private ComputeBuffer _collisionsBuffer;
        private ComputeBuffer _mats;

        #endregion

        [SerializeField] private List<Collider> colliders;
        [SerializeField] private List<HalfPlaneCollider> planes;

        [SerializeField] private int numColliders;

        [SerializeField] private ComputeShader detectionLogic;

        private Kernel _kDetect;
        private Kernel _kTransform;


        //DEBUG
        [SerializeField] private Vector4[] results;

        private List<int> faceOffsets = new List<int>();
        private List<int> heOffsets = new List<int>();

        private Dictionary<(int, int), bool> alreadyCollided = new Dictionary<(int, int), bool>();


        public static GPUCollideManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // kDetect = new Kernel(detectionLogic, "PolygonVsPolygon");
        }

        private void Reset()
        {
            detectionLogic = Resources.Load<ComputeShader>("Shaders/CollisionDetection");
        }

        private void Start()
        {
            Reset();
            detectionLogic = Resources.Load<ComputeShader>("Shaders/CollisionDetection");
            _kTransform = new Kernel(detectionLogic, "apply_transformations");
            _kDetect = new Kernel(detectionLogic, "collision_detection");

            colliders = new List<Collider>();
            var polygonColliders = FindObjectsOfType<PolygonCollider>();
            colliders.AddRange(polygonColliders);

            var sphereColliders = FindObjectsOfType<SphereCollider>().Where(sphereCollider =>
                polygonColliders.All(col =>
                    col.gameObject.GetInstanceID() != sphereCollider.gameObject.GetInstanceID()));
            foreach (var sC in sphereColliders)
            {
                sC.gameObject.AddComponent<PolygonCollider>();
            }

            colliders.AddRange(sphereColliders);
            numColliders = colliders.Count;

            planes = new List<HalfPlaneCollider>();
            planes.AddRange(FindObjectsOfType<HalfPlaneCollider>());


            CollisionResolution.Instance.SetColliders(colliders.Select(sC => sC as Collider).ToList());
            InitializeBuffers();


            colliders.AddRange(planes);

            results = new Vector4[colliders.Count * colliders.Count];
            detectionLogic.SetInt("objects_no", colliders.Count);
        }

        private void OnDisable()
        {
            colliders.Clear();

            // release buffers and make them null
            _halfEdgesBuffer.Release();
            _halfEdgesBuffer = null;

            _worldHalfEdgesBuffer.Release();
            _worldHalfEdgesBuffer = null;

            _worldFacesBuffer.Release();
            _worldFacesBuffer = null;

            _facesBuffer.Release();
            _facesBuffer = null;

            _shapesBuffer.Release();
            _shapesBuffer = null;

            _pointsBuffer.Release();
            _pointsBuffer = null;

            _collisionsBuffer.Release();
            _collisionsBuffer = null;

            _mats.Release();
            _mats = null;
        }

        private void Update()
        {
            UpdateBuffers();
            CollisionResolution.Instance.UpdateFrameDrag();

            detectionLogic.Dispatch(_kTransform.KernelID, colliders.Count, 1, 1);
            detectionLogic.Dispatch(_kDetect.KernelID, colliders.Count, colliders.Count, 1);

            _collisionsBuffer.GetData(results);

            foreach (var col in colliders)
            {
                col.Collides(false);
            }

            var len = colliders.Count;


            for (var i = 0; i < len; i++)
            {
                for (var j = i + 1; j < len; j++)
                {
                    if (alreadyCollided.ContainsKey((i, j)) || alreadyCollided.ContainsKey((j, i))) continue;
                    alreadyCollided.Add((i, j), true);

                    var index = i * len + j;
                    var res = results[index];
                    if ((int) (res[0]) == -2) // ObjVPlane
                    {
                        var pC = planes[(int) (res[1]) - numColliders];
                        if ((int) (res[2]) == 1) // SphereVPlane
                        {
                            var col = colliders[i] as SphereCollider;
                            CollideManager.HalfPlaneVsObject(pC.Plane, col, out var contactPoints);
                            CollisionResolution.Instance.ResolveCollision(col, pC, contactPoints);
                            foreach (var point in contactPoints)
                            {
                                point.Draw(Color.yellow);
                            }
                        }

                        if ((int) (res[2]) == 0) // PolygonVPlane
                        {
                            var col = colliders[i] as PolygonCollider;
                            CollideManager.HalfPlaneVsObject(pC.Plane, col, out var contactPoints);
                            print($"Contact Points: {contactPoints.Count}");
                            foreach (var point in contactPoints)
                            {
                                point.Draw(Color.yellow);
                            }

                            CollisionResolution.Instance.ResolveCollision(col, pC, contactPoints);
                        }
                    }

                    if ((int) (res[0]) == -1) // SphereVSphere
                    {
                        var sphereA = colliders[i] as SphereCollider;
                        if (sphereA == null) sphereA = (colliders[i] as PolygonCollider).SphereCollider;
                        var sphereB = colliders[(int) (res[1])] as SphereCollider;
                        if (sphereB == null) sphereB = (colliders[(int) (res[1])] as PolygonCollider).SphereCollider;
                        CollideManager.SphereVsSphere(sphereA, sphereB, out var contactPoints);
                        CollisionResolution.Instance.ResolveCollision(sphereA, sphereB, contactPoints);
                    }

                    if ((int) (res[0]) >= 0) // PolygonVPolygon
                    {
                        var colA = colliders[i].GetComponent<PolygonCollider>();
                        var colB = colliders[(int) res[1]].GetComponent<PolygonCollider>();

                        if (colB == null)
                        {
                            Debug.LogError($"Something went wrong {res}");
                        }

                        if (Math.Abs((int) res[0]) > 0) // FaceVFace
                        {
                            var sw = (int) res[0] > 10;
                            var refColider = sw ? colA : colB;
                            var faceIndex = (int) (res[2]);
                            Face refFace;
                            try
                            {
                                refFace = refColider.Shape.Faces[faceIndex];
                            }
                            catch
                            {
                                // alreadyCollided.Remove((i, j));
                                refColider = sw ? colB : colA;
                                refFace = refColider.Shape.Faces[faceIndex];
                                Debug.LogError(
                                    $"tried to access face index {faceIndex} but only {refColider.Shape.Faces.Count} faces");
                                // continue;
                            }

                            var distance = Mathf.Abs(res[3]);

                            var incidentFace = CollideManager.MostAntiParallelFace(refColider, refFace);
                            var contactPoints = CollideManager.FaceVFace(refColider, refFace, incidentFace, distance);
                            CollisionResolution.Instance.ResolveCollision(colA, colB, contactPoints.ToList());
                            foreach (var point in contactPoints)
                            {
                                point.Draw(Color.yellow);
                            }
                        }

                        if ((int) res[0] == 0) // EdgeVEdge
                        {
                            var edgeAIndex = (int) res[2];
                            var edgeBIndex = (int) res[3];

                            var edgeA = colA.Shape.HalfEdges[edgeAIndex];
                            var edgeB = colA.Shape.HalfEdges[edgeBIndex];

                            var distance = CollideManager.Distance(edgeA, edgeB, colA);
                            var contactPoints = CollideManager.EdgeVsEdge(colA, edgeA, edgeB, distance);
                            CollisionResolution.Instance.ResolveCollision(colA, colB, contactPoints.ToList());
                            foreach (var point in contactPoints)
                            {
                                point.Draw(Color.yellow);
                            }
                        }
                    }
                }
            }

            alreadyCollided.Clear();

            // TODO add physics on GPU
        }

        private void InitializeBuffers()
        {
            var points = new List<Vector3>();
            var shapesInfo = new List<ShapeInfo>();
            var halfEdgesInfo = new List<HalfEdgeInfo>();
            var facesInfo = new List<FaceInfo>();
            var transformMats = new List<TransformMat>();

            var hC = 0;
            var fC = 0;
            var sC = 0;

            var sz = 0;

            foreach (var s in colliders)
            {
                sz += 1;

                {
                    halfEdgesInfo.AddRange(s.Shape.HalfEdges.Select(he => new HalfEdgeInfo(he)));
                    heOffsets.Add(hC);
                    var halfEdges = s.Shape.HalfEdges.Count;

                    facesInfo.AddRange(s.Shape.Faces.Select(f => new FaceInfo(f)));
                    faceOffsets.Add(fC);
                    var faces = s.Shape.Faces.Count;

                    points.AddRange(s.Shape.MeshInfo.vertexPosition);

                    var pts = s.Shape.MeshInfo.vertexPosition.Length;
                    shapesInfo.Add(new ShapeInfo(s, new Vector2Int(sC, pts),
                        new Vector2Int(hC, halfEdges),
                        new Vector2Int(fC, faces), s.GetComponent<SphereCollider>().Radius));
                    sC += pts;
                    hC += halfEdges;
                    fC += faces;
                }

                {
                    transformMats.Add(new TransformMat(s.transform));
                }
            }

            foreach (var p in planes)
            {
                sz += 1;
                facesInfo.Add(new FaceInfo(Vector3.up, Vector3.zero));
                shapesInfo.Add(new ShapeInfo(p, new Vector2Int(sC, 0), new Vector2Int(hC, 0),
                    new Vector2Int(fC, 1), 0));
                fC += 1;
                transformMats.Add(new TransformMat(p.transform));
            }

            _pointsBuffer = new ComputeBuffer(points.Count, 12);
            _pointsBuffer.SetData(points);
            detectionLogic.SetBuffer(_kDetect.KernelID, "points", _pointsBuffer);

            _shapesBuffer = new ComputeBuffer(shapesInfo.Count, Marshal.SizeOf(typeof(ShapeInfo)));
            _shapesBuffer.SetData(shapesInfo);
            detectionLogic.SetBuffer(_kTransform.KernelID, "shapes", _shapesBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "shapes", _shapesBuffer);

            _mats = new ComputeBuffer(transformMats.Count, Marshal.SizeOf(typeof(TransformMat)));
            _mats.SetData(transformMats);
            detectionLogic.SetBuffer(_kDetect.KernelID, "mat", _mats);
            detectionLogic.SetBuffer(_kTransform.KernelID, "mat", _mats);

            _halfEdgesBuffer = new ComputeBuffer(halfEdgesInfo.Count, Marshal.SizeOf(typeof(HalfEdgeInfo)));
            _halfEdgesBuffer.SetData(halfEdgesInfo);
            detectionLogic.SetBuffer(_kTransform.KernelID, "half_edges", _halfEdgesBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "half_edges", _halfEdgesBuffer); // TODO: remove

            _worldHalfEdgesBuffer = new ComputeBuffer(halfEdgesInfo.Count, Marshal.SizeOf(typeof(HalfEdgeInfo)));
            _worldHalfEdgesBuffer.SetData(halfEdgesInfo);
            detectionLogic.SetBuffer(_kTransform.KernelID, "world_half_edges", _worldHalfEdgesBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "world_half_edges", _worldHalfEdgesBuffer);

            _facesBuffer = new ComputeBuffer(facesInfo.Count, Marshal.SizeOf(typeof(FaceInfo)));
            _facesBuffer.SetData(facesInfo);
            detectionLogic.SetBuffer(_kTransform.KernelID, "faces", _facesBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "faces", _facesBuffer);

            _worldFacesBuffer = new ComputeBuffer(facesInfo.Count, Marshal.SizeOf(typeof(FaceInfo)));
            _worldFacesBuffer.SetData(facesInfo);
            detectionLogic.SetBuffer(_kTransform.KernelID, "world_faces", _worldFacesBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "world_faces", _worldFacesBuffer);

            _collisionsBuffer = new ComputeBuffer(sz * sz, Marshal.SizeOf(typeof(Vector4)));
            _collisionsBuffer.SetData(new List<Vector4>(sz * sz));
            detectionLogic.SetBuffer(_kTransform.KernelID, "collisions", _collisionsBuffer);
            detectionLogic.SetBuffer(_kDetect.KernelID, "collisions", _collisionsBuffer);
        }

        private void UpdateBuffers()
        {
            var transformMats = colliders.Select(s => new TransformMat(s.transform)).ToList();
            // transformMats.AddRange(planes.Select(p => new TransformMat(p.transform)));

            _mats.SetData(transformMats);
            detectionLogic.SetBuffer(_kDetect.KernelID, "mat", _mats);
            detectionLogic.SetBuffer(_kTransform.KernelID, "mat", _mats);

            // TODO reset _collisionsBuffer each frame
        }

        public void AddCollider(Shape shape)
        {
            PolygonCollider p = shape.GetComponent<PolygonCollider>();
            // SphereCollider s = shape.GetComponent<SphereCollider>();

            if (p != null)
            {
                colliders.Insert(0, p);
            }
            // else if (s != null)
            // {
            //     colliders.Insert(0, s);
            // }
        }
    }
}