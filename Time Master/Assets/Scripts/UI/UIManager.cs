using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    /* UIManager :
	*
	* Manages the UI of the game
    * 
    * This script should be attached to an empty game object in the scene
    * 
    * The UI is constituted of the following elements :
    *       - An Image indicating the current time state
    *       - A Rewind Gauge indicating the timeline the user can rewind
    *       - A Text indicating the current scrolling speed of the time
	*/

    #region Events

    #endregion

    #region Fields / Accessors
    #region Time State
    [Header("Time State :", order = 0)]

    [Header("Time State Image :", order = 1)]

    // The time state indicating image
    [SerializeField] Image timeStateImage = null;

    [Header("Time State Sprites :")]

    // The pause time state icon
    [SerializeField] Sprite pauseIcon = null;

    // The play time state icon
    [SerializeField] Sprite playIcon = null;

    // The rewind time state icon
    [SerializeField] Sprite rewindIcon = null;
    #endregion

    #region Rewind
    [Header("Rewind :")]

    // The image of the rewind gauge which fill amount indicates the timeline to rewind
    [SerializeField] Image rewindGauge = null;

    // The text indicating the rewind scaler
    [SerializeField] TextMeshProUGUI rewindScalerInfo = null;
    #endregion
    #endregion

    #region Methods
    #region Unity Methods
    // Awake is called when the script instance is being loaded
    void Awake()
    {

    }

    // Use this for initialization
    void Start ()
    {
        // Set events of the TimeMaster Manager
        if (TimeMasterManager.Instance == null) return;

        TimeMasterManager.Instance.OnRewindGetPercentage += SetRewindGauge;
        TimeMasterManager.Instance.OnGlobalTimeStateUpdate += SetTimeStateIcon;
        TimeMasterManager.Instance.OnRewindScalerUpdate += SetRewindScaler;
    }

    // Update is called once per frame
    void Update ()
    {
		
	}
    #endregion
	
    #region Original Methods
    // Set the filled amount of the rewind gauge to a given value
	private void SetRewindGauge(float _fillAmount)
    {
        rewindGauge.fillAmount = _fillAmount;
    }

    // Set the rewind scaler information of the HUD
    private void SetRewindScaler(float _rewindScaler)
    {
        rewindScalerInfo.text = "x " + _rewindScaler;
    }

    // Set the icon of the actual state
    private void SetTimeStateIcon(TimeState _timeState)
    {
        switch (_timeState)
        {
            case TimeState.Pause:
                // Pause icon
                timeStateImage.sprite = pauseIcon;
                break;

            case TimeState.Play:
                // Play icon
                timeStateImage.sprite = playIcon;
                break;

            case TimeState.Rewind:
                // Rewind icon
                timeStateImage.sprite = rewindIcon;
                break;

            default:
                // What's going on here is a complete mystery, yep, even to me
                break;
        }
    }
    #endregion
    #endregion
}
