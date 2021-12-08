using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Shapes;
using UnityEngine;

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
            vertex = halfEdge.Vertex;
            local_vertex = halfEdge.Vertex - halfEdge.Transform.position;
            edge = halfEdge.Edge;
            n1 = halfEdge.Face.Normal;
            n2 = halfEdge.Twin.Face.Normal;
        }
    }

    struct FaceInfo
    {
        Vector3 normal;
        Vector3 center;

        public FaceInfo(Face face)
        {
            normal = face.Normal;
            center = face.Center;
        }
    }

    struct ShapeInfo
    {
        Vector2Int pointInfo;
        Matrix4x4 R;
        Matrix4x4 T;

        public ShapeInfo(Transform t, Vector2Int p)
        {
            pointInfo = p;
            R = Matrix4x4.Rotate(t.rotation);
            T = Matrix4x4.Translate(t.position);
        }
    }

    #endregion

    public class GPUCollideManager : MonoBehaviour
    {
        #region Buffers

        private ComputeBuffer _halfEdgesBuffer;
        private ComputeBuffer _facesBuffer;
        private ComputeBuffer _shapesBuffer;
        private ComputeBuffer _pointsBuffer;
        private ComputeBuffer _halfEdgeInfoBuffer;
        private ComputeBuffer _faceInfoBuffer;
        private ComputeBuffer _collisionsBuffer;

        #endregion

        [SerializeField] private List<Shape> colliders;

        [SerializeField] private ComputeShader detectionLogic;

        private Kernel kDetect;


        //DEBUG

        [SerializeField] private Vector2[] results;


        private void Reset()
        {
            // detectionLogic = Resources.Load<ComputeShader>("Shaders/CollisionDetection");
        }

        private void Awake()
        {
            // kDetect = new Kernel(detectionLogic, "PolygonVsPolygon");
        }

        private void Start()
        {
            detectionLogic = Resources.Load<ComputeShader>("Shaders/CollisionDetection");
            kDetect = new Kernel(detectionLogic, "CollisionDetection");

            var polygonColliders = FindObjectsOfType<Cube>();
            colliders.AddRange(polygonColliders);

            results = new Vector2[colliders.Count * colliders.Count];

            InitializeBuffers();

            detectionLogic.SetInt("objectsNo", colliders.Count);
            detectionLogic.SetBuffer(kDetect.KernelID, "halfedgeInfo", _halfEdgeInfoBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "faceInfo", _faceInfoBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "halfedges", _halfEdgesBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "faces", _facesBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "shapes", _shapesBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "points", _pointsBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "collisions", _collisionsBuffer);
        }

        private void OnDisable()
        {
            colliders.Clear();

            // release buffers and make them null
            _halfEdgesBuffer.Release();
            _halfEdgesBuffer = null;

            _facesBuffer.Release();
            _facesBuffer = null;

            _shapesBuffer.Release();
            _shapesBuffer = null;

            _pointsBuffer.Release();
            _pointsBuffer = null;

            _halfEdgeInfoBuffer.Release();
            _halfEdgeInfoBuffer = null;

            _faceInfoBuffer.Release();
            _faceInfoBuffer = null;

            _collisionsBuffer.Release();
            _collisionsBuffer = null;
        }

        private void Update()
        {
            UpdateBuffers();

            detectionLogic.SetBuffer(kDetect.KernelID, "halfedges", _halfEdgesBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "faces", _facesBuffer);
            detectionLogic.SetBuffer(kDetect.KernelID, "shapes", _shapesBuffer);

            detectionLogic.Dispatch(kDetect.KernelID, colliders.Count, colliders.Count, 1);


            _collisionsBuffer.GetData(results);

            foreach (var col in colliders)
            {
                col.GetComponent<Collider>().Collides(false);
            }

            foreach (var elm in results)
            {
                if (elm.x >= 0 && elm.x < colliders.Count)
                {
                    colliders[(int) elm.x].GetComponent<Collider>().Collides(true);
                }
            }
        }

        private void InitializeBuffers()
        {
            List<Vector3> points = new List<Vector3>();
            List<ShapeInfo> shapesInfo = new List<ShapeInfo>();
            List<Vector2Int> heOffsets = new List<Vector2Int>();
            List<Vector2Int> faceOffsets = new List<Vector2Int>();
            List<HalfEdgeInfo> halfEdgesInfo = new List<HalfEdgeInfo>();
            List<FaceInfo> facesInfo = new List<FaceInfo>();

            var hC = 0;
            var fC = 0;
            var sC = 0;

            var sz = 0;

            foreach (var s in colliders)
            {
                sz += 1;

                {
                    halfEdgesInfo.AddRange(s.HalfEdges.Select(he => new HalfEdgeInfo(he)));

                    var halfEdges = s.HalfEdges.Count;
                    heOffsets.Add(new Vector2Int(hC, halfEdges));
                    hC += halfEdges;
                }

                {
                    facesInfo.AddRange(s.Faces.Select(f => new FaceInfo(f)));

                    var faces = s.Faces.Count;
                    faceOffsets.Add(new Vector2Int(fC, faces));
                    fC += faces;
                }

                {
                    points.AddRange(s.MeshInfo.vertexPosition);

                    var pts = s.MeshInfo.vertexPosition.Length;
                    shapesInfo.Add(new ShapeInfo(s.transform, new Vector2Int(sC, pts)));
                    sC += pts;
                }
            }

            _pointsBuffer = new ComputeBuffer(points.Count, 12);
            _pointsBuffer.SetData(points);

            _shapesBuffer = new ComputeBuffer(shapesInfo.Count, Marshal.SizeOf(typeof(ShapeInfo)));
            _shapesBuffer.SetData(shapesInfo);

            _halfEdgeInfoBuffer = new ComputeBuffer(heOffsets.Count, Marshal.SizeOf(typeof(Vector2Int)));
            _halfEdgeInfoBuffer.SetData(heOffsets);

            _faceInfoBuffer = new ComputeBuffer(faceOffsets.Count, Marshal.SizeOf(typeof(Vector2Int)));
            _faceInfoBuffer.SetData(faceOffsets);

            _halfEdgesBuffer = new ComputeBuffer(halfEdgesInfo.Count, Marshal.SizeOf(typeof(HalfEdgeInfo)));
            _halfEdgesBuffer.SetData(halfEdgesInfo);

            _facesBuffer = new ComputeBuffer(facesInfo.Count, Marshal.SizeOf(typeof(FaceInfo)));
            _facesBuffer.SetData(facesInfo);

            _collisionsBuffer = new ComputeBuffer(sz * sz, Marshal.SizeOf(typeof(Vector2)));
            _collisionsBuffer.SetData(new List<Vector2>(sz * sz));
        }

        private void UpdateBuffers()
        {
            List<HalfEdgeInfo> halfEdgesInfo = new List<HalfEdgeInfo>();
            List<FaceInfo> facesInfo = new List<FaceInfo>();
            List<ShapeInfo> shapesInfo = new List<ShapeInfo>();


            var sC = 0;

            foreach (var s in colliders)
            {
                halfEdgesInfo.AddRange(s.HalfEdges.Select(he => new HalfEdgeInfo(he)));
                facesInfo.AddRange(s.Faces.Select(f => new FaceInfo(f)));
                {
                    var pts = s.MeshInfo.vertexPosition.Length;
                    shapesInfo.Add(new ShapeInfo(s.transform, new Vector2Int(sC, pts)));
                    sC += pts;
                }
            }

            _facesBuffer.SetData(facesInfo);
            _halfEdgesBuffer.SetData(halfEdgesInfo);
            _shapesBuffer.SetData(shapesInfo);
        }
    }
}