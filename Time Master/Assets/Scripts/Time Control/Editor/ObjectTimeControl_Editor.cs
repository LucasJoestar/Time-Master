using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(TimeControlObject))]
[CanEditMultipleObjects]
public class ObjectTimeControl_Editor : Editor
{
    /* ObjectTimeControl_Editor :
	*
	* Editor script for the ObjectTimeControl class
    * 
    * This editor should allow to the user an easy and precise utilisation & customization
    * of the ObjectTimeControl class, so on every object possessing this script
    * 
    * With this editor, the user can :
    *       - Choose which component(s) of the object to rewind
    *       - Select precisely for each of them the fields & properties to rewind
	*/

    #region Fields / Accessors
    /* ObjectTimeControl Fields & Properties
     * 
     * The fields & properties of the currently editing script
     * 
     * "New" version of the editor, instead of modying directly the script variables
     * 
     * Using the SerializedObject & SerializedProperty system to edit the scripts,
     * this automatically handles multi-object editing, undo, and prefab overrides !
     * 
     * With the CanEditMultipleObjects attribute, this approach allows the user to
     * select multiple assets in the hierarchy window, and change the values for all of them at once
    */

    #region Time
    private SerializedProperty timeState = null;
    #endregion

    #region Pause
    private SerializedProperty pauseComponents = null;
    #endregion

    #region Rewind
    private SerializedProperty animator = null;
    private SerializedProperty componentsRewind = null;
    private SerializedProperty doRewindAnimator = null;
    private SerializedProperty timelineLength = null;
    #endregion

    #region Editor Fields
    /* Editor Fields
     * 
     * The fields used only in the editor and not in the original scripts
     * 
     * Constantly used, these fields have no reason to be temporary
    */
    private Scene openScene = new Scene();
    private Component newRewindingComponent = null;

    #region Foldouts
    // Foldouts of the editor's sections
    private bool foldPause = false;
    private bool foldRewind = false;
    private bool foldTimeStatus = true;
    #endregion
    #endregion
    #endregion

    #region Methods
    #region Unity Methods
    // This function is called when the object is loaded
    private void OnEnable()
    {
        // Get the properties of the SerializedObject currently analysing
        timeState = serializedObject.FindProperty("timeState");

        pauseComponents = serializedObject.FindProperty("pauseComponents");

        animator = serializedObject.FindProperty("animator");
        componentsRewind = serializedObject.FindProperty("componentsRewind");
        doRewindAnimator = serializedObject.FindProperty("doRewindAnimator");
        timelineLength = serializedObject.FindProperty("timelineLength");

        // Get the active scene to save it when needed
        openScene = EditorSceneManager.GetActiveScene();

        // Initializes the fields & properties of the objects
        foreach (TimeControlObject _object in serializedObject.targetObjects)
        {
            _object.InitializesLostValues();
        }
    }

