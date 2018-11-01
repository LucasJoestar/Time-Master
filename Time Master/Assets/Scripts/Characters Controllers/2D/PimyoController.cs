using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PimyoController : MonoBehaviour
{
    /* PimyoController :
	*
	* [Write the Description of the Script here !]
	*/

    #region Events
    public event Action<float> OnAir = null;
    public event Action<float> OnMovement = null;
    #endregion

    #region Fields / Accessors
    #region Components
    // The components of the GameObject used in the script

    [Header("Components :")]

    [SerializeField] new Transform transform = null;
    [SerializeField] new Rigidbody2D rigidbody = null;
    #endregion

    #region Character Status
    // The variables of the character's status

    [Header("Status :")]

    [SerializeField] bool isFacingRight = true;
    [SerializeField] bool isRunning = false;

    [Header("Speed :")]

    // Actual speed of the character
    [SerializeField] private float speed = 1;

    // Speed settings to configure
    [SerializeField] private float speedRun = 1.25f;
    public float SpeedRun
    {
        get { return speedRun; }
    }

    [SerializeField] private float speedWalk = 1;
    public float SpeedWalk
    {
        get { return speedWalk; }
    }

    [Header("Jump :")]

    [SerializeField] private int jumpForce = 50;
    #endregion

    #region Key Codes
    // The keys used for the actions of the character

    [Header("Key Codes :")]

    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    #endregion

    [SerializeField] ClassSerializable classSerializable = new ClassSerializable();
    [SerializeField] List<string> strings = new List<string>();
    #endregion

    #region Methods
    #region Unity Methods
    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Components check

        if (!transform) transform = gameObject.transform;
        if (!rigidbody) rigidbody = GetComponent<Rigidbody2D>();

        if (!rigidbody)
        {
            Debug.Log("No Rigidbody2D on the Pimyo Controller !");
            Destroy(gameObject);
        }
    }

    // Use this for initialization
    void Start ()
    {
        // Initializes the value of the character's speed
        speed = speedWalk;
	}
	
	// Update is called once per frame
	void Update ()
    {
        CheckInputs();
	}
    #endregion
	
    #region Original Methods
    // Checks the inputs of the user and executes the associated actions
	private void CheckInputs()
    {
        // Jump action (When key is down)
        if (Input.GetKeyDown(jumpKey))
        {
            rigidbody.velocity += Vector2.up * jumpForce;
        }

        // Run action (While key is pressed)
        if (Input.GetKeyDown(runKey))
        {
            isRunning = true;
        }
        else if (Input.GetKeyUp(runKey))
        {
            isRunning = false;
        }

        // Get the horizontal movement of the character
        speed = Input.GetAxis("Horizontal") * (!isRunning ? speedWalk : speedRun);

        // Flip the character if needed
        if ((speed > 0 && !isFacingRight) || (speed < 0 && isFacingRight))
        {
            FlipX();
        }

        // Set the speed to a positive value
        speed = speed < 0 ? speed * -1 : speed;

        // Move the character towards the indicated direction
        transform.position = Vector2.MoveTowards(transform.position, transform.position + (isFacingRight ? transform.right : transform.right * -1), speed);

        // Call the situation's associated event(s)

        OnAir?.Invoke(rigidbody.velocity.y);
        OnMovement(speed);
    }

    // Flip the character of the X axis
    private void FlipX()
    {
        // Flip the character
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);

        // Update the boolean
        isFacingRight = !isFacingRight;
    }
    #endregion
    #endregion
}

[Serializable]
public class ClassSerializable
{
    [SerializeField] string name = "Name";

    [SerializeField] float amount = 0.1f;
}