using System;
using System.Collections;
using System.Collections.Generic;
using GK;
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
    private int[] writableTris { get; set; }
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
        writableVertices = new List<Vector3>();
        writableVerticesConvaxed = new List<Vector3>();
        writableNormals = new List<Vector3>();
        writableNormalsConvaxed = new List<Vector3>();
        physicedVertexes = new List<GameObject>();
        
        writableTrisConvaxed = new List<int>();
        
        originalMeshFilter = GetComponent<MeshFilter>();
        originalMeshFilter.mesh.GetVertices(writableVertices);
        originalMeshFilter.mesh.GetNormals(writableNormals);
        writableTris = originalMeshFilter.mesh.triangles;

        var localWorld = transform.localToWorldMatrix;
        for (int i = 0; i < writableVertices.Count; ++i)
        {
            writableVertices[i] = localWorld.MultiplyPoint3x4(writableVertices[i]);
        }

        if (runOptimizedVersion)
        {
            new ConvexHullCalculator().GenerateHull(
                writableVertices,
                false,
                ref writableVerticesConvaxed,
                ref writableTrisConvaxed,
                ref writableNormalsConvaxed);

            writableVertices = writableVerticesConvaxed;
            writableNormals = writableNormalsConvaxed;
            writableTris = writableTrisConvaxed.ToArray();
        }
        
        writableMesh = new Mesh();
        writableMesh.MarkDynamic();
        writableMesh.SetVertices(writableVertices);
        writableMesh.SetNormals(writableNormals);
        writableMesh.triangles = writableTris;
        originalMeshFilter.mesh = writableMesh;

        var _optimizedVertex = new List<Vector3>();
        vertexDictionary = new Dictionary<int, int>();

        for (int i = 0; i < writableVertices.Count; ++i)
        {
            bool isVertexDuplicated = false;
            for (int j = 0; j < _optimizedVertex.Count; ++j)
            {
                if (_optimizedVertex[j] == writableVertices[i])
                {
                    isVertexDuplicated = true;
                    vertexDictionary.Add(i, j);
                    break;
                }
            }
            if (!isVertexDuplicated)
            {
                vertexDictionary.Add(i, _optimizedVertex.Count);
                _optimizedVertex.Add(writableVertices[i]);
            }
        }

        foreach (var vertex in _optimizedVertex)
        {
            var _tempObj = new GameObject("Point " + _optimizedVertex.IndexOf(vertex));

            if (!debugMode)
            {
                _tempObj.hideFlags = HideFlags.HideAndDontSave;
            }

            _tempObj.transform.parent = this.transform;
            _tempObj.transform.position = vertex;
            
            // Add SphereCollider to the center of the mass
            var sphereCollider = _tempObj.AddComponent<SphereCollider>() as SphereCollider;
            sphereCollider.radius = collisionSurfaceOffset;
            
            // Add current collider to Colliders list
            sphereColliders.Add(sphereCollider);
            
            // Add Rigidbody to the center of the mass
            var _tempRigidbody = _tempObj.AddComponent<Rigidbody>() as Rigidbody;
            centerOfMasObj = _tempObj;
        }

        foreach (var collider1 in sphereColliders)
        {
            foreach (var collider2 in sphereColliders)
            {
                Physics.IgnoreCollision(collider1, collider2, true);
            }
        }

        List<Vector2Int> tempListOfSprings = new List<Vector2Int>();
        bool isFirstTrisOfQuad = true;

        for (int i = 0; i < writableTris.Length; i += 3)
        {
            int index0 = vertexDictionary[writableTris[i]];
            int index1 = vertexDictionary[writableTris[i + 1]];
            int index2 = vertexDictionary[writableTris[i + 2]];
            
            tempListOfSprings.Add(new Vector2Int(index1, index2));
            if (isFirstTrisOfQuad)
            {
                tempListOfSprings.Add(new Vector2Int(index0, index1));
                isFirstTrisOfQuad = false;
            }
            else
            {
                tempListOfSprings.Add(new Vector2Int(index2, index0));
                isFirstTrisOfQuad = true;
            }
        }
        
        // Distinct normal duplicates with check reverse
        for (int i = 0; i < tempListOfSprings.Count; ++i)
        {
            bool isDuplicated = false;
            Vector2Int normal = tempListOfSprings[i];
            Vector2Int reversedNormal = new Vector2Int(tempListOfSprings[i].y, tempListOfSprings[i].x);

            for (int j = 0; j < noDupesListOfSprings.Count; ++j)
            {
                if (normal == tempListOfSprings[j])
                {
                    isDuplicated = true;
                    break;
                } else if (reversedNormal == tempListOfSprings[j])
                {
                    isDuplicated = true;
                    break;
                }
            }
            
            if (!isDuplicated)
            {
                noDupesListOfSprings.Add(normal);
            }
        }

        foreach (var jointIndex in noDupesListOfSprings)
        {
            var thisGameObject = physicedVertexes[jointIndex.x];
            var thisBodyJoint = thisGameObject.AddComponent<CharacterJoint>();
            var destinationBody = physicedVertexes[jointIndex.y].GetComponent<Rigidbody>();
            
            float distance = Vector3.Distance(thisGameObject.transform.position, destinationBody.transform.position);
            
            thisBodyJoint.connectedBody = destinationBody;
            SoftJointLimit jointLimitHigh = new SoftJointLimit();
            jointLimitHigh.bounciness = 1.1f;
            jointLimitHigh.contactDistance = distance;
            jointLimitHigh.limit = 10;
            
            SoftJointLimit jointLimitLow = new SoftJointLimit();
            jointLimitLow.bounciness = 1.1f;
            jointLimitLow.contactDistance = distance;
            jointLimitLow.limit = -10;
            
            thisBodyJoint.highTwistLimit = jointLimitHigh;
            thisBodyJoint.lowTwistLimit = jointLimitLow;
            thisBodyJoint.swing1Limit = jointLimitLow;
            thisBodyJoint.swing2Limit = jointLimitHigh;

            springLimit.damper = damp;
            springLimit.spring = softness;
            
            thisBodyJoint.swingLimitSpring = springLimit;
            thisBodyJoint.twistLimitSpring = springLimit;

            if (!runOptimizedVersion)
            {
                thisBodyJoint.enableCollision = true;
            }
        }

        foreach (var jointIndex in physicedVertexes)
        {
            var destinationBodyJoint = jointIndex.AddComponent<SpringJoint>();

            float distanceToCenterOfMass = Vector3.Distance(
                centerOfMasObj.transform.localPosition,
                destinationBodyJoint.transform.localPosition
                );
            
            destinationBodyJoint.connectedBody = centerOfMasObj.GetComponent<Rigidbody>();
            destinationBodyJoint.spring = softness;
            destinationBodyJoint.damper = damp;

            if (!runOptimizedVersion)
            {
                destinationBodyJoint.enableCollision = true;
            }
        }
    }
    List<Vector2Int> noDupesListOfSprings = new List<Vector2Int>();
    
    private void Update()
    {
        throw new NotImplementedException();
    }
}