    // Implement this function to make a custom inspector
    public override void OnInspectorGUI()
    {
        #region Save Button
        // Button to save changes made in the editor
        GUI.color = new Color(0, .75f, 0, 1);

        if (GUILayout.Button("SAVE"))
        {
            SaveScene();
        }

        GUI.color = Color.white;
        #endregion

        EditorGUILayout.Space();

        #region Add new rewinding component
        // Presentation of the section
        EditorGUILayout.HelpBox("Add a new Component to Rewind", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();

        // Creates an object field where to drag or select a new component of this object to rewind
        newRewindingComponent = EditorGUILayout.ObjectField(null, typeof(Component), true) as Component;

        EditorGUILayout.EndHorizontal();

        /* If a new rewinding component has been assigned,
         * adds it if it belongs to the same GameObject as the editing script
         * and if it isn't already in the rewinding components list
        */
        if (newRewindingComponent != null)
        {
            // Get all editing scripts, and for each check if its GameObject has the added component on it
            Object[] _editingObjects = serializedObject.targetObjects;
            foreach (TimeControlObject _object in _editingObjects)
            {
                Component _componentRewind = _object.gameObject.GetComponent(newRewindingComponent.GetType()) as Component;
                if (_componentRewind)
                {
                    // If the GameObject has the component on it and this one isn't on the list of the rewinding components, adds it
                    if (!_object.ComponentsRewind.Any(c => c.Component == _componentRewind))
                    {
                        _object.ComponentsRewind.Add(new ComponentRewind(_componentRewind));
                    }
                }
            }

            // Nullify back the variable to let the user add a new rewinding component
            newRewindingComponent = null;
        }
        #endregion

        EditorGUILayout.Space();

        #region Color Settings

        #endregion

        EditorGUILayout.Space();

        #region Time Status
        // Foldout of the section

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Time Status", EditorStyles.boldLabel)) foldTimeStatus = !foldTimeStatus;

        EditorGUILayout.EndHorizontal();

        if (foldTimeStatus)
        {
            // Show the current time state of the object
            EditorGUILayout.TextField("Time State :", ((TimeState)(timeState.enumValueIndex)).ToString());
        }
        #endregion

        EditorGUILayout.Space();

        #region Pause
        // Foldout of the section
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Pause", EditorStyles.boldLabel)) foldPause = !foldPause;

        EditorGUILayout.EndHorizontal();

        if (foldPause)
        {
            // Show the list of components to configure on pause mode
            EditorGUILayout.PropertyField(pauseComponents, true);
        }
        #endregion

        EditorGUILayout.Space();

        #region Rewind
        // Foldout of the section
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Rewind", EditorStyles.boldLabel)) foldRewind = !foldRewind;

        EditorGUILayout.EndHorizontal();

        if (foldRewind)
        {
            // Are the animations of the GameObject rewinded or not
            doRewindAnimator.boolValue = EditorGUILayout.Toggle("Rewind Animator :", doRewindAnimator.boolValue);

            // If so, indicates the animator to use
            if (doRewindAnimator.boolValue)
            {
                animator.objectReferenceValue = EditorGUILayout.ObjectField("Animator :", animator.objectReferenceValue, typeof(Animator), true);
            }

            EditorGUILayout.Space();

            // The amount of recorded states
            EditorGUILayout.IntField("Timeline Length :", timelineLength.intValue);

            EditorGUILayout.Space();

            // Draw and show the rewinding components
            if (!serializedObject.isEditingMultipleObjects)
            {
                // Presentation of the section
                EditorGUILayout.LabelField("Rewinding Components :", EditorStyles.boldLabel);

                // Size of the list
                EditorGUILayout.LabelField("Size :", componentsRewind.arraySize.ToString());

                // Draw the editor of each component
                for (int _i = 0; _i < componentsRewind.arraySize; _i++)
                {
                    EditorGUILayout.PropertyField(componentsRewind.GetArrayElementAtIndex(_i), true);
                }
            }
            else EditorGUILayout.HelpBox("Warning ! You can not edit settings of components when editing multiple objects", MessageType.Warning);
        }
        #endregion

        // Apply the modifications one the editing object(s)
        serializedObject.ApplyModifiedProperties();

        // Save the scene if changes has been made
        // ???
    }
    #endregion

    #region Original Methods
    // Save the actually opened scene
    private void SaveScene()
    {
        EditorSceneManager.MarkSceneDirty(openScene);
        EditorSceneManager.SaveScene(openScene);

        Debug.Log("SAVE");
    }
    #endregion
    #endregion
}

[CustomPropertyDrawer(typeof(ComponentRewind))]
public class ComponentRewind_Editor : PropertyDrawer
{
    /* ComponentRewind_Editor :
     * 
     * Editor script for the ComponentRewind class
     * 
     * This Editor should allow to the user a precise customization
     * of the ComponentRewind class
     * 
     * Because the ComponentRewind class does not inherit from MonoBehaviour and is serializable,
     * this editor inherits from PropertyDrawer and override the OnGUI method,
     * so it's a bit different
     * 
     * With PropertyDrawer, the EditorGUILayout methods are not usable (Performance reasons)
    */

    #region Fields / Accesors

    #endregion

    #region Methods
    // Override this method to specify how tall the GUI for this field is in pixels
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * (10 + property.FindPropertyRelative("fieldsRewind").arraySize + property.FindPropertyRelative("propertiesRewind").arraySize);
    }

    // Override this method to make your own GUI for the property
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin a Property wrapper for much better working of this editor
        EditorGUI.BeginProperty(position, label, property);

