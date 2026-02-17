using UnityEngine;
using UnityEngine.XR;
public class PlaneGrabber : MonoBehaviour
{
    [Header("Controller Setup")]
    public OVRInput.Controller controller; 

    [Header("Plane Settings")]
    public float grabRadius = 0.15f;
    private PaperPlane heldPlane;
    private bool isGrabbingPlane = false;

    [Header("Task Interaction Settings")]

    public SelectionTaskMeasure selectionTaskMeasure;

    private float gripTriggerValue;
    private bool isInCollider;
    private bool isSelected;
    private GameObject selectedObj;

    void Update()
    {

        float indexTriggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);

        if (indexTriggerVal > 0.8f && !isGrabbingPlane)
        {
            TryGrabPlane();
        }
        else if (indexTriggerVal < 0.2f && isGrabbingPlane)
        {
            ThrowPlane();
        }

        if (isGrabbingPlane && heldPlane != null)
        {
            heldPlane.transform.position = transform.position;
            heldPlane.transform.rotation = transform.rotation * Quaternion.Euler(-35, 0, 0);
        }


        if (isInCollider)
        {
            
            if (!isSelected && gripTriggerValue > 0.95f)
            {
                isSelected = true;
                if (selectedObj != null)
                {
                   
                    selectedObj.transform.parent.transform.parent = transform;
                }
            }
           
            else if (isSelected && gripTriggerValue < 0.95f)
            {
                isSelected = false;
                if (selectedObj != null)
                {
                    selectedObj.transform.parent.transform.parent = null;
                }
            }
        }
    }


    void TryGrabPlane()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius);
        foreach (var hit in hits)
        {
            PaperPlane plane = hit.GetComponent<PaperPlane>();
            
            if (plane != null)
            {
                GrabPlane(plane);
                return;
            }
        }
    }
    XRNode GetXRNodeFromOVRController()
    {
        if (controller == OVRInput.Controller.LTouch)
            return XRNode.LeftHand;

        if (controller == OVRInput.Controller.RTouch)
            return XRNode.RightHand;

        return XRNode.RightHand; 
    }
    void SendHaptic(XRNode node, float amplitude = 0.25f, float duration = 0.04f)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (device.isValid &&
            device.TryGetHapticCapabilities(out var capabilities) &&
            capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }
    void GrabPlane(PaperPlane plane)
    {
        isGrabbingPlane = true;
        GetComponent<AudioSource>().Play();
        heldPlane = plane;
        XRNode hand = GetXRNodeFromOVRController();
        SendHaptic(hand, 0.25f, 0.04f);
    }

    void ThrowPlane()
    {
        if (heldPlane != null)
        {
            Vector3 vel = OVRInput.GetLocalControllerVelocity(controller);
            heldPlane.Launch(vel);
        }
        isGrabbingPlane = false;
        heldPlane = null;
    }

    void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.CompareTag("objectT"))
        {
            isInCollider = true;
            selectedObj = other.gameObject;
        }
       
        else if (other.gameObject.CompareTag("selectionTaskStart"))
        {
           
            if (selectionTaskMeasure != null && !selectionTaskMeasure.isCountdown)
            {
                selectionTaskMeasure.isTaskStart = true;
                selectionTaskMeasure.StartOneTask();
            }
        }
        
        else if (other.gameObject.CompareTag("done"))
        {
            if (selectionTaskMeasure != null)
            {
                selectionTaskMeasure.isTaskStart = false;
                selectionTaskMeasure.EndOneTask();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("objectT"))
        {
            isInCollider = false;
            selectedObj = null;
        }
    }
}