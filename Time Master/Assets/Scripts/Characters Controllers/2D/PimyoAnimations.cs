using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PimyoAnimations : MonoBehaviour
{
    /* PimyoAnimations :
	*
	* [Write the Description of the Script here !]
	*/

    #region Fields / Accessors
    // The Pimyo Controller attached to the animator script

    [Header("Pimyo Controller :")]

    [SerializeField] private PimyoController pimyoController = null;

    // The Animator of the character

    [Header("Animator :")]

    [SerializeField] private Animator pimyoAnimator = null;
    #endregion

    #region Methods
    #region Unity Methods
    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Components check

        if (!pimyoController) pimyoController = GetComponent<PimyoController>();
        if (!pimyoAnimator) pimyoAnimator = GetComponent<Animator>();

        if (!pimyoController || !pimyoAnimator)
        {
            Debug.Log("Pimyo Animator script reference(s) missing !");
            Destroy(this);
            return;
        }

        // Events set

        pimyoController.OnAir += SetAirAnimation;
        pimyoController.OnMovement += SetMovementAnimation;
    }
    #endregion

    #region Original Methods
    // Set the animation depending on the velocity of the character
    private void SetAirAnimation(float _velocity)
    {
        pimyoAnimator.SetFloat("Velocity", _velocity);
    }

    // Set the animation depending on the speed of the character
    private void SetMovementAnimation(float _speed)
    {
        pimyoAnimator.SetFloat("Speed", _speed);
    }
    #endregion
    #endregion
}
