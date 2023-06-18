using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPSController : MonoBehaviour
{
    [SerializeField]
    float thrustPower, triggerStatePower, maxThrustModifier;
    [SerializeField]
    float rotationRate, triggerStateRotationRate;
    [SerializeField]
    float distanceThreshold, minOrbitDistance = 30f, attackRange = 150f;
    [SerializeField, Tooltip("Mesh, can also have collider")]
    GameObject gfx;
    [SerializeField]
    float fireDeviationPercent = 0.02f, fireRate = 1f,multiFireOffset = 0.2f;
    [SerializeField]
    int gunDamage = 1, shotsPerFire = 3;
    [SerializeField]
    Transform[] npsGun;
    [SerializeField]
    LayerMask playerMask;
    [SerializeField]
    GameObject impactVFXPrefab,lineEffectPrefab;
    [SerializeField]
    float aggroDuration = 20f;
    float aggroTimer;

    Rigidbody rb;
    Transform playerRef;
    float distanceLimit { get { return GameManager.Instance.npsSpawnDist; } }
    float refreshRate = 3f;
    float tic = 0;

    [Header("Debug")]
    public int id;
    public List<NPSNode> path = new List<NPSNode>();
    [SerializeField]
    NPSNode lastTarget, lstKLoc;
    bool manualPath = false;
    public bool followPath;
    public bool vip;
    bool attemptRecalcPath;
    bool demoted;
    bool rightHanded;
    float preferedDistance;
    [SerializeField]
    bool drawGizmos = true;
    [SerializeField]
    bool hostile, triggered;
    float shot;
    int shipCondition { get { return shipDurability - shipDamage; } }
    int shipDurability, shipDamage;

    NPSController parentNPS;
    List<NPSController> goons = new List<NPSController>();
    Mission myMissionRef;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        thrustPower *= Random.Range(1 - maxThrustModifier, 1 + maxThrustModifier);


        id = Random.Range(0, 99999);
        if (id < 10) name = "0000" + id.ToString();
        else if(id < 100) name = "000" + id.ToString();
        else if (id < 1000) name = "00" + id.ToString();
        else if (id < 10000) name = "0" + id.ToString();
        else name = id.ToString();

        float rand = Random.value;
        if (rand >= 0.5f) rightHanded = true;
        else rightHanded = false;

        preferedDistance = Random.Range(minOrbitDistance, attackRange);
        demoted = false;
        triggered = false;
        shipDurability = 100;
        shipDamage = 0;

        if (gunDamage <= 0) gunDamage = 1;
        if (shotsPerFire <= 0) shotsPerFire = 1;
    }

    NPSNode CalcNearestNode()
    {
        NPSNode[] ens = GameManager.Instance.nodes;
        NPSNode nearest = ens[0];
        float distA = Vector3.Distance(transform.position, nearest.transform.position);
        float distB;
        for(int i = 1; i < ens.Length; i++)
        {
            distB = Vector3.Distance(transform.position, ens[i].transform.position);
            if (distB < distA)
            {
                nearest = ens[i];
                distA = distB;
            }
        }
        return nearest;
    }

    public void Setup(NPSNode current, NPSNode target,bool vipState, bool hostilityState)
    {
        path = GeneratePath(current, target);
        lstKLoc = current;
        lastTarget = target;
        manualPath = true;
        vip = vipState;
        if (vip) 
        {
            gameObject.name += " (vip)";
            SphereCollider trig = gameObject.AddComponent<SphereCollider>();
            trig.radius = 30f;
            trig.isTrigger = true;
        } 
        hostile = hostilityState;

        if (path.Count > 0 && path[1]) transform.LookAt(path[1].transform);
    }

    public void Setup(NPSController papa)
    {
        gameObject.name += " (goon)";
        manualPath = true;
        followPath = false;
        vip = true;
        hostile = true;

        parentNPS = papa;

        SphereCollider trig = gameObject.AddComponent<SphereCollider>();
        trig.radius = 50f;
        trig.isTrigger = true;
    }

    public void SetMissionRef(Mission mission)
    {
        myMissionRef = mission;
    }

    private void Start()
    {
        if (!lstKLoc) lstKLoc = CalcNearestNode();
        if (!manualPath && path.Count < 1)
        {
            NPSNode n = GameManager.Instance.RandomEndTarget();
            path = GeneratePath(lstKLoc, n);
        }

        if (GameManager.Instance.activeShip)
        {
            playerRef = GameManager.Instance.activeShip.transform;
            StartCoroutine(WaitForLookAway());
        }
    }

    IEnumerator WaitForLookAway()
    {
        while (!gfx.activeSelf)
        {
            if(Vector3.Distance(transform.position,playerRef.position) > 20f)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
                float xPos = screenPos.x;
                float yPos = screenPos.y;
                float zPos = screenPos.z;
                if (zPos < 0) if (xPos < 0 || xPos > Screen.width) if (yPos < 0 || yPos > Screen.height)
                {
                    gfx.SetActive(true);
                    break;
                }
            }
            
            yield return new WaitForEndOfFrame();
        }
    }

    void Update()
    {
        if (triggered)
        {
            aggroTimer += Time.deltaTime;
            if(aggroTimer >= aggroDuration) //check aggro
            {
                aggroTimer = 0f;
                //Debug.Log(Vector3.Distance(transform.position, playerRef.position));
                if (Vector3.Distance(transform.position, playerRef.position) > 250f) triggered = false; // if player/target is too far
                else if (parentNPS && Vector3.Distance(transform.position, parentNPS.transform.position) > 150f) triggered = false; //if vip is too far
            }

            if (hostile) AttackMove(playerRef);
            else FleeFrom(playerRef);
        }
        else if (followPath && path != null && path.Count > 0) FollowPath();
        else if (parentNPS != null) FollowAtDistance(parentNPS.transform);

        if (tic >= refreshRate) 
        {
            tic = 0;
            if (attemptRecalcPath)
            {
                attemptRecalcPath = false;
                path = GeneratePath(lstKLoc, GameManager.Instance.RandomEndTarget());
            }

            if (!vip || demoted)
            {
                if (GameManager.Instance.activeShip && Vector3.Distance(transform.position, playerRef.position) > distanceLimit)
                {
                    if (demoted) Destroy(gameObject);
                    else GameManager.Instance.RecycleNPS(this);
                }
                    
                
            }
            
        }
        tic += Time.deltaTime;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered)
        {
            if (other.tag == "Player")
            {
                triggered = true;
                aggroTimer = 0f;

                if (goons.Count > 0) TriggerGoons();
            }
        }
    }

    void TriggerGoons()
    {
        if(goons.Count > 0)
        {
            for(int i = 0; i < goons.Count; i++)
            {
                if (!goons[i].triggered) 
                {
                    goons[i].triggered = true;
                    goons[i].aggroTimer = 0f;
                } 
            }
        }
    }

    void DemoteGoons()
    {
        if (goons.Count > 0)
        {
            for (int i = 0; i < goons.Count; i++)
            {
                goons[i].demoted = true;
            }
        }
    }

    void FollowPath()
    {
        if (Vector3.Distance(path[0].transform.position, transform.position) < distanceThreshold + path[0].nodeOrbitDistance)
        {
            if (path[0] == path[path.Count - 1]) DockNDie();
            lstKLoc = path[0];
            path.RemoveAt(0);
        }

        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, path[0].transform.position - transform.position, rotationRate * Time.deltaTime, 0f), path[0].transform.up);
        rb.AddForce(transform.forward * thrustPower);
    }

    void FleeFrom(Transform target)
    {
        Vector3 trgDir = -(target.position - transform.position).normalized;

        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, trgDir, triggerStateRotationRate * Time.deltaTime, 0f), path[0].transform.up);
        rb.AddForce(trgDir * triggerStatePower);
    }

    void FollowAtDistance(Transform target)
    {
        Vector3 projectedPosition = target.TransformPoint((transform.position - target.position).normalized * (preferedDistance + 10f));
        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, target.forward, rotationRate * Time.deltaTime, 0f), path[0].transform.up);
        rb.AddForce((projectedPosition - transform.position).normalized * thrustPower);
    }

    void AttackMove(Transform target)
    {
        Vector3 projectedPosition = target.TransformPoint((transform.position - target.position).normalized * preferedDistance);
        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, (target.position - transform.position).normalized, triggerStateRotationRate * Time.deltaTime, 0f), path[0].transform.up);

        float dist = Vector3.Distance(transform.position, target.position);

        
        if (dist < attackRange) 
        {
            shot += Time.deltaTime;

            if (shot > fireRate && Quaternion.Angle(transform.rotation, Quaternion.LookRotation((target.position - transform.position).normalized,transform.up)) < 10f)
            {
                StartCoroutine(BurstFire());
                shot = 0f;
            }
        }

        if (dist < preferedDistance + 1f) 
        {
            if (rightHanded) rb.AddForce(transform.right * thrustPower);
            else rb.AddForce(-transform.right * thrustPower);
        }
        else rb.AddForce((projectedPosition - transform.position).normalized * triggerStatePower);
    }

    IEnumerator BurstFire()
    {
        for(int i = 0; i < shotsPerFire; i++)
        {
            yield return new WaitForSeconds(multiFireOffset);
            FireWeapons();
        }
    }

    void FireWeapons()
    {
        for(int i = 0; i < npsGun.Length; i++)
        {
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            Vector3 deviation = Vector3.Lerp(npsGun[i].forward, randomDir, fireDeviationPercent);

            //weapon shot
            Vector3[] points = new Vector3[2];
            RaycastHit hit;
            //mask = LayerMask.NameToLayer("RigidbodyMatters");
            if (Physics.Raycast(npsGun[i].position, deviation, out hit, 1000f, playerMask, QueryTriggerInteraction.Ignore))
            {
                GameObject go = hit.collider.gameObject;
                points[1] = hit.point;
                Rigidbody rig = go.GetComponent<Rigidbody>();
                if (rig != null) rig.AddForce(npsGun[i].forward * 5f, ForceMode.Impulse);
                Instantiate(impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (go.tag == "Player")
                {
                    //deal damage
                    go.GetComponent<ShipInfo>().ReceiveDamage(gunDamage);
                }
            }
            else
            {
                //Debug.Log("Hit Nothing");
                points[1] = npsGun[i].position + npsGun[i].forward * 1000f;
            }
            LineRenderer lR = Instantiate(lineEffectPrefab, npsGun[i]).GetComponent<LineRenderer>();
            lR.positionCount = 2;
            points[0] = lR.transform.InverseTransformPoint(npsGun[i].position);
            points[1] = lR.transform.InverseTransformPoint(points[1]);
            lR.SetPositions(points);
        }
        
        shot = 0f;
    }

    public void ReceiveDamage(int val)
    {
        shipDamage += val;
        if (shipCondition <= 0) 
        {
            Debug.Log("NPS has received lethal damage");
            if (myMissionRef) MissionCheck();

            if (vip) DemoteGoons();

            Destroy(gameObject);
        } 
        else if(shipCondition <= shipDurability / 4) //chance to toggle hostility
        {
            float rand = Random.value;
            if (rand > 0.9f) hostile = !hostile;
        }

        aggroTimer = 0f;
        if (!triggered)
        {
            triggered = true;

            if(goons.Count > 0)
            {
                TriggerGoons();
            }
        }
    }

    void MissionCheck()
    {
        if(myMissionRef.missionType == Mission.MType.Destroy)
        {
            //complete mission
            GameManager.Instance.mMgr.CompleteMission(myMissionRef.id);
        }
        else if (myMissionRef.missionType == Mission.MType.Loot)
        {
            //spawn loot

        }
        else if(myMissionRef.missionType == Mission.MType.Fine || myMissionRef.missionType == Mission.MType.Tag ||myMissionRef.missionType == Mission.MType.Tow)
        {
            //fail mission
            GameManager.Instance.mMgr.RemoveMission(myMissionRef.menuItem.posInArray);
        }
        
    }

    public void Fine()
    {

    }

    void DockNDie()
    {
        GameManager.Instance.RecycleNPS(this);
    }

    List<NPSNode> GeneratePath(NPSNode start, NPSNode goal)
    {
        List<NPSNode> openSet = new List<NPSNode>();
        HashSet<NPSNode> closedSet = new HashSet<NPSNode>();

        openSet.Add(start);

        // fromScore = Distance to start
        start.gScore = 0f;
        start.hScore = Vector3.Distance(start.transform.position, goal.transform.position);


        while (openSet.Count > 0)
        {
            NPSNode current = GetLowest(openSet);
            //Debug.Log();

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == goal)
            {
                //Debug.Log("Path success: " + gameObject.name);
                return CalcFinalPath(start, goal);
            }

            for (int i = 0; i < current.connectedNodes.Count; i++)
            {
                NPSNode neighbor = current.connectedNodes[i];

                if (closedSet.Contains(neighbor)) continue;
                

                // tentaScore = fromScore of current + distance between current and neighbor
                float tentaScore = current.gScore + current.distanceValues[i];

                if (tentaScore < neighbor.gScore || !openSet.Contains(neighbor))
                {

                    neighbor.gScore = tentaScore;
                    neighbor.hScore = Vector3.Distance(neighbor.transform.position, goal.transform.position);
                    neighbor.prevNode = current;

                    //add neighbor to open set so it will be tested
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        Debug.Log("Path fail: " + gameObject.name);
        attemptRecalcPath = true;
        return openSet;
    }

    List<NPSNode> CalcFinalPath(NPSNode a_start, NPSNode a_end)
    {
        List<NPSNode> finalPath = new List<NPSNode>();
        finalPath.Add(a_end);
        NPSNode current = a_end.prevNode;
        HashSet<NPSNode> checkes = new HashSet<NPSNode>();

        while(current != a_start)
        {
            if (checkes.Contains(current)) 
            {
                Debug.Log("current is contained. should not happen");
                break;
            }
            checkes.Add(current);


            finalPath.Add(current);
            current = current.prevNode;
        }

        finalPath.Reverse();

        return finalPath;
    }

    NPSNode GetLowest(List<NPSNode> oSet)
    {
        NPSNode lowest = oSet[0];
        if (oSet.Count > 1)
        {
            for (int i = 1; i < oSet.Count; i++)
            {
                if (oSet[i].hScore < lowest.hScore)
                {
                    if (oSet[i].fScore < lowest.fScore || oSet[i].fScore == lowest.fScore) lowest = oSet[i];
                }  
                else continue;
            }
        }

        return lowest;
    }

    private void OnDrawGizmosSelected()
    {
        if (drawGizmos)
        {
            if (path.Count > 0)
            {
                
                Debug.DrawLine(transform.position, path[0].transform.position,Color.green);
                if (path.Count > 1)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 1; i < path.Count; i++)
                    {
                        Debug.DrawLine(path[i - 1].transform.position, path[i].transform.position,Color.yellow);
                    }
                }
            }
        }

        
    }
    /*
    private void OnDrawGizmos()
    {
        if(path.Count > 0)
        {
            for(int i = 1; i < path.Count; i++)
            {
                Debug.DrawLine(path[i - 1].transform.position, path[i].transform.position, Color.red, 1f);
            }
        }
    }
    */
}