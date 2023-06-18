using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPSNode : MonoBehaviour
{
    [Header("Set In Inspector")]
    public List<NPSNode> connectedNodes;
    public float nodeOrbitDistance;
    public bool endPoint;
    public bool noSpawn;

    [Header("Debug")]
    [SerializeField]
    float gizmoObjScale = 1f;
    [SerializeField]
    bool recalcConnections = true,drawGizmos = false;
    public float[] distanceValues; // how much it "costs" to move to the node with the the same index
                                   // distancevalue[0] = distance between this & connectednodes[0]

    public float fScore { get { return gScore + hScore; } }

    public float gScore;
    public float hScore;

    public NPSNode prevNode;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Nodes");
        gameObject.AddComponent<SphereCollider>();
        if (endPoint) nodeOrbitDistance = 0f;
        else if (nodeOrbitDistance < 1f) nodeOrbitDistance = 1f;
    }

    private void Start()
    {
        if(connectedNodes.Count < 1)
        {
            Debug.Log("No connections removing node");
            Destroy(gameObject);
        }

        distanceValues = new float[connectedNodes.Count];
        for(int i = 0; i < connectedNodes.Count; i++)
        {
            distanceValues[i] = Vector3.Distance(transform.position, connectedNodes[i].transform.position);
            if (recalcConnections && !connectedNodes[i].connectedNodes.Contains(this)) 
            {
                connectedNodes[i].connectedNodes.Add(this);
                connectedNodes[i].EstablishDistanceValues();
            } 
        }
    }

    public void EstablishDistanceValues()
    {
        distanceValues = new float[connectedNodes.Count];
        for (int i = 0; i < connectedNodes.Count; i++)
        {
            distanceValues[i] = Vector3.Distance(transform.position, connectedNodes[i].transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Color c = new Color(1f, 0.92f, 0.016f, 0.05f);
            if (connectedNodes != null && connectedNodes.Count > 0)
            {
                Gizmos.color = c;
                for (int i = 0; i < connectedNodes.Count; i++)
                {
                    NPSNode n = connectedNodes[i];
                    if (n)
                    {
                        Gizmos.DrawLine(transform.position, (transform.position + n.transform.position) / 2);
                    }
                }
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.5f * gizmoObjScale);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color c = new Color(0, 1, 0, 1);
        if (connectedNodes != null && connectedNodes.Count > 0)
        {
            Gizmos.color = c;
            for (int i = 0; i < connectedNodes.Count; i++)
            {
                NPSNode n = connectedNodes[i];
                if (n)
                {
                    Gizmos.DrawLine(transform.position, (transform.position + n.transform.position) / 2);
                }
            }
        }
    }
}
