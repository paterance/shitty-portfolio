using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    [Header("Prefabs etc.")]
    [SerializeField]
    GameObject playerDependencies;
    [SerializeField]
    GameObject playerPhysicsPrefab;
    [SerializeField]
    GameObject customPlayerScripts;
    [SerializeField]
    GameObject[] shipPrefabs;
    [SerializeField]
    Transform spawnPoint;

    [HideInInspector]
    public bool playerInit = false;
    [Tooltip("Saving and loading. Number in the shipprefabs array.")]
    public int storedShip;

    [Header("NPS")]
    public NPSNode[] nodes;
    [SerializeField]
    List<NPSNode> endNodes = new List<NPSNode>();
    [SerializeField]
    List<NPSNode> spawnNodes = new List<NPSNode>();
    [SerializeField]
    int maxIdleNPSs;
    public GameObject nonPlayerShipPrefab;
    public float npsSpawnDist = 1000f;
    LayerMask nodeLm;
    [SerializeField]
    GameObject[] idlenpsses;
    bool npsArrayIsFilled = false;
    [SerializeField]
    float npsSpawnRate = 1f;
    float npsSpawnTimer = 0f;
    [SerializeField, Tooltip("example: 0.1 = 10 chance for a idle ship to retaliate against player attacks")]
    float npsIdleHostilityPercent;
    [SerializeField]
    bool spawnIdleNPS = true;

    [Header("READ ONLY")]
    public GameObject activeShip;
    public GameObject playerObj;
    public Transform playerTrans;
    public GameObject playerSub;

    [SerializeField]
    bool debugMode;
    void Awake()
    {
        if (instance != null && instance != this) Destroy(gameObject);
        else instance = this;

        nodeLm = LayerMask.NameToLayer("Nodes");
        idlenpsses = new GameObject[maxIdleNPSs];
    }

    private void Start()
    {
        nodes = FindObjectsOfType<NPSNode>();
        GetNodeTypes();
    }

    void Update()
    {
        if(activeShip != null)
        {
            if (!playerInit)
            {
                //player init
            }
            else
            {
                //populate area near the player with ships
                if (spawnIdleNPS)
                {
                    npsSpawnTimer += Time.deltaTime;
                    if(npsSpawnTimer >= npsSpawnRate)
                    {
                        npsSpawnTimer = 0;

                        //resize nps array
                        if (maxIdleNPSs != idlenpsses.Length) 
                        {
                            idlenpsses = new GameObject[maxIdleNPSs];
                            NPSController[] npsses = FindObjectsOfType<NPSController>();
                            for(int i = 0;i < npsses.Length; i++)
                            {
                                NPSController go = npsses[i];
                                if (!go.vip)
                                {
                                    if (!npsArrayIsFilled)
                                    {
                                        int x = FindAvailablePosInNPSArray();
                                        if (x >= 0) idlenpsses[x] = go.gameObject;
                                    }
                                    else Destroy(npsses[i].gameObject);
                                }
                                else continue;
                            }
                        }

                        if (!npsArrayIsFilled)
                        {
                            int x = FindAvailablePosInNPSArray();
                            if (x >= 0)
                            {
                                List<NPSNode> applicableNodes = GetNodesNearPlayer();
                                NPSNode nod = applicableNodes[Random.Range(0, applicableNodes.Count - 1)];
                                NPSNode end = RandomEndTarget();
                                GameObject go = Instantiate(nonPlayerShipPrefab, GetPosNearNode(nod), Random.rotation);
                                idlenpsses[x] = go;

                                bool b;
                                if (Random.value > 1f - npsIdleHostilityPercent) b = true;
                                else b = false;
                                go.GetComponent<NPSController>().Setup(nod, end, false, b);
                            }
                        }
                        
                    }
                }
            }
        }
        else if (debugMode)
        {
            if (spawnIdleNPS)
            {
                npsSpawnTimer += Time.deltaTime;
                if (npsSpawnTimer >= npsSpawnRate)
                {
                    npsSpawnTimer = 0;
                    NPSNode nod = RandomSpawnTarget();
                    NPSNode end = RandomEndTarget();
                    GameObject go = Instantiate(nonPlayerShipPrefab, GetPosNearNode(nod), Random.rotation);

                    int x = FindAvailablePosInNPSArray();
                    if(x >= 0) idlenpsses[x] = go;

                    bool b;
                    if (Random.value > 1f - npsIdleHostilityPercent) b = true;
                    else b = false;
                    go.GetComponent<NPSController>().Setup(nod, end, false, b);
                }
            }
        }
    }

    public void RecycleNPS(NPSController go)
    {
        go.gameObject.SetActive(false);

        List<NPSNode> applicableNodes = GetNodesNearPlayer();
        NPSNode nod = applicableNodes[Random.Range(0, applicableNodes.Count - 1)];
        NPSNode end = RandomEndTarget();

        bool b;
        if (Random.value > npsIdleHostilityPercent) b = true;
        else b = false;

        go.transform.position = GetPosNearNode(nod);
        go.transform.LookAt(nod.transform);
        go.Setup(nod, end, false, b);

        go.gameObject.SetActive(true);
    }

    int FindAvailablePosInNPSArray()
    {
        for(int i = 0;i < idlenpsses.Length; i++)
        {
            if (!idlenpsses[i]) return i;
        }
        npsArrayIsFilled = true;
        return -1;
    }

    Vector3 GetPosNearNode(NPSNode target)
    {
        Vector3 pos = Vector3.Normalize(new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)));
        if (Random.value >= 0.2f) pos = ConnectionMidpoint(target,pos);
        else pos = pos * target.nodeOrbitDistance + target.transform.position ;

        return pos;
    }

    Vector3 ConnectionMidpoint(NPSNode node, Vector3 offset)
    {
        NPSNode pair = node.connectedNodes[Random.Range(0, node.connectedNodes.Count - 1)];
        float percent = Random.value;
        Vector3 pos = Vector3.Lerp(node.transform.position, pair.transform.position, percent);
        return pos + offset;
    }

    List<NPSNode> GetNodesNearPlayer()
    {
        Collider[] cols = Physics.OverlapSphere(activeShip.transform.position, npsSpawnDist);
        List<NPSNode> nearNodes = new List<NPSNode>();
        if (cols.Length > 0)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                NPSNode n = cols[i].GetComponent<NPSNode>();
                if (n && !n.endPoint && !n.noSpawn) nearNodes.Add(n);
            }
        }

        return nearNodes;
    }

    void GetNodeTypes()
    {
        for(int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].endPoint) endNodes.Add(nodes[i]);
            else if (!nodes[i].noSpawn) spawnNodes.Add(nodes[i]);
        }
    }

    public GameObject SpawnMissionNPS(bool hostility, bool moving)
    {
        NPSNode n = RandomSpawnTarget();
        NPSNode n2 = RandomSpawnTarget();
        GameObject go = Instantiate(nonPlayerShipPrefab, GetPosNearNode(n), Random.rotation);
        go.GetComponent<NPSController>().Setup(n, n2, true, hostility);
        return go;
    }

    public GameObject SpawnMissionNPS(bool hostility, bool moving, int quantity)
    {
        if (quantity < 2) quantity = 2;

        NPSNode n = RandomSpawnTarget();
        NPSNode n2 = RandomSpawnTarget();
        
        GameObject gox = Instantiate(nonPlayerShipPrefab, GetPosNearNode(n), Random.rotation);
        NPSController npscGox = gox.GetComponent<NPSController>();
        npscGox.Setup(n, n2, true,hostility);
        for (int i = 1; i < quantity; i++)
        {
            GameObject go = Instantiate(nonPlayerShipPrefab, GetPosNearNode(n), Random.rotation);
            go.GetComponent<NPSController>().Setup(npscGox);
        }

        return gox;
    }

    public NPSNode RandomSpawnTarget()
    {
        return spawnNodes[Random.Range(0, spawnNodes.Count - 1)];
    }

    public NPSNode RandomEndTarget()
    {
        return endNodes[Random.Range(0, endNodes.Count - 1)];
    }
}
