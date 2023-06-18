using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
//used unitys old navmesh components for AI

[RequireComponent(typeof(MeshFilter))]
public class MapGenerator : MonoBehaviour
{

    Mesh mesh;

    List<Vector3> holdingPos = new List<Vector3>();
    Vector3[] vertices;
    int[] triangles;

    public float seaLevel;
    public int mSize = 32;
    Vector3 centerPoint;

    public GameObject seaObject;
    public GameObject wallObject;
    //NavMeshSurface navSurface;
    //NavMeshModifierVolume seaVolume;

    [SerializeField]
    float addToFort;
    [SerializeField]
    GameObject holdingEmpty;
    [SerializeField]
    GameObject holdingFort;
    [SerializeField]
    GameObject holdingFarm;
    [SerializeField]
    GameObject holdingTown;
    [SerializeField]
    List<GameObject> trees;

    [SerializeField]
    GameObject playerPrefab;
    //DateAndTime timeSys;

    void SetInitialReferences()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        //navSurface = GetComponent<NavMeshSurface>();
        //seaVolume = gameObject.AddComponent<NavMeshModifierVolume>();

        seaLevel = Random.Range(0.68f, 0.80f);

        if (addToFort <= 0) addToFort = Random.Range(0.4f, 0.5f);
        addToFort += seaLevel;

        centerPoint = new Vector3(mSize / 2, seaLevel, mSize / 2);

