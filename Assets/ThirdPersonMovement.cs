using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;
    
    public float speed = 6f;
    public float gravity = -9.81f;
    
    public float turnSmoothTime = 0.1f;
    private float _turnSmoothVelocity;
    
    public float jumpHeight = 3f;
    public float jumpPadHeight = 10f;
    public Transform groundCheckMaster;
    public Transform ceilingCheckMaster;
    public float groundDistance = 0.4f;
    public float ceilingDistance = 0.01f;
    public float groundSlamTime = 3f;
    
    public LayerMask groundMask;
    public LayerMask jumpPadMask;
    
    private Vector3 _velocity;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isJumpPad;
    [SerializeField] private bool underCeiling;
    [SerializeField] private bool hasUnstuckCeiling;
    [SerializeField] private float groundSlamCounter;
    [SerializeField] private bool groundSlamInProgress = false;

    public AudioSource personalSounds;

    public AudioClip bonkSound;
    public AudioClip slamSound;

    public ParticleSystem bonkParticles;
    public ParticleSystem groundSlamParticles;
    
    private void Start()
    {
        // lock cursor on game start (temporary)
        Cursor.lockState = CursorLockMode.Locked;
        groundSlamCounter = groundSlamTime;
    }

    private void Update()
    {
        // set local variables
        var groundCheck = groundCheckMaster.position;
        var ceilingCheck = ceilingCheckMaster.position;
        isGrounded = Physics.CheckSphere(groundCheck, groundDistance, groundMask);
        isJumpPad = Physics.Raycast(groundCheck, -transform.up, out var jumpHit, groundDistance, jumpPadMask);
        underCeiling = Physics.Raycast(ceilingCheck, transform.up, ceilingDistance, groundMask);
        
        if (groundSlamCounter > 0) {
            groundSlamCounter -= Time.deltaTime;
        }

        // set grounded y velocity
        if (isGrounded)
        {
            // when ground slam lands
            if (groundSlamInProgress)
            {
                _velocity.y = Mathf.Sqrt(Mathf.Abs(_velocity.y) * -0.5f * gravity);
                personalSounds.clip = slamSound;
                personalSounds.Play();
                groundSlamParticles.Play();
            }
            
            groundSlamInProgress = false;
            hasUnstuckCeiling = false;
            
            if (_velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
        
        // gather input
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        var dir = new Vector3(horizontal, 0, vertical).normalized;

        // apply gravity
        _velocity.y += gravity * Time.deltaTime;
        controller.Move(_velocity * Time.deltaTime);
        
        // jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // ground slam
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isGrounded && groundSlamCounter <= 0)
        {
            _velocity.y = -Mathf.Sqrt(20f * -2f * gravity);
            groundSlamCounter = groundSlamTime;
            groundSlamInProgress = true;
        }

        // jump pads
        if (isJumpPad)
        {
            isGrounded = true;
            hasUnstuckCeiling = false;
            _velocity.y = Mathf.Sqrt(jumpPadHeight * -2f * gravity);
            var _audio = jumpHit.transform.GetComponent<AudioSource>();
            if (_audio)
            {
                _audio.reverbZoneMix = 1f;
                _audio.Play();
            }
        }

        // fix ceiling sticking and play bonk sound
        if (underCeiling && !hasUnstuckCeiling)
        {
            _velocity.y = -_velocity.y;
            hasUnstuckCeiling = true;

            personalSounds.clip = bonkSound;
            personalSounds.Play();
            
            bonkParticles.Play();
        }

        // rotate and move player in correct direction
        if (dir.magnitude >= 0.1f)
        {
            var targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            var moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move( moveDir.normalized * (Time.deltaTime * speed));
        }
    }
}
