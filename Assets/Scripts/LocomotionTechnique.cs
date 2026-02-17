using UnityEngine;

public class LocomotionTechnique : MonoBehaviour
{
    // Please implement your locomotion technique in this script. 
    [Header("Paper Plane Setup")]
    public GameObject planePrefab;      
    public Transform holsterTransform; 
    public GameObject hmd;              

    private GameObject currentPlane;
    [Header("Ground Detection")]
    public LayerMask groundLayer;


    /////////////////////////////////////////////////////////
    // These are for the game mechanism.
    public ParkourCounter parkourCounter;
    public string stage;
    public SelectionTaskMeasure selectionTaskMeasure;
    
    void Start()
    {
        SpawnPlane();
    }
    public void OnPlaneLanded(Vector3 landingPos)
    {

        Vector3 targetPos = landingPos;
  
        RaycastHit hit;
        
        if (Physics.Raycast(landingPos + Vector3.up * 0.5f, Vector3.down, out hit, 2.0f))
        {
           
            targetPos.y = hit.point.y;
        }

        transform.position = targetPos;

    
        SpawnPlane();
    }

    public void SpawnPlane()
    {

        
        currentPlane = Instantiate(planePrefab, holsterTransform.position, holsterTransform.rotation);

        
        currentPlane.transform.SetParent(holsterTransform);
        currentPlane.transform.localPosition = Vector3.zero;
        currentPlane.transform.localRotation = Quaternion.identity;

        
        PaperPlane planeScript = currentPlane.GetComponent<PaperPlane>();
        if (planeScript) planeScript.Initialize(this);
    }

    public void CollectCoin(GameObject coinObject)
    {
        if (parkourCounter != null)
        {
            parkourCounter.coinCount += 1;
            
            GetComponent<AudioSource>()?.Play();
            coinObject.SetActive(false);
           
        }
    }
    void Update()
    {
        ////////////////////////////////////////////////////////////////////////////////
        // These are for the game mechanism.
        if (OVRInput.Get(OVRInput.Button.Two) || OVRInput.Get(OVRInput.Button.Four))
        {
            if (parkourCounter.parkourStart)
            {
                transform.position = parkourCounter.currentRespawnPos;

                SpawnPlane();
            }
        }
        FollowGround();
    }
    void FollowGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2.0f, groundLayer))
        {
            Vector3 targetPos = transform.position;
            targetPos.y = hit.point.y + 0.09f;
            transform.position = targetPos;
        }
    }
    void OnTriggerEnter(Collider other)
    {

        // These are for the game mechanism.
        if (other.CompareTag("banner"))
        {
            stage = other.gameObject.name;
            parkourCounter.isStageChange = true;
        }
        else if (other.CompareTag("objectInteractionTask"))
        {
            selectionTaskMeasure.isTaskStart = true;
            selectionTaskMeasure.scoreText.text = "";
            selectionTaskMeasure.partSumErr = 0f;
            selectionTaskMeasure.partSumTime = 0f;
            // rotation: facing the user's entering direction
            float tempValueY = other.transform.position.y > 0 ? 12 : 0;
            Vector3 tmpTarget = new(hmd.transform.position.x, tempValueY, hmd.transform.position.z);
            selectionTaskMeasure.taskUI.transform.LookAt(tmpTarget);
            selectionTaskMeasure.taskUI.transform.Rotate(new Vector3(0, 180f, 0));
            selectionTaskMeasure.taskStartPanel.SetActive(true);
        }
        else if (other.CompareTag("coin"))
        {
            parkourCounter.coinCount += 1;
            GetComponent<AudioSource>().Play();
            other.gameObject.SetActive(false);
        }
        // These are for the game mechanism.
    }
}