using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeControlObject : MonoBehaviour
{
    /* TimeControlObject :
	*
	* Manages the notion of time of an object
    * 
    * This script should be attached on an object to control its time notion and relation
    * 
    * With this script, the user should be allowed to :
    *       - Pauses & Releases the time relativity of this object
    *       - Rewinds time, for this particular object
    *       - Increases & Decreases the speed scrolling of time for this object
    * 
    * An editor of this script is also available to specify precisely what to use and customize
    * the user control of time on this object
	*/

    #region Events
    /* Global Events
     * 
     * Events giving informations about the control of the time
     * These events can be very helpful to get feedback about what's going on
    */
    public event Action<float> OnRewindGetPercentage = null;
    public event Action<float> OnRewindScalerUpdate = null;
    public event Action<TimeState> OnTimeStateUpdate = null;

    /* Pause Event
     * 
     * Called when (un)pausing time for this object
     * (Bool parameter indicates if the pause is on or off)
    */
    public event Action<bool> OnPause = null;

    /* Record Events
     * 
     * Called when starting / ending record or just when recording time for this object
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRecordStarts = null;
    public event Action<int> OnRecord = null;
    public event Action<int> OnRecordEnds = null;

    /* Rewind Events
     * 
     * Called when starting / ending rewind or just when rewinding time for this object
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRewindStarts = null;
    public event Action<int> OnRewind = null;
    public event Action<int> OnRewindEnds = null;
    #endregion

    #region Fields / Accessors
    // ********** TIME STATE ********** //

    // Is this object's time independant or controled by the TimeMaster Manager ?
    [SerializeField] private bool isIndependant = false;
    public bool IsIndependant
    {
        get { return isIndependant; }
    }

    // The actual time state of the control of the time for this object
    [SerializeField] TimeState timeState = TimeState.Play;
    public TimeState TimeState
    {
        get { return timeState; }
        set
        {
            // When setting this TimeState property,
            // the timeState field is used as the previous TimeState value

            // Calls the associated event
            OnTimeStateUpdate?.Invoke(value);

            // First, executes actions depending on the previous state
            switch (timeState)
            {
                case TimeState.Pause:
                    OnPause?.Invoke(false);
                    break;
                case TimeState.Play:
                    OnRecordEnds?.Invoke(timelineCurrentValue);
                    break;
                case TimeState.Rewind:
                    // If we were replaying, set this boolean to false
                    if (IsReplaying) IsReplaying = false;
                    OnRewindEnds?.Invoke(timelineCurrentValue);
                    Debug.Log("End rewind => " + timelineCurrentValue);
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
            timeState = value;
        }
    }

    // ********** PAUSE ********** //

    // List of all the components to configure when pausing the object
    [SerializeField] List<ComponentPause> pauseComponents = new List<ComponentPause>();

    // ********** REWIND COMPONENTS ********** //

    // Components to rewind
    [SerializeField] private List<ComponentRewind> componentsRewind = new List<ComponentRewind>();
    public List<ComponentRewind> ComponentsRewind
    {
        get { return componentsRewind; }
    }

    // ********** TIMELINE ********** //

    // The length of the recorded timeline (in frame)
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

    // The current timeline state value (in frame)
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

    // Indicates if the rewind action is currently replaying or not
    [SerializeField] public bool IsReplaying = false;

    // ********** REWIND SCALER ********** //

    // The scaler of the rewind speed
    [SerializeField] private int rewindScalerIndex = 0;
    public int RewindScalerIndex
    {
        get { return rewindScalerIndex; }
        set
        {
            value = Mathf.Clamp(value, 0, TimeMasterManager.Instance.RewindScalerValues.Length - 1);

            rewindScalerIndex = value;
            rewindScalerHelper = 0;

            OnRewindScalerUpdate?.Invoke(TimeMasterManager.Instance.RewindScalerValues[value]);
        }
    }

    // The current rewind scaler
    public float RewindScaler
    {
        get { return TimeMasterManager.Instance.RewindScalerValues[rewindScalerIndex]; }
        set
        {
            if (TimeMasterManager.Instance.RewindScalerValues.Contains(value))
            {
                RewindScalerIndex = Array.IndexOf(TimeMasterManager.Instance.RewindScalerValues, value);
            }
        }
    }

    // This helper keeps the count of frames where the timeline didn't changed if the rewind scaler is between 0 & 1
    [SerializeField] private float rewindScalerHelper = 0;

    // ********** ANIMATOR ********** //

    // Should we rewind this object's animator ?
    [SerializeField] private bool doRewindAnimator = true;
    public bool DoRewindAnimator
    {
        get { return doRewindAnimator; }
    }

    // Animator of the GameObject
    [SerializeField] private Animator animator = null;
    public Animator Animator
    {
        get { return animator; }
    }

    // The animator rewind object
    [SerializeField] AnimatorRewind animatorRewind = null;
    #endregion

    #region Methods
    #region Unity Methods
    // Awake is called when the script instance is being loaded
    void Awake()
    {

    }

    // This function is called every fixed framerate frame
    private void FixedUpdate()
    {
        // If this object is not independant, let it controls its time
        if (!isIndependant)
        {
            TimeControl();
        }
    }

    // Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy
    private void OnDestroy()
    {
        if (TimeMasterManager.Instance) TimeMasterManager.Instance.SaveObjectBeforeDestruction(gameObject);
    }

    // Use this for initialization
    private void Start ()
    {
        // Initializes this object
        InitializesObject();
    }

    // Update is called once per frame
    private void Update ()
    {

    }
    #endregion

    #region Original Methods
    // Initializes this object for Play !
    public void InitializesObject()
    {
        // If there is no TimeMasterManager in the scene, debug it and destroy this object
        if (TimeMasterManager.Instance == null)
        {
            Debug.Log("There is no TimeMasterManager in the scene ! Destruction of the TimeControl object(s) in coming...");
            Destroy(this);
            return;
        }

        // If the animations are rewinded, set the events
        if (doRewindAnimator)
        {
            if (animator)
            {
                #region Animator.PlayBack
                /*OnRecordStart += (() => animator.StartRecording(10000));
                OnRecordEnd += (() => animator.StopRecording());

                OnRewindStart += (() => animator.StartPlayback());
                OnRewindStart += (() => animator.playbackTime = animator.recorderStopTime);
                OnRewind += ((int _i) => animator.playbackTime -= 0.02f);
                OnRewind += ((int _i) => Debug.Log("Start time : " + animator.recorderStartTime + " | Stop time : " + animator.recorderStopTime));
                OnRewind += ((int _i) => Debug.Log("Playback Time : " + animator.playbackTime));
                OnRewindEnd += (() => animator.StopPlayback());*/
                #endregion

                animatorRewind = new AnimatorRewind(animator, this);
            }
        }

        // Initializes each rewinding component
        componentsRewind = componentsRewind.Where(c => c.Component != null).ToList();
        componentsRewind.ForEach(c => c.InitializeObject(this));

        // Get all the components to configure when pausing the object
        GetComponentsInChildren<Component>().ToList().ForEach(c => pauseComponents.Add(new ComponentPause(c, this)));
        pauseComponents = pauseComponents.Where(p => p.Type != ComponentPause.ComponentType.Other).ToList();

        // Adds this object to the TimeMasterManager's list
        TimeMasterManager.Instance.AddTimeControlObject(this);
    }

    // Initializes the ComponentRewind objects
    public void InitializesLostValues()
    {
        foreach (ComponentRewind _component in componentsRewind)
        {
            if (_component.Component != null)
            {
                _component.InitializeLostValues();
            }
        }
    }

    // Records the actual state of this object for each component listed
    private void RecordState()
    {
        // Increases the length of the recorded timeline
        TimelineLength++;

        // Record the state of each component
        OnRecord?.Invoke(timelineCurrentValue);
    }

    // Rewinds the state of this object for each component listed
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
                rewindScalerHelper = 0;
            }
            // If not, rewinds at the same timeline value
            else
            {
                OnRewind?.Invoke(timelineCurrentValue);
                return;
            }
        }

        // Changes the timeline depending if replay is allowed
        if (TimeMasterManager.Instance.DoAllowsReplay)
        {
            // Decreases the length of the recorded timeline
            TimelineCurrentValue -= Mathf.CeilToInt(RewindScaler) * (IsReplaying ? -1 : 1);
        }
        else
        {
            // Decreases the length of the recorded timeline
            TimelineLength -= Mathf.CeilToInt(RewindScaler);
        }

        // Call the rewind event
        OnRewindGetPercentage?.Invoke((float)timelineCurrentValue / rewindTimelineLength);

        // If the timeline has been rewinded to its first state, set the object in pause
        if (timelineCurrentValue == 0)
        {
            TimeState = TimeState.Pause;
            return;
        }

        // If the timeline has been replayed to its last state, play the object
        if (TimeMasterManager.Instance.DoAllowsReplay && timelineCurrentValue == timelineLength)
        {
            TimeState = TimeState.Play;
            return;
        }

        // Rewind the state of each component
        OnRewind?.Invoke(timelineCurrentValue);
    }

    // Here is exercised the control on this object's time
	private void TimeControl()
    {
        // Manages the time scrolling of this object
        switch (timeState)
        {
            case TimeState.Pause:
                // Nothing is happening
                break;

            case TimeState.Play:
                // Let the life goes on
                RecordState();
                break;

            case TimeState.Rewind:
                // Rewind this object's timeline
                Rewind();
                break;

            default:
                // Well, I don't know what's happening here, this is a mystery
                break;
        }
    }
    #endregion
    #endregion
}
