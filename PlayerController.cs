using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public enum PlayMode { Overview, Character }

    private PlayMode currentMode;
    public PlayMode CurrentMode
    {
        set
        {
            hasTransit = false;
            currentMode = value;
        }
        get
        {
            return currentMode;
        }
    }

    bool hasTransit;
    PlayerCharacterControl playerControl = PlayerCharacterControl.Full;
    public enum PlayerCharacterControl { Full, Movement, Interaction, None }

    [SerializeField]
    Transform playerObject, overviewObject;
    NavMeshAgent playerAgent;
    Camera cam;
    [SerializeField]
    LayerMask rtsMask, fpsMask, creatureMask, interactableMask;

    [SerializeField]
    float camZoomSpeed = 50f;
    [SerializeField]
    float camOvrCamRotSpd = 360f;
    [SerializeField]
    int edgeScrollBuffer = 12;
    [SerializeField]
    float camFpsCamRotSpd = 360f;
    Vector3 camDefaultLocalPos = new Vector3(-20f, 20f, 0f); //x groundlevel pos -18.75
    Vector3 camMaxZoom = new Vector3(-2f, 3f, 0f);

    [SerializeField]
    float moveSpeed = 15f;
    [SerializeField]
    float movePwr = 1f;
    [SerializeField]
    int cameralvl;

    float ovrScrl = 0f;
    float ovrYRot = 0f;
    float ovrZRot = 0f;
    float fpsXRot = 0f;
    float fpsYRot = 0f;

    bool init = false;

    bool mouse1click = false;
    bool mouse2click = false;

    //UIManager and GameManager are static objects in scene

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void Init() //this object is initialized, by GameManager, when the scene is done loading.
    {
        cam = GameManager.playerCamera;

        CurrentMode = PlayMode.Overview;
        playerObject = GameManager.Instance.playerCreature.transform;
        playerAgent = playerObject.GetComponent<NavMeshAgent>();
        playerAgent.enabled = true;

        overviewObject = new GameObject().transform;
        overviewObject.name = "ovrViewObj";
        overviewObject.position = new Vector3(playerObject.position.x, 0f, playerObject.position.z);

        playerControl = PlayerCharacterControl.Full;
        init = true;
    }

    public void SetPlayerControl(PlayerCharacterControl x)
    {
        playerControl = x;
    }

    public void MoveOverviewObject(Vector3 pos)
    {

    }

    private void Update()
    {
        if (init)
        {
            //store input values in variables

            float xMouse = Input.GetAxis("Mouse X");
            float zMouse = Input.GetAxis("Mouse Y");
            float yMouse = Input.GetAxis("Mouse ScrollWheel");

            float hKeys = Input.GetAxis("Horizontal");
            float vKeys = Input.GetAxis("Vertical");


            bool mouse1 = Input.GetButton("Fire1");
            bool mouse2 = Input.GetButton("Fire2");
            bool mouse3 = Input.GetButton("Fire3");

            Vector3 mousePos = Input.mousePosition;

            switch (currentMode)
            {
                case PlayMode.Overview: // strategy view
                    if (!hasTransit)
                    {
                        if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;

                        playerAgent.updateRotation = true;

                        cam.transform.SetParent(overviewObject, true);
                        cam.transform.localPosition = new Vector3(-20f, 20f, 0f);
                        cam.transform.LookAt(overviewObject);
                        cam.cullingMask = rtsMask;

                        ovrYRot = overviewObject.eulerAngles.y;
                        ovrZRot = overviewObject.eulerAngles.z;
                        hasTransit = true;
                    }
                    // movement
                    Vector3 moveDir = Vector3.zero;
                    float zMagnitude = 0f;
                    float xMagnitude = 0f;

                    if (vKeys != 0f || hKeys != 0f)
                    {
                        zMagnitude = Mathf.Abs(hKeys);
                        xMagnitude = Mathf.Abs(vKeys);
                        moveDir += overviewObject.rotation * new Vector3(vKeys, 0f, -hKeys).normalized;
                        //Debug.Log(moveDir + " | " + zKeys + " : " + xKeys);
                    }

                    // left mouse btn
                    if (mouse1)
                    {
                        if (!mouse1click)
                        {
                            //mouse click fires once per press until released
                            mouse1click = true;

                            //selection on units etc...
                        }
                        else
                        {
                            //mouse 1 hold down
                            //setup timer

                            //drag box selector
                        }


                    }
                    else
                    {
                        if (mouse1click) mouse1click = false;
                    }

                    //right mouse btn
                    if (mouse2)
                    {
                        if (!mouse2click)
                        {
                            //mouse click fires once per press until released
                            mouse2click = true;

                            //command selected unit
                        }
                    }
                    else
                    {
                        if(mouse2click) mouse2click = false;
                    }

                    //middle mouse btn
                    //camera rotation by mouse
                    if (mouse3)
                    {
                        if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

                        float step = camOvrCamRotSpd * Time.deltaTime;

                        ovrYRot += xMouse;
                        ovrZRot += zMouse;
                        ovrZRot = Mathf.Clamp(ovrZRot, -40f, 40f);

                        Vector3 rotEuler = new Vector3(0f, ovrYRot, ovrZRot);
                        overviewObject.rotation = Quaternion.RotateTowards(overviewObject.rotation, Quaternion.Euler(rotEuler), step);

                        ovrYRot = overviewObject.eulerAngles.y;
                    }
                    else
                    {
                        if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;

                        //this is for edge panning
                        if (!UIManager.Instance.IsCursorOverUI)
                        {
                            bool inBuffer = false;

                            float widthBuff = Screen.width / edgeScrollBuffer;
                            if (mousePos.x < widthBuff && mousePos.x > -widthBuff)
                            {
                                //move left
                                xMagnitude += (widthBuff - mousePos.x) / widthBuff;
                                inBuffer = true;
                            }
                            else if (mousePos.x > Screen.width - widthBuff && mousePos.x < Screen.width + widthBuff)
                            {
                                //move right
                                xMagnitude += (Screen.width - widthBuff - mousePos.x) / -widthBuff;
                                inBuffer = true;
                            }

                            float heightBuff = Screen.height / edgeScrollBuffer;
                            if (mousePos.y < heightBuff && mousePos.y > -heightBuff)
                            {
                                //move down
                                zMagnitude += (heightBuff - mousePos.y) / -heightBuff;
                                inBuffer = true;
                            }
                            else if (mousePos.y > Screen.height - heightBuff && mousePos.y < Screen.height + heightBuff)
                            {
                                //move up
                                zMagnitude += (Screen.height - heightBuff - mousePos.y) / -heightBuff;
                                inBuffer = true;
                            }

                            if (inBuffer) moveDir += overviewObject.rotation * new Vector3(mousePos.y - Screen.height / 2, 0f, -(mousePos.x - Screen.width / 2)).normalized;
                        }

                    }

                    moveDir = Vector3.ClampMagnitude(new Vector3(moveDir.x, cameralvl, moveDir.z), 1f);
                    overviewObject.position += moveDir * Vector2.ClampMagnitude(new Vector2(xMagnitude, zMagnitude), 1f).magnitude * moveSpeed * Time.deltaTime;

                    //adjust zoom level
                    ovrScrl = Mathf.Clamp01(ovrScrl + yMouse * camZoomSpeed * Time.deltaTime);
                    Vector3 zoomMove = Vector3.Slerp(camDefaultLocalPos, camMaxZoom, ovrScrl);
                    cam.transform.localPosition = zoomMove;


                    break;
                case PlayMode.Character: // fps control
                    if (!hasTransit)
                    {
                        if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

                        playerAgent.updateRotation = false;

                        cam.transform.SetParent(playerObject, true);
                        cam.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                        cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        cam.cullingMask = fpsMask;

                        fpsXRot = 0f;
                        fpsYRot = playerObject.eulerAngles.y;
                        hasTransit = true;
                    }
                    bool fpsCursor = Input.GetButton("FpsCursor");

                    if (fpsCursor && Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
                    else if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

                    //if player has movement
                    if (playerControl == PlayerCharacterControl.Full || playerControl == PlayerCharacterControl.Movement)
                    {
                        //cam look
                        float rotStep = camFpsCamRotSpd * Time.deltaTime;
                        fpsXRot += -zMouse;
                        fpsYRot += xMouse;
                        fpsXRot = Mathf.Clamp(fpsXRot, -60f, 70f);
                        cam.transform.localRotation = Quaternion.RotateTowards(cam.transform.localRotation, Quaternion.Euler(fpsXRot, 0f, 0f), rotStep);
                        playerObject.rotation = Quaternion.RotateTowards(playerObject.rotation, Quaternion.Euler(0f, fpsYRot, 0f), rotStep);

                        fpsYRot = playerObject.eulerAngles.y;

                        // movement
                        Vector3 movmentDir = new Vector3(hKeys, 0f, vKeys).normalized;
                        Vector3 movmentPos = playerObject.position + movmentDir;
                        playerAgent.Move(playerAgent.transform.rotation * movmentDir * playerAgent.speed * Time.deltaTime);
                    }


                    break;
            }
        }
        
    }

    
}
