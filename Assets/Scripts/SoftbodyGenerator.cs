using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoftbodyGenerator : MonoBehaviour
{
    private MeshFilter originalMeshFilter;
    private List<Vector3> writableVertices {get; set;}
    private List<Vector3> writableVerticesConvaxed;
    private List<Vector3> writableNormals { get; set; }
    private List<Vector3> writableNormalsConvaxed;
    
    private List<SphereCollider> sphereColliders = new List<SphereCollider>();
    private List<int> writableTris { get; set; }
    private List<int> writableTrisConvaxed;
    private Mesh writableMesh;

    private List<GameObject> physicedVertexes;
    private new Dictionary<int, int> vertexDictionary;

    [Header("Softbody Settings")]
    public bool runOptimizedVersion = false;
    public float _collisionSurfaceOffset = 0.1f;
    public float collisionSurfaceOffset
    {
        get => _collisionSurfaceOffset;
        set
        {
            _collisionSurfaceOffset = value;
            if (physicedVertexes != null)
            {
                foreach (var sphereCollider in sphereColliders)
                {
                    sphereCollider.radius = _collisionSurfaceOffset;
                }
            }
        }
    }
    public SoftJointLimitSpring springLimit;
    public float _softness = 1.0f;
    public float softness
    {
        get => _softness;
        set
        {
            _softness = value;
            if (physicedVertexes != null)
            {
                foreach (var gObject in physicedVertexes)
                {
                    gObject.GetComponent<SpringJoint>().spring = _softness;
                }
            }
        }
    }
    public float _damp = 0.2f;
    public float damp
    {
        get => _damp;
        set
        {
            _damp = value;
            if (physicedVertexes != null)
            {
                foreach (var gObject in physicedVertexes)
                {
                    gObject.GetComponent<SpringJoint>().damper = _damp;
                }
            }
            springLimit.damper = _damp;
        }
    }
    public float _mass = 1.0f;
    public float mass
    {
        get => _mass;
        set
        {
            _mass = value;
            if (physicedVertexes != null)
            {
                foreach (var gObject in physicedVertexes)
                {
                    gObject.GetComponent<Rigidbody>().mass = _mass;
                }
            }
        }
    }
    private bool _debugMode = false;
    public bool debugMode
    {
        get => _debugMode;
        set
        {
            _debugMode = value;
            if (_debugMode == false)
            {
                if (physicedVertexes != null)
                {
                    foreach (var gObject in physicedVertexes)
                    {
                        gObject.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
                if (centerOfMasObj != null)
                {
                    centerOfMasObj.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            else
            {
                if (physicedVertexes != null)
                {
                    foreach (var gObject in physicedVertexes)
                    {
                        gObject.hideFlags = HideFlags.None;
                    }
                }
                if (centerOfMasObj != null)
                {
                    centerOfMasObj.hideFlags = HideFlags.None;
                }
            }
        }
    }
    private float _physicsRoughness = 4.0f;

    public float physicsRoughness
    {
        get => _physicsRoughness;
        set
        {
            _physicsRoughness = value;
            if (physicedVertexes != null)
            {
                foreach (var gObject in physicedVertexes)
                {
                    gObject.GetComponent<Rigidbody>().drag = _physicsRoughness;
                }
            }
        }
    }
    private bool _gravity = true;
    public bool gravity
    {
        get => _gravity;
        set
        {
            _gravity = value;
            if (physicedVertexes != null)
            {
                foreach (var gObject in physicedVertexes)
                {
                    gObject.GetComponent<Rigidbody>().useGravity = _gravity;
                }
            }
            if (centerOfMasObj != null)
            {
                centerOfMasObj.GetComponent<Rigidbody>().useGravity = _gravity;
            }
        }
    }
    public GameObject centerOfMasObj = null;

    private void Awake()
    {
        throw new NotImplementedException();
    }

    private void Update()
    {
        throw new NotImplementedException();
    }
}
