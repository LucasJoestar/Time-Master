using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class ComponentRewind
{
    /* ComponentRewind :
     * 
     * Contains and describes this component used characteristics for Rewind
    */

    #region Events
    /* Pause Event
     * 
     * Called when (un)pausing time for this component
     * (Bool parameter indicates if the pause is on or off)
    */
    public event Action<bool> OnPause = null;

    /* Record Events
     * 
     * Called when starting / ending record or just when recording time for this component
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRecordStarts = null;
    public event Action<int> OnRecord = null;
    public event Action<int> OnRecordEnds = null;

    /* Rewind Events
     * 
     * Called when starting / ending rewind or just when rewinding time for this component
     * (Int parameter indicates the timeline frame)
    */
    public event Action<int> OnRewindStarts = null;
    public event Action<int> OnRewind = null;
    public event Action<int> OnRewindEnds = null;
    #endregion

    #region Fields / Accessors
    // ********** COMPONENT CHARACTERISTICS ********** //

    // What component this is - the most important
    [SerializeField] private Component component = null;
    public Component Component
    {
        get { return component; }
    }

    // The type of this component
    [SerializeField] private Type type = null;
    public Type Type
    {
        get { return type; }
    }

    // The name of the component, indicates its type
    [SerializeField] private string name = string.Empty;
    public string Name
    {
        get { return name; }
    }

    // The GameObject the component is from
    [SerializeField] private GameObject source = null;

    // Is this component a MonoBehaviour script ?
    [SerializeField] private bool isMonoBehaviour = false;
    public bool IsMonoBehaviour
    {
        get { return isMonoBehaviour; }
    }

    private MonoBehaviour monoBehaviour = null;

    // ********** DESTRUCTION ********** //

    // Indicates if the component is or was destroyed
    [SerializeField] private bool isDestroyed = false;
    public bool IsDestroyed
    {
        get { return isDestroyed; }
    }
    [SerializeField] private bool wasDestroyed = false;
    public bool WasDestroyed
    {
        get { return wasDestroyed; }
        private set
        {
            wasDestroyed = value;
            isDestroyed = value;
        }
    }

    // The frame when the component was destroyed
    [SerializeField] private int destructionFrame = 0;

    // ********** FIELDS & PROPERTIES ********** //

    /* Fields Infos
     * 
     * Contains all the fields of the component used for Rewind
    */
    [SerializeField] private List<FieldInfoRewind> fieldsRewind = new List<FieldInfoRewind>();
    public List<FieldInfoRewind> FieldsRewind
    {
        get { return fieldsRewind; }
    }

    /* Properties Infos
     * 
     * Contains all the properties of the component used for Rewind
    */
    [SerializeField] private List<PropertyInfoRewind> propertiesRewind = new List<PropertyInfoRewind>();
    public List<PropertyInfoRewind> PropertiesRewind
    {
        get { return propertiesRewind; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Constructs a new ComponentRewind object and initializes its variables depending on its component
    /// </summary>
    /// <param name="_component">Component to rewind</param>
    public ComponentRewind(Component _component)
    {
        // Set the component of this object
        component = _component;

        // Get the type of the component
        type = component.GetType();

        // Set the GameObject source of this component
        source = component.gameObject;

        // Check if the component is a MonoBehaviour script
        isMonoBehaviour = component is MonoBehaviour;

        // Set the name of the object
        name = isMonoBehaviour ? type.ToString() : type.ToString().Split('.')[1];

        // Initializes the fields & properties of this component when this object is created
        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(f => !f.IsDefined(typeof(ObsoleteAttribute))).ToList().ForEach(f => fieldsRewind.Add(new FieldInfoRewind(f)));

        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(p => p.CanRead && p.CanWrite && !p.IsDefined(typeof(ObsoleteAttribute))).ToList().ForEach(p => propertiesRewind.Add(new PropertyInfoRewind(p)));
    }
    #endregion

    #region Methods
    // ********** INITIALIZATIONS ********** //

    // Initiliazes this object for Play !
    public void InitializeObject(TimeControlObject _source)
    {
        // Initializes the values of the object (component + fields & rewind
        InitializeLostValues();

        // Get the time 0 value of each field & property
        fieldsRewind.ForEach(f => f.PastValues.Add(f.FieldInfo.GetValue(component)));
        propertiesRewind.ForEach(p => p.PastValues.Add(p.PropertyInfo.GetValue(component)));

        // Get if the entries should be deleted on rewind or kept
        bool _doAllowsReplay = TimeMasterManager.Instance.DoAllowsReplay;

        // Configures this object events
        OnRecord += Record;

        // Configures events depending if the component is MonoBehaviour or not
        if (isMonoBehaviour)
        {
            monoBehaviour = (MonoBehaviour)component;

            OnRewindStarts += ((int _timeline) => monoBehaviour.enabled = false);

            if (_doAllowsReplay)
            {
                OnRewindEnds += SetTimeline;
                OnRecordStarts += RemoveTimeline;
            }
            else
            {
                OnRewindEnds += RemoveAndSetTimeline;
            }
        }
        else
        {
            if (_doAllowsReplay)
            {
                OnRewind += SetTimeline;
                OnRecordStarts += RemoveTimeline;
            }
            else
            {
                OnRewindStarts += SetTimeline;
                OnRewind += RemoveAndSetTimeline;
            }
        }

        // Configures the events of the ObjectTimeControl source
        if (OnRecord != null) _source.OnRecord += ((int _timelineValue) => CheckComponent(OnRecord, _timelineValue));
        if (OnRecordEnds != null) _source.OnRecordEnds += ((int _timelineValue) => CheckComponent(OnRecordEnds, _timelineValue));
        if (OnRecordStarts != null) _source.OnRecordStarts += ((int _timelineValue) => CheckComponent(OnRecordStarts, _timelineValue));
        if (OnRewind != null) _source.OnRewind += ((int _timelineValue) => CheckComponent(OnRewind, _timelineValue));
        if (OnRewindEnds != null) _source.OnRewindEnds += ((int _timelineValue) => CheckComponent(OnRewindEnds, _timelineValue));
        if (OnRewindStarts != null) _source.OnRewindStarts += ((int _timelineValue) => CheckComponent(OnRewindStarts, _timelineValue));
    }

    // Initializes the fields of the object that can be lost between play and edit mode
    public void InitializeLostValues()
    {
        // Initializes the type of the component
        type = component.GetType();

        // Initializes the fields & properties rewind ; if their field info / property info does no longer exist, remove them
        fieldsRewind.Where(f => !f.InitializeField(type)).ToList().ForEach(f => fieldsRewind.Remove(f));
        propertiesRewind.Where(p => !p.InitializeProperty(type)).ToList().ForEach(p => propertiesRewind.Remove(p));

        // Get the new fields & properties of the component
        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(f => !fieldsRewind.Any(r => r.FieldInfo == f) && !f.IsDefined(typeof(ObsoleteAttribute))).ToList().ForEach(f => fieldsRewind.Add(new FieldInfoRewind(f)));

        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(p => !propertiesRewind.Any(r => r.PropertyInfo == p) && p.CanRead && p.CanWrite && !p.IsDefined(typeof(ObsoleteAttribute))).ToList().ForEach(p => propertiesRewind.Add(new PropertyInfoRewind(p)));

        // Order the fields & properties rewind lists by name
        fieldsRewind.OrderBy(f => f.Name);
        propertiesRewind.OrderBy(p => p.Name);
    }

    // ********** RECORD ********** //

    // Record the state of the fields and properties of this component
    private void Record(int _timelineValue)
    {
        // If the component has just been destroyed, mark it and return
        if (component == null)
        {
            WasDestroyed = true;
            destructionFrame = _timelineValue;
            return;
        }

        // Records the fields values of this component
        foreach (FieldInfoRewind _field in fieldsRewind.Where(f => f.IsRewinded))
        {
            _field.PastValues.Add(_field.FieldInfo.GetValue(component));
        }

        // Records the properties values of this component
        foreach (PropertyInfoRewind _property in propertiesRewind.Where(p => p.IsRewinded))
        {
            _property.PastValues.Add(_property.PropertyInfo.GetValue(component));
        }
    }

    // ********** REWIND ********** //

    // Removes unnecessarily entries and set values on the indicated timeline value
    private void RemoveAndSetTimeline(int _timelineValue)
    {
        // The value of the timeline + 1
        int _timelinePlus = _timelineValue + 1;

        // For each field & property, remove their value at instants more advanced than the indicated timeline and set their value to that at this moment
        foreach (FieldInfoRewind _field in fieldsRewind.Where(f => f.IsRewinded))
        {
            _field.PastValues.RemoveRange(_timelinePlus, _field.PastValues.Count - _timelinePlus);
            _field.FieldInfo.SetValue(component, _field.PastValues[_timelineValue]);
        }

        foreach (PropertyInfoRewind _property in propertiesRewind.Where(p => p.IsRewinded))
        {
            _property.PastValues.RemoveRange(_timelinePlus, _property.PastValues.Count - _timelinePlus);
            _property.PropertyInfo.SetValue(component, _property.PastValues[_timelineValue]);
        }
    }

    // Removes unnecessarily entries on the indicated timeline value
    private void RemoveTimeline(int _timelineValue)
    {
        // The value of the timeline + 1
        int _timelinePlus = _timelineValue + 1;

        // For each field & property, remove their value at instants more advanced than the indicated timeline
        foreach (FieldInfoRewind _field in fieldsRewind.Where(f => f.IsRewinded))
        {
            _field.PastValues.RemoveRange(_timelinePlus, _field.PastValues.Count - _timelinePlus);
        }

        foreach (PropertyInfoRewind _property in propertiesRewind.Where(p => p.IsRewinded))
        {
            _property.PastValues.RemoveRange(_timelinePlus, _property.PastValues.Count - _timelinePlus);
        }
    }

    // Set values on the indicated timeline value without removing
    private void SetTimeline(int _timelineValue)
    {
        // For each field & property, set their value to that at the moment of the indicated timeline
        foreach (FieldInfoRewind _field in fieldsRewind.Where(f => f.IsRewinded))
        {
            _field.FieldInfo.SetValue(component, _field.PastValues[_timelineValue]);
        }

        foreach (PropertyInfoRewind _property in propertiesRewind.Where(p => p.IsRewinded))
        {
            _property.PropertyInfo.SetValue(component, _property.PastValues[_timelineValue]);
        }
    }

    // ********** UTILITIES ********** //

    // Check if the component is destroyed before calling the associated event
    private void CheckComponent(Action<int> _event, int _timelineValue)
    {
        // If the component is destroyed, recreates it if required or return
        if (isDestroyed)
        {
            if (_timelineValue < destructionFrame)
            {
                RecreateComponent(_timelineValue);
            }
            else return;
        }
        // If the component is no longer destroyed but was :
        else if (wasDestroyed)
        {
            // If the component needs to be re-destroyed on rewind, destroys it
            if (_timelineValue >= destructionFrame)
            {
                UnityEngine.Object.Destroy(component);
                isDestroyed = true;
                return;
            }
            // Else if starting recording, the component was no longer destroyed in this timeline, so update the boolean
            else if (_event == OnRecordStarts)
            {
                wasDestroyed = false;
            }
        }

        // Calls the associated event
        _event?.Invoke(_timelineValue);
    }

    // Recreates the component if it has been destroyed and needs to be back
    private void RecreateComponent(int _timelineValue)
    {
        // Creates the component and adds it to the GameObject
        component = source.AddComponent(type);

        // Set the component default / last values
        foreach (FieldInfoRewind _field in fieldsRewind)
        {
            if (_field.IsRewinded)
            {
                _field.FieldInfo.SetValue(component, _field.PastValues[_timelineValue]);
            }
            else
            {
                _field.FieldInfo.SetValue(component, _field.PastValues[0]);
            }
        }
        foreach (PropertyInfoRewind _property in propertiesRewind)
        {
            if (_property.IsRewinded)
            {
                _property.PropertyInfo.SetValue(component, _property.PastValues[_timelineValue]);
            }
            else
            {
                _property.PropertyInfo.SetValue(component, _property.PastValues[0]);
            }
        }

        // Re set the monoBehaviour field if needed
        if (isMonoBehaviour) monoBehaviour = (MonoBehaviour)component;

        // Calls the event of the start of the rewind
        OnRewindStarts?.Invoke(_timelineValue);

        // Update the destruction boolean(s)
        if (TimeMasterManager.Instance.DoAllowsReplay)
        {
            isDestroyed = false;
        }
        else
        {
            WasDestroyed = false;
        }
    }
    #endregion
}

/* MemberType :
 * 
 * The different types of member that can be rewinded in the MemberRewind class
*/
public enum MemberType
{
    Class,
    Dictionary,
    List,
    Value
}

[Serializable]
public abstract class MemberRewind
{
    /* MemberRewind :
     * 
    */

    #region Fields / Accessors
    // Indicates if this member is used in rewind or not
    public bool IsRewinded = true;

    // The past values of this member
    public List<object> PastValues = new List<object>();

    // The type of this member
    [SerializeField] protected MemberType memberType = MemberType.Value;
    public MemberType MemberType { get { return memberType; } }

    // The name of this member
    [SerializeField] protected string name = string.Empty;
    public string Name
    {
        get { return name; }
    }
    #endregion

    #region Methods

    #endregion
}

[Serializable]
public class FieldInfoRewind : MemberRewind
{
    /* FieldInfoRewind :
     * 
     * Contains a field info and its past values
    */

    #region Fields / Accessors
    // Field Info, what this is
    [SerializeField] private FieldInfo fieldInfo = null;
    public FieldInfo FieldInfo
    {
        get { return fieldInfo; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Construct a new FieldInfoRewind object based on a given field info
    /// </summary>
    /// <param name="_fieldInfo">Field info to use with this object</param>
    public FieldInfoRewind(FieldInfo _fieldInfo)
    {
        fieldInfo = _fieldInfo;
        name = fieldInfo.Name;
    }
    #endregion

    #region Methods
    // Initializes some non-serializable fields of the object
    public bool InitializeField(Type _componentType)
    {
        // If the field info is already set, it's no utility to go any further ; return true
        if (fieldInfo != null) return true;

        // Else, get it and initializes its past values list
        fieldInfo = _componentType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        // If the field info could not be found, return false
        if (fieldInfo == null)
        {
            return false;
        }

        // Initializes the past values list and return true
        PastValues = new List<object>();
        return true;
    }
    #endregion
}

[Serializable]
public class PropertyInfoRewind : MemberRewind
{
    /* PropertyInfoRewind :
     * 
     * Contains a property info and its past values
    */

    #region Fields / Accessors
    // Property Info, what this is
    [SerializeField] private PropertyInfo propertyInfo = null;
    public PropertyInfo PropertyInfo
    {
        get { return propertyInfo; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Construct a new PropertyInfoRewind object based on a given property info
    /// </summary>
    /// <param name="_propertyInfo">Property info to use with this object</param>
    public PropertyInfoRewind(PropertyInfo _propertyInfo)
    {
        propertyInfo = _propertyInfo;
        name = propertyInfo.Name;
    }
    #endregion
    
    #region Methods
    // Initializes some non-serializable fields of the object
    public bool InitializeProperty(Type _componentType)
    {
        // If the property info is already set, it's no utility to go any further ; return true
        if (propertyInfo != null) return true;

        // Else, get it and initializes its past values list
        propertyInfo = _componentType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        // If the property info could not be found, return false
        if (propertyInfo == null)
        {
            return false;
        }

        // Initializes the past values list and return true
        PastValues = new List<object>();
        return true;
    }
    #endregion
}
