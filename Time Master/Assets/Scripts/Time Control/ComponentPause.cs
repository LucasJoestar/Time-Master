using System;
using UnityEngine;

[Serializable]
public class ComponentPause
{
    /* ComponentPause :
     * 
     * Contains all what's needed for the components to pause
    */

    #region Enum
    // The types of ComponentPause managed
    public enum ComponentType
    {
        Behaviour,
        Other,
        Rigidbody,
        Rigidbody2D
    }
    #endregion

    #region Fields / Accessors
    // Component related to this object
    [SerializeField] private Component component = null;
    public Component Component
    {
        get { return component; }
    }

    // The type of this component
    [SerializeField] private ComponentType type = ComponentType.Other;
    public ComponentType Type
    {
        get { return type; }
    }

    // The last value of this component before pausing it
    [SerializeField] private object stateBeforePause = null;
    public object StateBeforePause
    {
        get { return stateBeforePause; }
    }

    /*
     ********** RIGIDBODY **********
    */

    // If this component is a Rigidbody / Rigidbody2D, this is its velocity before pausing it
    [SerializeField] private Vector3 velocityBeforePause = Vector3.zero;
    public Vector3 VelocityBeforePause
    {
        get { return velocityBeforePause; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Constructs a new ComponentPause object and initializes it
    /// </summary>
    /// <param name="_component">Component to pause</param>
    public ComponentPause(Component _component, TimeControlObject _source)
    {
        component = _component;

        // Set the type of the component depending on what this is
        if (component is Behaviour && !(component is TimeControlObject))
        {
            type = ComponentType.Behaviour;
        }
        else if (component is Rigidbody)
        {
            type = ComponentType.Rigidbody;
        }
        else if (component is Rigidbody2D)
        {
            type = ComponentType.Rigidbody2D;
        }
        else
        {
            type = ComponentType.Other;
        }

        // Add the pause method to the pause event of its ObjectTimeControl
        _source.OnPause += Pause;
    }
    #endregion

    #region Method
    // Pause / Unpause this component
    private void Pause(bool _doPause)
    {
        if (component == null) return;
        switch (type)
        {
            case ComponentType.Behaviour:
                Behaviour _behaviour = (Behaviour)component;

                if (_doPause)
                {
                    stateBeforePause = _behaviour.enabled;
                    _behaviour.enabled = false;
                }
                else
                {
                    _behaviour.enabled = (bool)stateBeforePause;
                }
                break;

            case ComponentType.Rigidbody:
                Rigidbody _rigidbody = (Rigidbody)component;

                if (_doPause)
                {
                    stateBeforePause = _rigidbody.isKinematic;
                    velocityBeforePause = _rigidbody.velocity;
                    _rigidbody.isKinematic = true;
                }
                else
                {
                    _rigidbody.isKinematic = (bool)stateBeforePause;
                    _rigidbody.velocity = velocityBeforePause;
                }
                break;

            case ComponentType.Rigidbody2D:
                Rigidbody2D _rigidbody2D = (Rigidbody2D)component;

                if (_doPause)
                {
                    stateBeforePause = _rigidbody2D.bodyType;
                    velocityBeforePause = _rigidbody2D.velocity;
                    _rigidbody2D.bodyType = RigidbodyType2D.Static;
                }
                else
                {
                    _rigidbody2D.bodyType = (RigidbodyType2D)stateBeforePause;
                    _rigidbody2D.velocity = velocityBeforePause;
                }
                break;

            case ComponentType.Other:
                // Unknown case, just doesn't know what to do
                break;

            default:
                break;
        }
    }
    #endregion
}
