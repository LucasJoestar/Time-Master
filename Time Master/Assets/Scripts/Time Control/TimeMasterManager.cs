using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* TimeStates :
 * 
 * The different possible states of the time scrolling for the objects
 * on which a time control is exercised
 * 
 * The Time can be :
 *      - Paused, so no more scrolling
 *      - Playing, so it goes on and is advancing
 *      - Rewinding, that is coming back in earlier times
*/
public enum TimeState
{
    Pause,
    Play,
    Rewind
}

public class TimeMasterManager : MonoBehaviour
{
    /* TimeMasterManager :
	*
	* Manages the control of the time over the scene
    * 
    * This script should be attached to an empty GameObject in the scene
    * 
    * This object is Singleton and so should be accessible from anyone anywhere in the scene to get information or
    * for rewinding objects to bind themselves to this instance
    * 
    * For external scripts to get informations during runtime on the control of the time, use this instance
	*/

    #region Events
    /* Global Events
     * 
     * Events giving informations about the control of the time
     * These events can be very helpful to get feedback about what's going on
    */
    public event Action<float> OnRewindGetPercentage = null;
    public event Action<float> OnRewindScalerUpdate = null;
    public event Action<TimeState> OnGlobalTimeStateUpdate = null;

    /* Pause Event
     * 
     * Called when (un)pausing time in the scene
     * (Bool parameter indicates if the pause is on or off)
    */
    public event Action<bool> OnPause = null;