        // Get variables of the object
        string _name = property.FindPropertyRelative("name").stringValue;
        Object _component = property.FindPropertyRelative("component").objectReferenceValue;
        SerializedProperty _fieldsRewind = property.FindPropertyRelative("fieldsRewind");
        SerializedProperty _propertiesRewind = property.FindPropertyRelative("propertiesRewind");

        // Get Rects of the editor
        Rect _removeRect = new Rect(position.x, position.y, 30, 20);
        Rect _nameRect = new Rect(position.x + 40, position.y, position.width, 20);
        Rect _componentRect = new Rect(position.x, position.y + 25, position.width, 15);

        Rect _fieldsPresRect = new Rect(position.x, position.y + 50, position.width, 50);

        Rect _propertiesPresRect = new Rect(position.x, position.y + 70 + (17.5f * _fieldsRewind.arraySize), position.width, 15);

        // CUSTOM EDITOR

        // Remove button
        GUI.color = new Color(.9f, .25f, .25f, 1);
        if (GUI.Button(_removeRect, "X"))
        {
            GUI.color = Color.white;
            property.DeleteCommand();
            return;
        }
        GUI.color = Color.white;

        // Title Presentation
        EditorGUI.HelpBox(_nameRect, _name, MessageType.None);

        // The component source
        if (_component == null)
        {
            property.DeleteCommand();
            return;
        }
        EditorGUI.ObjectField(_componentRect, "Component :", _component, _component.GetType(), true);

        // The fields of the component
        EditorGUI.LabelField(_fieldsPresRect, "Fields Rewind", EditorStyles.boldLabel);
        for (int _i = 0; _i < _fieldsRewind.arraySize; _i++)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y + 70 + (17.5f * _i), position.width, 17.5f), _fieldsRewind.GetArrayElementAtIndex(_i), true);
        }

        // The properties of the component
        EditorGUI.LabelField(_propertiesPresRect, "Properties Rewind", EditorStyles.boldLabel);
        for (int _i = 0; _i < _propertiesRewind.arraySize; _i++)
        {
            EditorGUI.PropertyField(new Rect(position.x, position.y + 90 + (17.5f * _fieldsRewind.arraySize) + (17.5f * _i), position.width, 17.5f), _propertiesRewind.GetArrayElementAtIndex(_i), true);
        }

        // Ends the Property wrapper
        EditorGUI.EndProperty();
    }
    #endregion
}

[CustomPropertyDrawer(typeof(FieldInfoRewind))]
public class FieldInfoRewind_Editor : PropertyDrawer
{
    /* FieldInfoRewind_Editor :
     * 
    */

    #region Fields / Accessors

    #endregion

    #region Methods
    // Override this method to make your own GUI for the property
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin a Property wrapper for much better working of this editor
        EditorGUI.BeginProperty(position, label, property);

        // Get variables of the object
        SerializedProperty _isRewinded = property.FindPropertyRelative("IsRewinded");
        string _name = property.FindPropertyRelative("name").stringValue;

        // Get Rects of the editor
        Rect _toggleRect = new Rect(position.x + 50, position.y, position.width, position.height);

        // CUSTOM EDITOR
        _isRewinded.boolValue = EditorGUI.ToggleLeft(_toggleRect, _name, _isRewinded.boolValue);

        // Ends the Property wrapper
        EditorGUI.EndProperty();
    }
    #endregion
}

[CustomPropertyDrawer(typeof(PropertyInfoRewind))]
public class PropertyInfoRewind_Editor : PropertyDrawer
{
    /* PropertyInfoRewind_Editor
     * 
    */

    #region Fields / Accessors

    #endregion

    #region Methods
    // Override this method to make your own GUI for the property
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin a Property wrapper for much better working of this editor
        EditorGUI.BeginProperty(position, label, property);

        // Get variables of the object
        SerializedProperty _isRewinded = property.FindPropertyRelative("IsRewinded");
        string _name = property.FindPropertyRelative("name").stringValue;

        // Get Rects of the editor
        Rect _toggleRect = new Rect(position.x + 50, position.y, position.width, position.height);

        // CUSTOM EDITOR
        _isRewinded.boolValue = EditorGUI.ToggleLeft(_toggleRect, _name, _isRewinded.boolValue);

        // Ends the Property wrapper
        EditorGUI.EndProperty();
    }
    #endregion
}