using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class PaperPlane : MonoBehaviour
{
    [Header("Flight Settings")]
    public float glideForce = 50f;
    public float gravityScale = 0.2f; 
    public float forwardMomentum = 0.98f;

    [Header("Interactive Controls")]
    public float steerSensitivity = 1.8f; 
    public float blowThreshold = 0.0015f;
    public float blowLiftForce = 1.8f;

    private Rigidbody rb;
    private bool isFlying = false;
    private LocomotionTechnique locomotionManager;
    private ParkourCounter parkourCounter;
    private Transform headTransform;
    public TrailRenderer[] trails;
    private ParticleSystem blowParticles;


    // Microphone Handling
    private bool micReady = false;
    private static AudioClip micClip; 
    private static string micDevice;
    private float[] audioSamples = new float[128]; 
    private float lastBlowTime = 0f;
    private float blowCooldown = 0.7f; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        parkourCounter = FindFirstObjectByType<ParkourCounter>();

        StartCoroutine(InitMicrophone());
    }
    IEnumerator InitMicrophone()
    {

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);


            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                yield return null;
        }

        if (Microphone.devices.Length == 0)
        {
            
            yield break;
        }

        micDevice = Microphone.devices[Microphone.devices.Length - 1];
        

        micClip = Microphone.Start(micDevice, true, 5, 44100);


        while (Microphone.GetPosition(micDevice) <= 0)
            yield return null;

        micReady = true;
        
    }
    void SendHaptic(XRNode node, float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }
    void SendHapticBoth(float amplitude = 0.15f, float duration = 0.04f)
    {
        SendHaptic(XRNode.LeftHand, amplitude, duration);
        SendHaptic(XRNode.RightHand, amplitude, duration);
    }
    public void Initialize(LocomotionTechnique manager)
    {
        locomotionManager = manager;
        if (manager.hmd != null)
        {
            headTransform = manager.hmd.transform;

            Transform particleObject = headTransform.Find("Breath/Breathy");

            if (particleObject != null)
            {
                blowParticles = particleObject.GetComponent<ParticleSystem>();
            }
            else
            {
                
                blowParticles = headTransform.GetComponentInChildren<ParticleSystem>();
            }
        }
    }
    float NormalizeMic(float mic)
    {

        const float blowTrigger = 0.02f;
        const float blowMax = 0.043f;
        const float fixedBlowValue = 0.8f;

        if (mic >= blowTrigger && blowMax >= mic)
            return fixedBlowValue;

        return 0f;
    }

    public void Launch(Vector3 velocity)
    {
        transform.SetParent(null); 
        rb.isKinematic = false;   
        rb.detectCollisions = true;
        isFlying = true;

        rb.linearVelocity = velocity * 1.5f;

        foreach (var t in trails)
            t.emitting = true;

        GetComponent<AudioSource>().Play();
    }

    void FixedUpdate()
    {
        if (isFlying)
        {

            rb.AddForce(Vector3.down * 9.81f * gravityScale, ForceMode.Acceleration);
            

            if (rb.linearVelocity.sqrMagnitude > 0.5f)
            {
                Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity) * Quaternion.Euler(-85f, 0f, 0f);
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f));
            }

            if (headTransform != null)
            {
    
                float headTilt = headTransform.eulerAngles.z;

                if (headTilt > 180) headTilt -= 360;

                float steer = -headTilt * steerSensitivity;


                Vector3 lateralForce = transform.right * steer;
                rb.AddForce(lateralForce, ForceMode.Acceleration);

                rb.MoveRotation(
                    rb.rotation *
                    Quaternion.Euler(0f, steer * 0.1f, 0f)
                );
            }

            float mic = GetMicVolume();
            float blow = NormalizeMic(mic);
   
            if (blow > 0f && Time.time - lastBlowTime > blowCooldown)
            {
                Vector3 flatForward = transform.forward;
                flatForward.y = 0;
                flatForward.Normalize();

                Vector3 impulse;
                float heightLimit = 14.5f; 

                if (transform.position.y < heightLimit)
                {
                    Vector3 forwardPush = flatForward * blowLiftForce * 0.75f * blow;
                    Vector3 liftPush = Vector3.up * blowLiftForce * 1.2f * blow;
                    impulse = forwardPush + liftPush;
                }
                else
                {
                    impulse = flatForward * blowLiftForce * 0.8f * blow;

                }
                rb.AddForce(impulse, ForceMode.Impulse);

                SendHapticBoth(0.18f, 0.05f);
                lastBlowTime = Time.time;
                if (blowParticles != null) blowParticles.Play();

            }
        }
    }
    float GetMicVolume()
    {
        if (!micReady || micClip == null) return 0f;

        int micPos = Microphone.GetPosition(micDevice) - audioSamples.Length;
        if (micPos < 0) return 0f;

        micClip.GetData(audioSamples, micPos);

        float max = 0f;
        for (int i = 0; i < audioSamples.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(audioSamples[i]));

        return max;
    }
    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("coin"))
        {
            if (parkourCounter != null)
            {
                parkourCounter.coinCount++;

                AudioSource playerAudio = locomotionManager.GetComponent<AudioSource>();
                if (playerAudio != null)
                {
                    playerAudio.Play();
                }
            }
            other.gameObject.SetActive(false);
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!isFlying) return;

        if (collision.gameObject.CompareTag("Player")) return;

        isFlying = false;
        rb.isKinematic = true;
        if (locomotionManager != null)
        {
            locomotionManager.OnPlaneLanded(transform.position);
        }
        foreach (var t in trails)
            t.emitting = false;

        Destroy(gameObject);
    }
}