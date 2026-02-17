using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RemoteControl : MonoBehaviour
{
    [Header("References")]
    public Transform headCamera; 

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float rotateSpeed = 80.0f;
    public float depthSpeed = 1.5f;
    public float maxDistance = 100f;

    [Header("Laser Visuals")]
    public float laserStartWidth = 0.006f;
    public float laserEndWidth = 0.012f;
    public float eyeOffset = 0.05f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.15f;

    private GameObject currentTarget;
    private Rigidbody targetRb;
    private LineRenderer laserLine;

    void Start()
    {
        laserLine = GetComponent<LineRenderer>();

        laserLine.positionCount = 2;
        laserLine.useWorldSpace = true;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));

   
        laserLine.startWidth = laserStartWidth;
        laserLine.endWidth = laserEndWidth;

        laserLine.colorGradient = NoHitGradient();
    }

    void Update()
    {
        if (headCamera == null) return;

        
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        laserLine.startWidth = laserStartWidth * pulse;
        laserLine.endWidth = laserEndWidth * pulse;

        Vector3 rayOrigin = headCamera.position + headCamera.forward * eyeOffset;
        Vector3 rayDirection = headCamera.forward;

        laserLine.SetPosition(0, rayOrigin);

        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Ignore Raycast", "Player");

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance, layerMask))
        {
            laserLine.SetPosition(1, hit.point);
            laserLine.colorGradient = HitGradient();

            currentTarget = hit.collider.gameObject;

            
            if (currentTarget.transform.parent != null && currentTarget.name.Contains("Cube"))
                currentTarget = currentTarget.transform.parent.gameObject;
                

            targetRb = currentTarget.GetComponent<Rigidbody>();
        }
        else
        {
            laserLine.SetPosition(1, rayOrigin + rayDirection * maxDistance);
            laserLine.colorGradient = NoHitGradient();
            currentTarget = null;
            targetRb = null;
        }

        if (currentTarget != null)
        {
            HandleMovement();
            HandleRotation();
            HandleDepth();
        }
    }

    void HandleMovement()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (input.magnitude < 0.1f) return;

        Vector3 dir = (headCamera.right * input.x + headCamera.up * input.y).normalized;

        
        float distance = Vector3.Distance(currentTarget.transform.position, headCamera.position);

        
        Vector3 moveDir = dir * moveSpeed * distance * 0.2f * Time.deltaTime;

        if (targetRb != null)
            targetRb.MovePosition(targetRb.position + moveDir);
        else
            currentTarget.transform.position += moveDir;
    }
    void HandleRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (input.magnitude < 0.1f) return;

        Quaternion rotX = Quaternion.AngleAxis(-input.x * rotateSpeed * Time.deltaTime, headCamera.up);
        Quaternion rotY = Quaternion.AngleAxis(input.y * rotateSpeed * Time.deltaTime, headCamera.right);
        Quaternion finalRot = rotX * rotY;

        if (targetRb != null)
            targetRb.MoveRotation(finalRot * targetRb.rotation);
        else
            currentTarget.transform.rotation = finalRot * currentTarget.transform.rotation;
    }

    void HandleDepth()
    {
        Vector3 dirToHead = (headCamera.position - currentTarget.transform.position).normalized;
        Vector3 depthMove = Vector3.zero;

        float distance = Vector3.Distance(currentTarget.transform.position, headCamera.position);

        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
            depthMove = dirToHead * depthSpeed * distance * 0.2f * Time.deltaTime;

        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick))
            depthMove = -dirToHead * depthSpeed * distance * 0.2f * Time.deltaTime;

        if (depthMove == Vector3.zero) return;

        if (targetRb != null)
            targetRb.MovePosition(targetRb.position + depthMove);
        else
            currentTarget.transform.position += depthMove;
    }


    Gradient HitGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.3f, 1f, 0.3f), 0f),
                new GradientColorKey(Color.green, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.15f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    Gradient NoHitGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.1f, 0f),
                new GradientAlphaKey(0.6f, 1f)
            }
        );
        return g;
    }
}
