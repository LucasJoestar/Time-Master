using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AnimatorRewind
{
    /* AnimatorRewind :
     * 
     * PENSER À REMBOBINER CORRECTEMENT FIREEVENTS
    */

    #region Fields / Accessors
    // The animator to rewind
    [SerializeField] private Animator animator = null;
    public Animator Animator
    {
        get { return animator; }
    }

    // The number of layer in the animator
    [SerializeField] private int layerCount = 1;
    public int LayerCount
    {
        get { return layerCount; }
    }

    // The recorded states of the animator ; key is the layer for its recorded states
    [SerializeField] private List<AnimatorRewind_Layer> layers = new List<AnimatorRewind_Layer>();
    public List<AnimatorRewind_Layer> Layers
    {
        get { return layers; }
    }

    // The recorded parameters values of the animator
    [SerializeField] private List<AnimatorRewind_Parameter> parameters = new List<AnimatorRewind_Parameter>();
    public List<AnimatorRewind_Parameter> Parameters
    {
        get { return parameters; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a brand new AnimatorRewind with a given animator
    /// </summary>
    /// <param name="_animator">Animator to rewind</param>
    public AnimatorRewind(Animator _animator, TimeControlObject _source)
    {
        // Assigns the animator and its layer count
        animator = _animator;
        layerCount = animator.layerCount;

        // For each layer, creates a new AnimatorRewind_Layer object
        for (int _i = 0; _i < layerCount; _i++)
        {
            layers.Add(new AnimatorRewind_Layer(animator, _i));
        }

        // For each parameter of the animator, creates a new AnimatorControllerParameter with its main properties (Name & Type)
        foreach (AnimatorControllerParameter _parameter in animator.parameters)
        {
            // If it's a trigger, do not add it to the list (No utility)
            if (_parameter.type != AnimatorControllerParameterType.Trigger)
            {
                parameters.Add(new AnimatorRewind_Parameter(_parameter.name, _parameter.type));
            }
        }

        // Adds to the ObjectTimeControl source of this object the needed methods on events
        _source.OnRecordStarts += ((int _timeline) => animator.fireEvents = true);
        _source.OnRecordStarts += ((int _timeline) => animator.speed = 1);
        _source.OnRecord += Record;
        _source.OnRewindStarts += ((int _timeline) => animator.fireEvents = false);
        _source.OnRewindStarts += ((int _timeline) => animator.speed = 0);

        if (TimeMasterManager.Instance.DoAllowsReplay)
        {
            _source.OnRewind += SetTimeline;
            _source.OnRecordStarts += RemoveTimeline;
        }
        else
        {
            _source.OnRewindStarts += SetTimeline;
            _source.OnRewind += RemoveAndSetTimeline;
        }

        // Records the first state
        Record(0);
    }
    #endregion

    #region Methods
    // Record the animator states & parameters
    private void Record(int _timelineValue)
    {
        /*
         ********** STATES **********
        */

        // Record the state of each layer
        for (int _i = 0; _i < layerCount; _i++)
        {
            layers[_i].Record();
        }

        /*
         ********** PARAMETERS **********
        */

        parameters.ForEach(p => p.Record(animator));
    }

    // Removes unnecessarily entries and set values on the indicated timeline value
    private void RemoveAndSetTimeline(int _timeline)
    {
        /*
         ********** STATES **********
        */

        // For each layer, rewind its state
        for (int _i = 0; _i < layerCount; _i++)
        {
            layers[_i].RemoveAndSetTimeline(_timeline);
        }

        /*
         ********** PARAMETERS **********
        */

        parameters.ForEach(p => p.RemoveAndSetTimeline(animator, _timeline));
    }

    // Removes unnecessarily entries on the indicated timeline value
    private void RemoveTimeline(int _timeline)
    {
        /*
     ********** STATES **********
    */

        // For each layer, rewind its state
        for (int _i = 0; _i < layerCount; _i++)
        {
            layers[_i].RemoveTimeline(_timeline);
        }

        /*
         ********** PARAMETERS **********
        */

        parameters.ForEach(p => p.RemoveTimeline(_timeline));
    }

    // Set values on the indicated timeline value without removing
    private void SetTimeline(int _timeline)
    {
        /*
         ********** STATES **********
        */

        for (int _i = 0; _i < layerCount; _i++)
        {
            layers[_i].SetTimeline(_timeline);
        }

        /*
         ********** PARAMETERS **********
        */

        parameters.ForEach(p => p.SetTimeline(animator, _timeline));
    }
    #endregion
}

[Serializable]
public class AnimatorRewind_Layer
{
    /* AnimatorRewind_Layer :
     * 
     * 
    */

    #region Fields / Accessors
    // The animator of this layer
    [SerializeField] private Animator animator = null;
    public Animator Animator
    {
        get { return animator; }
    }

    // This layer value
    [SerializeField] private int layer = 0;
    public int Layer
    {
        get { return layer; }
    }

    // The list of recorded states for this layer
    [SerializeField] private List<AnimatorRewind_State> states = new List<AnimatorRewind_State>();
    public List<AnimatorRewind_State> States
    {
        get { return states; }
    }

    /*
     ********** REWIND **********
    */

    // The index of the current state playing
    [SerializeField] private int currentStateIndex = 0;

    // The index of the current state's current frame playing
    [SerializeField] private int currentStateFrameIndex = 0;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new AnimatorRewind_Layer object to rewind an animator's layer
    /// </summary>
    /// <param name="_animator">Animator to rewind</param>
    /// <param name="_layer">Layer to rewind</param>
    public AnimatorRewind_Layer(Animator _animator, int _layer)
    {
        // Set object fields values
        animator = _animator;
        layer = _layer;
    }
    #endregion

    #region Methods
    // Records this layer actual state
    public void Record()
    {
        // Get informations about the animator current state
        int _stateHash = animator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
        bool _isTransition = animator.IsInTransition(layer);

        // If there is no state already recorded, records the first one
        if (states.Count < 1)
        {
            if (_isTransition)
            {
                AnimatorTransitionInfo _transitionInfos = animator.GetAnimatorTransitionInfo(layer);

                states.Add(new AnimatorRewind_State(_stateHash, animator.GetNextAnimatorStateInfo(layer).fullPathHash, _transitionInfos.duration, _transitionInfos.durationUnit));
            }
            else
            {
                states.Add(new AnimatorRewind_State(_stateHash));
            }
        }

        // Get the last recorded state
        AnimatorRewind_State _lastState = states.Last();

        // Get to know if the last recorded state hash is different than the current one
        bool _isStateHashDifferent = _stateHash != _lastState.CurrentStateHash;

        // If the animator's actual state is different than the last recorded one
        if (_isTransition)
        {
            int _nextStateHash = animator.GetNextAnimatorStateInfo(layer).fullPathHash;
            AnimatorTransitionInfo _transitionInfos = animator.GetAnimatorTransitionInfo(layer);

            if (_isStateHashDifferent || !_lastState.IsTransition || _lastState.NextStateHash != _nextStateHash || _lastState.TransitionDuration != _transitionInfos.duration)
            {
                AnimatorRewind_State _newState = new AnimatorRewind_State(_stateHash, _nextStateHash, _transitionInfos.duration, _transitionInfos.durationUnit);

                states.Add(_newState);
            }
        }
        else if (_lastState.IsTransition || _isStateHashDifferent)
        {
            AnimatorRewind_State _newState = new AnimatorRewind_State(_stateHash);

            states.Add(_newState);
        }

        // Records the frame values of the last registered state
        states.Last().Record(animator, layer);
    }

    /*
     ******************* REWIND & REPLAY *******************
    */

    // Removes unnecessarily entries and set values on the indicated timeline value
    public void RemoveAndSetTimeline(int _timelineValue)
    {
        // Set the indexs on the timeline
        SetIndexs(_timelineValue);

        // Creates two int to indicates the index where to start remove and the quantity to remove
        int _indexFromToRemove = currentStateIndex + 1;
        int _toRemove = states.Count - _indexFromToRemove;

        // Removes the needed state(s)
        if (_toRemove > 0)
        {
            states.RemoveRange(_indexFromToRemove, _toRemove);
        }

        // Get the current state
        AnimatorRewind_State _currentState = states[currentStateIndex];

        // Set the helpers int
        _indexFromToRemove = currentStateFrameIndex + 1;
        _toRemove = _currentState.CurrentStateNormalizedTimes.Count - _indexFromToRemove;

        // Removes the needed frame(s)
        if (_toRemove > 0)
        {
            _currentState.CurrentStateNormalizedTimes.RemoveRange(_indexFromToRemove, _toRemove);

            if (_currentState.IsTransition)
            {
                _currentState.NextStateTimes.RemoveRange(_indexFromToRemove, _toRemove);
                _currentState.TransitionNormalizedTimes.RemoveRange(_indexFromToRemove, _toRemove);
            }
        }

        // Rewind to the needed frame
        states[currentStateIndex].Rewind(animator, layer, currentStateFrameIndex);
    }

    // Removes unnecessarily entries on the indicated timeline value
    public void RemoveTimeline(int _timelineValue)
    {
        // Set the indexs on the timeline
        SetIndexs(_timelineValue);

        // Creates two int to indicates the index where to start remove and the quantity to remove
        int _indexFromToRemove = currentStateIndex + 1;
        int _toRemove = states.Count - _indexFromToRemove;

        // Removes the needed state(s)
        if (_toRemove > 0)
        {
            states.RemoveRange(_indexFromToRemove, _toRemove);
        }

        // Get the current state
        AnimatorRewind_State _currentState = states[currentStateIndex];

        // Set the helpers int
        _indexFromToRemove = currentStateFrameIndex + 1;
        _toRemove = _currentState.CurrentStateNormalizedTimes.Count - _indexFromToRemove;

        // Removes the needed frame(s)
        if (_toRemove > 0)
        {
            _currentState.CurrentStateNormalizedTimes.RemoveRange(_indexFromToRemove, _toRemove);

            if (_currentState.IsTransition)
            {
                _currentState.NextStateTimes.RemoveRange(_indexFromToRemove, _toRemove);
                _currentState.TransitionNormalizedTimes.RemoveRange(_indexFromToRemove, _toRemove);
            }
        }
    }

    // Set values on the indicated timeline value without removing
    public void SetTimeline(int _timelineValue)
    {
        // Set the indexs of the timeline
        SetIndexs(_timelineValue);

        // Rewind to the needed frame
        states[currentStateIndex].Rewind(animator, layer, currentStateFrameIndex);
    }

    /*
     ******************* OTHER UTILITY *******************
    */

    // Set the indexs of the current state & current frame for rewind
    private void SetIndexs(int _timelineValue)
    {
        // Get the frames gap, creates a utility int and reset fields
        int _framesGap = _timelineValue + 1;
        int currentStateFrames = 0;

        currentStateIndex = 0;
        currentStateFrameIndex = 0;

        // Creates a loop that runs the states and their frames to convert the timeline value in state & frame index
        while (true)
        {
            currentStateFrames = states[currentStateIndex].Duration;

            if (currentStateFrames < _framesGap)
            {
                _framesGap -= currentStateFrames;
                currentStateIndex++;
            }
            else
            {
                currentStateFrameIndex = _framesGap - 1;
                break;
            }
        }
    }
    #endregion
}

[Serializable]
public class AnimatorRewind_State
{
    /* AnimatorState :
     * 
     * 
    */

    #region Fields / Accessors
    // The duration of this state, in frame
    public int Duration
    {
        get { return CurrentStateNormalizedTimes.Count; }
    }

    // The current state hash
    [SerializeField] private int currentStateHash = 0;
    public int CurrentStateHash
    {
        get { return currentStateHash; }
    }

    // Is this a transitionnal state
    [SerializeField] private bool isTransition = false;
    public bool IsTransition
    {
        get { return isTransition; }
    }

    // The list containing the normalized time of the current state at each frame
    [SerializeField] public List<float> CurrentStateNormalizedTimes = new List<float>();

    /*
     ********** IF TRANSITION **********
    */

    // The animation state to transit to
    [SerializeField] private int nextStateHash = 0;
    public int NextStateHash
    {
        get { return nextStateHash; }
    }

    // The duration of the transition
    [SerializeField] private float transitionDuration = 0;
    public float TransitionDuration
    {
        get { return transitionDuration; }
    }

    // The duration unit of this transition
    [SerializeField] DurationUnit durationUnit = DurationUnit.Fixed;
    public DurationUnit DurationUnit
    {
        get { return durationUnit; }
    }

    // The list containing the normalized time of the transition at each frame
    [SerializeField] public List<float> TransitionNormalizedTimes = new List<float>();
    // The list containing the time of the next state at each frame
    [SerializeField] public List<float> NextStateTimes = new List<float>();
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new Animator Rewind State not in transition
    /// </summary>
    /// <param name="_currentStateHash">Hash name of the state</param>
    public AnimatorRewind_State(int _currentStateHash)
    {
        isTransition = false;
        currentStateHash = _currentStateHash;
    }

    /// <summary>
    /// Creates a new Animator Rewind State in transition
    /// </summary>
    /// <param name="_currentStateHash">Current state hash name</param>
    /// <param name="_nextStateHash">Next state hash name</param>
    /// <param name="_transitionDuration">Duration of this transition</param>
    /// <param name="_durationUnit">Unit of the duration</param>
    public AnimatorRewind_State(int _currentStateHash, int _nextStateHash, float _transitionDuration, DurationUnit _durationUnit)
    {
        isTransition = true;
        currentStateHash = _currentStateHash;
        nextStateHash = _nextStateHash;
        transitionDuration = _transitionDuration;
        durationUnit = _durationUnit;
    }
    #endregion

    #region Methods
    // Increases the duration of this state
    public void Record(Animator _animator, int _layer)
    {
        // Get the normalized time of the current state
        float _currentStateNormalizedTime = _animator.GetCurrentAnimatorStateInfo(_layer).normalizedTime;

        // If it's a transition
        if (isTransition)
        {
            // Registered the current normalized time of the transition
            TransitionNormalizedTimes.Add(_animator.GetAnimatorTransitionInfo(_layer).normalizedTime);

            // Registered the current time of the next state
            AnimatorStateInfo _nextStateInfos = _animator.GetNextAnimatorStateInfo(_layer);
            NextStateTimes.Add(_nextStateInfos.normalizedTime * (durationUnit == DurationUnit.Normalized ? 1 : _nextStateInfos.length));
        }
        // Registers the current normalied time of the current state
        CurrentStateNormalizedTimes.Add(_currentStateNormalizedTime);
    }

    // Rewind this state
    public void Rewind(Animator _animator, int _layer, int _timelineFrame)
    {
        // Plays the state that is going to transit at tge disered normalized time
        _animator.Play(currentStateHash, _layer, CurrentStateNormalizedTimes[_timelineFrame]);

        // If it's a transition
        if (isTransition)
        {
            // Updates the animator
            _animator.Update(Time.deltaTime);

            // Crossfades at the disered normalized time using a method depending on the duration unit and update the animator
            if (durationUnit == DurationUnit.Normalized)
            {
                _animator.CrossFade(nextStateHash, transitionDuration, _layer, NextStateTimes[_timelineFrame], TransitionNormalizedTimes[_timelineFrame]);
            }
            else
            {
                _animator.CrossFadeInFixedTime(nextStateHash, transitionDuration, _layer, NextStateTimes[_timelineFrame], TransitionNormalizedTimes[_timelineFrame]);
            }
            _animator.Update(Time.deltaTime);
        }
    }
    #endregion
}

[Serializable]
public class AnimatorRewind_Parameter
{
    /* AnimatorRewind_Parameters :
     * 
     * 
    */

    #region Fields / Accessors
    // The name of the parameter
    [SerializeField] private string name = string.Empty;
    public string Name
    {
        get { return name; }
    }

    // The type of this parameter
    [SerializeField] private AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool;
    public AnimatorControllerParameterType Type
    {
        get { return type; }
    }

    // The past values of this parameter
    [SerializeField] private List<object> pastValues = new List<object>();
    public List<object> PastValues
    {
        get { return pastValues; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new Animator Rewind Parameter to rewind animator's parameter
    /// </summary>
    /// <param name="_name">Name of the parameter</param>
    /// <param name="_type">Type of the parameter</param>
    public AnimatorRewind_Parameter(string _name, AnimatorControllerParameterType _type)
    {
        name = _name;
        type = _type;
    }
    #endregion

    #region Methods
    // Records the value of the parameter
    public void Record(Animator _animator)
    {
        object _value = null;

        // Depending on the parameter type, get its value
        switch (type)
        {
            case AnimatorControllerParameterType.Float:
                _value = _animator.GetFloat(name);
                break;

            case AnimatorControllerParameterType.Int:
                _value = _animator.GetInteger(name);
                break;

            case AnimatorControllerParameterType.Bool:
                _value = _animator.GetBool(name);
                break;

            default:
                break;
        }

        // Adds this value to the list
        pastValues.Add(_value);
    }

    /*
     ******************* REWIND & REPLAY *******************
    */

    // Removes unnecessarily entries and set values on the indicated timeline value
    public void RemoveAndSetTimeline(Animator _animator, int _timelineValue)
    {
        // The value of the timeline + 1
        int _timelinePlus = _timelineValue + 1;

        // Removes their value at instants more advanced than the indicated timeline and set their value to that at this moment
        pastValues.RemoveRange(_timelinePlus, pastValues.Count - _timelinePlus);

        // Depending on the parameter type, set the parameter value on the animator
        switch (type)
        {
            case AnimatorControllerParameterType.Float:
                _animator.SetFloat(name, (float)pastValues[_timelineValue]);
                break;

            case AnimatorControllerParameterType.Int:
                _animator.SetInteger(name, (int)pastValues[_timelineValue]);
                break;

            case AnimatorControllerParameterType.Bool:
                _animator.SetBool(name, (bool)pastValues[_timelineValue]);
                break;

            case AnimatorControllerParameterType.Trigger:
                break;

            default:
                break;
        }
    }

    // Removes unnecessarily entries on the indicated timeline value
    public void RemoveTimeline(int _timelineValue)
    {
        // The value of the timeline + 1
        int _timelinePlus = _timelineValue + 1;

        // Removes their value at instants more advanced than the indicated timeline
        pastValues.RemoveRange(_timelinePlus, pastValues.Count - _timelinePlus);
    }

    // Set values on the indicated timeline value without removing
    public void SetTimeline(Animator _animator, int _timelineValue)
    {
        // Depending on the parameter type, set the parameter value on the animator
        switch (type)
        {
            case AnimatorControllerParameterType.Float:
                _animator.SetFloat(name, (float)pastValues[_timelineValue]);
                break;

            case AnimatorControllerParameterType.Int:
                _animator.SetInteger(name, (int)pastValues[_timelineValue]);
                break;

            case AnimatorControllerParameterType.Bool:
                _animator.SetBool(name, (bool)pastValues[_timelineValue]);
                break;

            default:
                break;
        }
    }
    #endregion
}