    /* Record Events
     * 
     * Called when starting / ending record or just when recording time in the scene
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRecordStarts = null;
    public event Action<int> OnRecord = null;
    public event Action<int> OnRecordEnds = null;

    /* Rewind Events
     * 
     * Called when starting / ending rewind or just when rewinding time in the scene
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRewindStarts = null;
    public event Action<int> OnRewind = null;
    public event Action<int> OnRewindEnds = null;
    #endregion

    #region Fields / Accessors
    // ********** TIME STATE ********** //

    // The actual time state of the control of the time in the scene
    [SerializeField] private TimeState globalTimeState = TimeState.Play;
    public TimeState GlobalTimeState
    {
        get { return globalTimeState; }
        set
        {
            // When setting this TimeState property,
            // the timeState field is used as the previous TimeState value

            // Calls the associated event
            OnGlobalTimeStateUpdate?.Invoke(value);

            // First, executes actions depending on the previous state
            switch (globalTimeState)
            {
                case TimeState.Pause:
                    OnPause?.Invoke(false);
                    break;
                case TimeState.Play:
                    OnRecordEnds?.Invoke(timelineCurrentValue);
                    break;
                case TimeState.Rewind:
                    // If we were replaying, set this boolean to false
                    if (isReplaying) IsReplaying = false;
                    OnRewindEnds?.Invoke(timelineCurrentValue);
                    break;
                default:
                    break;
            }

            // Then, executes actions depending on the new state
            switch (value)
            {
                case TimeState.Pause:
                    OnPause?.Invoke(true);
                    break;
                case TimeState.Play:
                    // Reset the rewind scaler to 1
                    RewindScaler = 1;
                    // If the replay is enable, we may need to update the timeline length based on its current value
                    if (timelineCurrentValue < timelineLength) timelineLength = timelineCurrentValue;
                    // When starting play, send the percentage of the rewind as 100%
                    OnRewindGetPercentage?.Invoke(1);
                    OnRecordStarts?.Invoke(timelineCurrentValue);
                    break;
                case TimeState.Rewind:
                    // Set the timeline length of this rewind
                    rewindTimelineLength = timelineLength;
                    OnRewindStarts?.Invoke(timelineCurrentValue);
                    break;
                default:
                    break;
            }

            // Finally, set the timeState field
            globalTimeState = value;

            // Changes the time state of each time control object
            timeControlObjects.ForEach(o => o.TimeState = value);
        }
    }

    // ********** TIMECONTROL OBJECTS ********** //

    // List containing all the Time Control Objects in the scene
    [SerializeField] private List<TimeControlObject> timeControlObjects = new List<TimeControlObject>();
    public List<TimeControlObject> TimeControlObjects
    {
        get { return timeControlObjects; }
    }

    // ********** DESTRUCTED OBJECTS ********** //

    // Dictionary containing the destroyed objects and the frame at which this happened
    [SerializeField] private Dictionary<GameObjectReconstructor, int> destroyedObjects = new Dictionary<GameObjectReconstructor, int>();
    public Dictionary<GameObjectReconstructor, int> DestroyedObjects
    {
        get { return destroyedObjects; }
    }

    // ********** TIMELINE ********** //

    // The total length of the timeline (in frame)
    [SerializeField] private int timelineLength = 0;
    public int TimelineLength
    {
        get { return timelineLength; }
        set
        {
            value = value < 0 ? 0 : value;
            timelineLength = value;
            timelineCurrentValue = value;
        }
    }

    // The current value of the timeline (in frame)
    [SerializeField] private int timelineCurrentValue = 0;
    public int TimelineCurrentValue
    {
        get { return timelineCurrentValue; }
        set
        {
            value = Mathf.Clamp(value, 0, timelineLength);
            timelineCurrentValue = value;
        }
    }

    // When rewinding, this indicates the timeline length at its start
    [SerializeField] private int rewindTimelineLength = 0;
    public int RewindTimelineLength
    {
        get { return rewindTimelineLength; }
    }

    // ********** REPLAY ********** //

    // Indicates if the entries rewinded should be directly removed
    [SerializeField] private bool doAllowsReplay = true;
    public bool DoAllowsReplay
    {
        get { return doAllowsReplay; }
    }

    // Indicates if the rewind action is currently replaying or not
    [SerializeField] private bool isReplaying = false;
    public bool IsReplaying
    {
        get { return isReplaying; }
        set
        {
            isReplaying = value;

            // Set the isReplaying value for each time control object
            timeControlObjects.ForEach(o => o.IsReplaying = value);
        }
    }

    // ********** REWIND SCALER ********** //

    // The Array containing all the possible values of rewind scale
    [SerializeField] private float[] rewindScalerValues = new float[] { 0.2f, .25f, .5f, 1, 2, 4, 8, 16, 32 };
    public float[] RewindScalerValues
    {
        get { return rewindScalerValues; }
    }

    // The scaler of the rewind speed
    [SerializeField] private int rewindScalerIndex = 0;
    public int RewindScalerIndex
    {
        get { return rewindScalerIndex; }
        set
        {
            // Clamps the value
            value = Mathf.Clamp(value, 0, rewindScalerValues.Length - 1);
            rewindScalerIndex = value;

            // Calls the associated event
            OnRewindScalerUpdate?.Invoke(rewindScalerValues[value]);

            // Update the rewind scaler of each time control object
            timeControlObjects.ForEach(o => o.RewindScalerIndex = value);
        }
    }

    // The current rewind scaler
    public float RewindScaler
    {
        get { return rewindScalerValues[rewindScalerIndex]; }
        set
        {
            // If giving a value to the time scaler, check if it is available among the possible values and if so set the rewind scaler index
            if (rewindScalerValues.Contains(value))
            {
                RewindScalerIndex = Array.IndexOf(rewindScalerValues, value);
            }
        }
    }

    // This helper keeps the count of frames where the timeline didn't changed if the rewind scaler is between 0 & 1
    [SerializeField] private float rewindScalerHelper = 0;
    #endregion

    #region Singleton
    // Singleton instance of the Time Master Manager
    public static TimeMasterManager Instance = null;
    #endregion

    #region Methods
    #region Unity Methods
    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Set this as Singleton or destroy this if there is already one
        if (!Instance) Instance = this;
        else
        {
            Debug.Log("Two TimeMasterManager are not allowed in the same scene : Desctruction of the second instance !");
            Destroy(this);
        }
    }

    // This function is called every fixed framerate frame
    private void FixedUpdate()
    {
        ControlTime();
    }

    // Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy
    private void OnDestroy()
    {
        // Nullify the Singleton instance
        Instance = null;
    }

    // Use this for initialization
    private void Start ()
    {
        // Set the rewind scaler as 1
        RewindScaler = 1;
	}
	
	// Update is called once per frame
	private void Update ()
    {
        TimeControlInputs();
	}
    #endregion

    #region Original Methods
    /// <summary>
    /// Adds a new TimeControl object to the list
    /// </summary>
    /// <param name="_object">Object to add</param>
    public void AddTimeControlObject(TimeControlObject _object)
    {
        timeControlObjects.Add(_object);
        _object.RewindScaler = RewindScaler;
    }

    // Controls the time depending on the time state
    private void ControlTime()
    {
        switch (globalTimeState)
        {
            case TimeState.Pause:
                break;
            case TimeState.Play:
                Play();
                break;
            case TimeState.Rewind:
                Rewind();
                break;
            default:
                break;
        }
    }

    // Play time
    private void Play()
    {
        // Increases the length of the recorded timeline
        TimelineLength++;

        // Calls the record event
        OnRecord?.Invoke(timelineCurrentValue);
    }

    // Rewinds time
    private void Rewind()
    {
        // If the rewind scaler is between 0 & 1, do not change the timeline current value at each frame
        if (RewindScaler < 1)
        {
            // Increases the value of the helper by the value of the rewind scaler
            rewindScalerHelper += RewindScaler;

            // If it's superior or equal to one, then change the timeline & reset the helper
            if (rewindScalerHelper >= 1)
            {
                rewindScalerHelper--;
            }
            // If not, rewinds at the same timeline value
            else
            {
                OnRewind?.Invoke(timelineCurrentValue);
                return;
            }
        }

        // Changes the timeline depending if replay is allowed
        if (doAllowsReplay)
        {
            // Decreases the length of the recorded timeline
            TimelineCurrentValue -= Mathf.CeilToInt(RewindScaler) * (isReplaying ? -1 : 1);
        }
        else
        {
            // Decreases the length of the recorded timeline
            TimelineLength -= Mathf.CeilToInt(RewindScaler);
        }

        // Calls the rewind percentage event
        OnRewindGetPercentage?.Invoke((float)timelineCurrentValue / rewindTimelineLength);

        // Creates an empty list that will contain the recreated object
        List<GameObjectReconstructor> _recreatedObjects = new List<GameObjectReconstructor>();

        // If an destroyed object needs to be recreated, recreates it
        foreach (KeyValuePair<GameObjectReconstructor, int> entry in destroyedObjects)
        {
            if (entry.Value < timelineCurrentValue)
            {
                // Recreates the object
                entry.Key.Recreate();

                // Adds this key to the list of element to remove
                _recreatedObjects.Add(entry.Key);
            }
        }

        // Removes each recreated object from the destroyed objects list
        _recreatedObjects.ForEach(o => destroyedObjects.Remove(o));

        // If the timeline has been rewinded to its first state, set time state in pause
        if (timelineCurrentValue == 0)
        {
            GlobalTimeState = TimeState.Pause;
            return;
        }

        // If the timeline has been replayed to its last state, set time state to play
        if (doAllowsReplay && timelineCurrentValue == timelineLength)
        {
            GlobalTimeState = TimeState.Play;
            return;
        }

        // Calls the rewind event
        OnRewind?.Invoke(timelineCurrentValue);
    }

    // Saves a GameObject before it's destroyed to recreate it on rewind
    public void SaveObjectBeforeDestruction(GameObject _gameObject)
    {
        destroyedObjects.Add(new GameObjectReconstructor(_gameObject), timelineCurrentValue);
    }

    // Get the related user inputs and set the time control depending on them
    private void TimeControlInputs()
    {
        // Set the rewind scaler
        float _mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if (_mouseWheel > 0)
        {
            RewindScalerIndex++;
        }
        else if (_mouseWheel < 0)
        {
            RewindScalerIndex--;
        }

        // Set the time state of this object
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            GlobalTimeState = TimeState.Rewind;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1) && globalTimeState == TimeState.Rewind && !isReplaying)
        {
            GlobalTimeState = TimeState.Pause;
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (globalTimeState == TimeState.Pause) GlobalTimeState = TimeState.Play;
            else GlobalTimeState = TimeState.Pause;
        }

        // If replay is enable, required and possible, enable it
        else if (Input.GetKeyDown(KeyCode.Mouse0) && (timelineCurrentValue < timelineLength))
        {
            IsReplaying = true;

            if (GlobalTimeState == TimeState.Pause)
            {
                GlobalTimeState = TimeState.Rewind;
            }
        }
        // If the user wants to stop replay while rewinding, stop it
        else if (Input.GetKeyUp(KeyCode.Mouse0) && isReplaying)
        {
            IsReplaying = false;

            if (!Input.GetKey(KeyCode.Mouse1))
            {
                GlobalTimeState = TimeState.Pause;
            }
        }
    }
    #endregion
    #endregion
}
