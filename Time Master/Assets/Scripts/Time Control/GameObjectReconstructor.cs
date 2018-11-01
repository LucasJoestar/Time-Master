using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class GameObjectReconstructor
{
    /* GameObjectReconstructor :
	*
	* This object has in purpose to save all the characteristics of a GameObject and its children to recreate them from scratch as a copy
    * 
    * It should be created & stored when a copy of a GameObject at this particular state is desired
	*/

    #region Events
    /* Recreation Event :
     * 
     * Called when the object is recreated
     * GameObject parameter gives the recreated GameObject
    */
    public event Action<GameObject> OnRecreated = null;
    #endregion

    #region Fields / Accessors
    // The ComponentReconstructor objects for each component of the GameObject
    [SerializeField] private List<ComponentReconstructor> components = new List<ComponentReconstructor>();
    public List<ComponentReconstructor> Components
    {
        get { return components; }
    }

    // The GameObjectReconstructor objects for the children of this GameObject (At stage #1)
    [SerializeField] private List<GameObjectReconstructor> children = new List<GameObjectReconstructor>();
    public List<GameObjectReconstructor> Children
    {
        get { return children; }
    }

    // The name of this GameObject
    [SerializeField] private string name = string.Empty;
    public string Name
    {
        get { return name; }
    }

    // The tag of this GameObject
    [SerializeField] private string tag = string.Empty;
    public string Tag
    {
        get { return tag; }
    }

    // The parent transform of this GameObject
    [SerializeField] private Transform parent = null;
    public Transform Parent
    {
        get { return parent; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a reconstructor object of a specified existing GameObject
    /// </summary>
    /// <param name="_gameObject">Original GameObject to recreate</param>
    public GameObjectReconstructor(GameObject _gameObject)
    {
        // Get the name, the tag and the parent of this GameObject
        name = _gameObject.name;
        tag = _gameObject.tag;
        parent = _gameObject.transform.parent;

        // Creates a ComponentReconstructor object foreach component of this GameObject and add them to the list
        _gameObject.GetComponents<Component>().ToList().ForEach(c => components.Add(new ComponentReconstructor(c)));

        // Get the number of children of this GameObject
        Transform _transform = _gameObject.transform;

        // Foreach children, creates a GameObjectReconstructor and adds it to the list
        for (int _i = 0; _i < _transform.childCount; _i++)
        {
            children.Add(new GameObjectReconstructor(_gameObject.transform.GetChild(_i).gameObject));
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Recreates the original GameObject and returns it
    /// </summary>
    /// <returns>The recreated GameObject</returns>
    public GameObject Recreate()
    {
        // Creates a new GameObject named as the original
        GameObject _recreation = new GameObject(name, components.Select(c => c.Type).ToArray());

        // Set the tag of this GameObject
        _recreation.tag = tag;

        // Set the parent of this GameObject
        _recreation.transform.SetParent(parent);

        // Recreates each component of the original GameObject on this
        components.ForEach(c => c.Recreate(_recreation, true));

        // Recreates each child of the original GameObject on this
        children.ForEach(c => c.Recreate());

        // Returns the recreated object
        return _recreation;
    }
    #endregion
}

[Serializable]
public class ComponentReconstructor
{
    /* ComponentReconstructor :
	*
	* This object has in purpose to save all the characteristics of a Component to recreate it from scratch as a copy
    * 
    * It should be created & stored when a copy of a Component at this particular state is desired
	*/

    #region Events
    /* Recreation Event :
     * 
     * Called when the component is recreated
     * Component parameter gives the recreated Component
    */
    public event Action<Component> OnRecreated = null;
    #endregion

    #region Fields / Accessors
    // The type of this component
    [SerializeField] private Type type = null;
    public Type Type
    {
        get { return type; }
    }

    // The fields name of the component and their value
    [SerializeField] private Dictionary<string, object> fields = new Dictionary<string, object>();
    public Dictionary<string, object> Fields
    {
        get { return fields; }
    }

    // The fields containing in scene references
    [SerializeField] private List<FieldPropertyReferenceReconstructor> fieldsReference = new List<FieldPropertyReferenceReconstructor>();
    public List<FieldPropertyReferenceReconstructor> FieldsReference
    {
        get { return fieldsReference; }
    }

    // The properties name of component and their value
    [SerializeField] private Dictionary<string, object> properties = new Dictionary<string, object>();
    public Dictionary<string, object> Properties
    {
        get { return properties; }
    }

    // The properties containing in scene references
    [SerializeField] List<FieldPropertyReferenceReconstructor> propertiesReference = new List<FieldPropertyReferenceReconstructor>();
    public List<FieldPropertyReferenceReconstructor> PropertiesReference
    {
        get { return propertiesReference; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a reconstructor object of a specified existing Component
    /// </summary>
    /// <param name="_component">Original Component to recreate</param>
    public ComponentReconstructor(Component _component)
    {
        // Get the type of the component
        type = _component.GetType();

        // Store each field & property of the component and their value
        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ToList().ForEach(f => fields.Add(f.Name, f.GetValue(_component)));
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(p => p.CanRead && p.CanWrite && !p.IsDefined(typeof(ObsoleteAttribute))).ToList().ForEach(p => properties.Add(p.Name, p.GetValue(_component)));
    }
    #endregion

    #region Methods
    /// <summary>
    /// Recreates the original Component and returns it
    /// </summary>
    /// <param name="_gameObject">GameObject where to add the recreated Component</param>
    /// <returns>The recreated Component</returns>
    public Component Recreate(GameObject _gameObject, bool _isAlreadyOnGameObject)
    {
        // Creates a new component of the right type and adds it the the GameObject
        Component _recreation = _isAlreadyOnGameObject ? _gameObject.GetComponent(type) : _gameObject.AddComponent(type);

        // Set the value of each field & property of the component
        foreach (KeyValuePair<string, object> entry in fields)
        {
            type.GetField(entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(_recreation, entry.Value);
        }
        foreach (KeyValuePair<string, object> entry in properties)
        {
            type.GetProperty(entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(_recreation, entry.Value);
        }

        // Calls the OnRecreated event
        OnRecreated?.Invoke(_recreation);

        // Returns the recreated component
        return _recreation;
    }
    #endregion
}

[Serializable]
public class FieldPropertyReferenceReconstructor
{
    /* FieldPropertyReferenceReconstructor :
     * 
    */

    #region Fields / Accessors
    // The name of this field / property
    [SerializeField] private string name = string.Empty;
    public string Name
    {
        get { return name; }
    }

    // The name of the GameObject of the reference
    [SerializeField] private string gameObjectReferenceName = string.Empty;
    public string GameObjectReferenceName
    {
        get { return gameObjectReferenceName; }
    }

    // The type of the Component of the reference
    [SerializeField] private Type componentReferenceType = null;
    public Type ComponentReferenceType
    {
        get { return componentReferenceType; }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new FieldPropertyReferenceConstructor with a GameObject reference
    /// </summary>
    /// <param name="_name">Name of the field / property</param>
    /// <param name="_gameObjectReferenceName">Name of the GameObject reference</param>
    public FieldPropertyReferenceReconstructor(string _name, string _gameObjectReferenceName)
    {
        name = _name;
        gameObjectReferenceName = _gameObjectReferenceName;
    }
    /// <summary>
    /// Creates a new FieldPropertyReferenceConstructor with a Component reference
    /// </summary>
    /// <param name="_name">Name of the field / property</param>
    /// <param name="_gameObjectReferenceName">Name of the GameObject source of the Component reference</param>
    /// <param name="_componentType">Type of the Component reference</param>
    public FieldPropertyReferenceReconstructor(string _name, string _gameObjectReferenceName, Type _componentReferenceType)
    {
        name = _name;
        gameObjectReferenceName = _gameObjectReferenceName;
        componentReferenceType = _componentReferenceType;
    }
    #endregion
}