        //timeSys = FindObjectOfType<DateAndTime>();
    }

	void Start ()
    {
        SetInitialReferences();

        CreateShape();
        UpdateMesh();
        CreateSeaMesh();
        CreateWalls();
        SetupGameElements();
	}
	
    void CreateShape()
    {
        vertices = new Vector3[(mSize + 1) * (mSize + 1)];

        //create the verts
        for(int i = 0, z = 0; z <= mSize; z++)
        {
            for (int x = 0; x <= mSize; x++)
            {
                float y = Mathf.PerlinNoise(x * Random.Range(.3f, .4f), z * Random.Range(.3f, .4f)) * Random.Range(1.8f,2.1f);
                vertices[i] = new Vector3(x, y, z);
                if (vertices[i].x > 0f && vertices[i].z > 0f && vertices[i].x < mSize && vertices[i].z < mSize)
                {
                    if (vertices[i].y > seaLevel + 0.1f)
                    {
                        holdingPos.Add(vertices[i]);
                    }
                }
                
                i++;
            }
        }

        //Make the triangles
        triangles = new int[mSize * mSize * 6];

        for(int vert = 0, tris = 0,z = 0;z < mSize; z++)
        {
            for (int x = 0; x < mSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + mSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + mSize + 1;
                triangles[tris + 5] = vert + mSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }



    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void CreateSeaMesh()
    {
        if (seaObject != null)
        {
            Vector3[] seaVerts;
            int[] seaTris;
            GameObject go = Instantiate(seaObject, transform);
            Mesh seaMesh = new Mesh();
            go.GetComponent<MeshFilter>().mesh = seaMesh;

            seaVerts = new Vector3[4];
            seaVerts[0] = new Vector3(0, seaLevel, 0);
            seaVerts[1] = new Vector3(mSize, seaLevel, 0);
            seaVerts[2] = new Vector3(mSize, seaLevel, mSize);
            seaVerts[3] = new Vector3(0, seaLevel, mSize);
            seaTris = new int[6];
            seaTris[0] = 0;
            seaTris[1] = 3;
            seaTris[2] = 2;
            seaTris[3] = 2;
            seaTris[4] = 1;
            seaTris[5] = 0;

            seaMesh.Clear();

            seaMesh.vertices = seaVerts;
            seaMesh.triangles = seaTris;

            seaMesh.RecalculateNormals();
            go.GetComponent<MeshCollider>().sharedMesh = seaMesh;
        }
        else Debug.Log("Noe sea man");
    }

    void CreateWalls()
    {
        if (wallObject != null)
        {
            Vector3[] wVerts;
            int[] wTris;
            GameObject go = Instantiate(wallObject, transform);
            Mesh wMesh = new Mesh();
            go.GetComponent<MeshFilter>().mesh = wMesh;

            float floor = seaLevel - 3f, ceiling = seaLevel + 1.5f;
            wVerts = new Vector3[12];
            //vertical verts
            wVerts[0] = new Vector3(0, floor, 0);
            wVerts[1] = new Vector3(0, ceiling, 0);
            wVerts[2] = new Vector3(0, ceiling, mSize);
            wVerts[3] = new Vector3(0, floor, mSize);
            wVerts[4] = new Vector3(mSize, floor, mSize);
            wVerts[5] = new Vector3(mSize, ceiling, mSize);
            wVerts[6] = new Vector3(mSize, ceiling, 0);
            wVerts[7] = new Vector3(mSize, floor, 0);
            //farverts
            wVerts[8] = new Vector3(-5, ceiling, -5);
            wVerts[9] = new Vector3(-5, ceiling, mSize + 5);
            wVerts[10] = new Vector3(mSize + 5, ceiling, mSize + 5);
            wVerts[11] = new Vector3(mSize + 5, ceiling, -5);

            wTris = new int[48];
            //front
            wTris[0] = 0;
            wTris[1] = 1;
            wTris[2] = 2;
            wTris[3] = 2;
            wTris[4] = 3;
            wTris[5] = 0;

            //right
            wTris[6] = 3;
            wTris[7] = 2;
            wTris[8] = 5;
            wTris[9] = 5;
            wTris[10] = 4;
            wTris[11] = 3;

            //back
            wTris[12] = 4;
            wTris[13] = 5;
            wTris[14] = 6;
            wTris[15] = 6;
            wTris[16] = 7;
            wTris[17] = 4;

            //left
            wTris[18] = 7;
            wTris[19] = 6;
            wTris[20] = 1;
            wTris[21] = 1;
            wTris[22] = 0;
            wTris[23] = 7;

            //front top
            wTris[24] = 1;
            wTris[25] = 8;
            wTris[26] = 9;
            wTris[27] = 9;
            wTris[28] = 2;
            wTris[29] = 1;

            //right top
            wTris[30] = 2;
            wTris[31] = 9;
            wTris[32] = 10;
            wTris[33] = 10;
            wTris[34] = 5;
            wTris[35] = 2;

            //back top
            wTris[36] = 5;
            wTris[37] = 10;
            wTris[38] = 11;
            wTris[39] = 11;
            wTris[40] = 6;
            wTris[41] = 5;

            //left top
            wTris[42] = 6;
            wTris[43] = 11;
            wTris[44] = 8;
            wTris[45] = 8;
            wTris[46] = 1;
            wTris[47] = 6;

            wMesh.Clear();

            wMesh.vertices = wVerts;
            wMesh.triangles = wTris;

            wMesh.RecalculateNormals();
            go.GetComponent<MeshCollider>().sharedMesh = wMesh;
        }
        else Debug.Log("Noe walls man");
    }

    IEnumerator SpawnForest()
    {
        //spawn trees (fix array size)
        Vector3[] treeLocations = new Vector3[(mSize * 3) * (mSize * 3) + (mSize * 3 * 2 + 1)];
        LayerMask layers = LayerMask.GetMask("Terrain", "Holdings");
        for (int i = 0, z = 0; z <= mSize*3; z++)
        {
            for (int x = 0, c = 0; x <= mSize*3; x++)
            {
                float y = Mathf.PerlinNoise(x * Random.Range(.2f, .4f), z * Random.Range(.2f, .4f)) * Random.Range(1.5f, 2.1f);
                if(y >= 1f)
                {
                    Vector3 randomizer = new Vector3(Random.Range(-0.05f, 0.05f), 0f, Random.Range(-0.05f, 0.05f));
                    treeLocations[i] = new Vector3(x / 3f, 10f, z / 3f);
                    treeLocations[i] += randomizer;
                    if (Physics.Raycast(treeLocations[i], Vector3.down, out RaycastHit hitInfo, 20f, layers))
                    {
                        if (hitInfo.point.x > 0f && hitInfo.point.z > 0f && hitInfo.point.x < mSize && hitInfo.point.z < mSize)
                        {
                            if(hitInfo.collider.gameObject.layer != 10)
                            {
                                if (hitInfo.point.y > seaLevel + 0.1f)
                                {
                                    Vector3 lookDir = Vector3.Normalize(new Vector3(Random.Range(-.7f, .7f), 0f, Random.Range(-.7f, .7f)));
                                    GameObject randomTree = trees[Random.Range(0, trees.Count)];
                                    GameObject go = Instantiate(randomTree, hitInfo.point, Quaternion.LookRotation(lookDir), transform);
                                    //timeSys.sceneEnvRenderers.Add(go.GetComponentInChildren<Renderer>());
                                    c++;
                                    if(c >= 5)
                                    {
                                        c = 0;
                                        yield return new WaitForSeconds(0.001f);
                                    }
                                }
                            }
                        }
                    }
                }

                i++;
            }
        }
        //timeSys.sceneEnvRenderers.Add(GetComponent<Renderer>());
        //timeSys.enabled = true;
    }

    void SetupGameElements()
    { 
        // navmesh from waterareas
        //seaVolume.size = new Vector3(mSize, .025f, mSize);
        //seaVolume.center = centerPoint;
        //seaVolume.area = 3;
        // build navmesh
        //navSurface.BuildNavMesh();
        //randomly place prebuilt holdings
        bool hasJustPlacedOne = false;
        for(int h = 0;h < holdingPos.Count; h++)
        {
            if (!hasJustPlacedOne)
            {
                float perChance = Random.value;
                //fortresses
                if (Mathf.PerlinNoise(holdingPos[h].x * 0.3f, holdingPos[h].z * 0.3f) * 2f > addToFort)
                {
                    SpawnHolding(holdingFort, holdingPos[h]);
                    hasJustPlacedOne = true;
                }
                //farms
                else if (perChance > 0.5f)
                {
                    SpawnHolding(holdingFarm, holdingPos[h]);
                    hasJustPlacedOne = true;
                }
                //towns
                else if (perChance > 0.3f)
                {
                    SpawnHolding(holdingTown, holdingPos[h]);
                    hasJustPlacedOne = true;
                }
                //everything else is empty
                else
                {
                    SpawnHolding(holdingEmpty, holdingPos[h]);
                }

            }
            else
            {
                SpawnHolding(holdingEmpty, holdingPos[h]);
                hasJustPlacedOne = false;
            }
        }
        StartCoroutine("SpawnForest");
        
        //spawn player prefab
    }

    void SpawnHolding(GameObject hold,Vector3 pos)
    {
        Vector3 lookDir = Vector3.Normalize(new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
        GameObject go = Instantiate(hold, pos, Quaternion.LookRotation(lookDir), transform);
        //if(go.GetComponentInChildren<Renderer>() != null) timeSys.sceneEnvRenderers.Add(go.GetComponentInChildren<Renderer>());
    }
}
