using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using MixedReality.Devices.AtlasButton;
#endif

public class InputSDKContorller : MonoBehaviour
{

    private HudsonInput hudsonInput = null;

    private void Awake()
    {

        try
        {
            hudsonInput = new HudsonInput();
            SubscribeToHudsonButtonEvents();
        }
        catch (Exception exception)
        {
            Debug.Log($"Error setting up Hudson Input: {exception.Message}");
            UIManager.HudsonInputError($"Error setting up Hudson Input: {exception.Message}");
        }

    }

    /// <summary>
    /// Assigns an event handler for both the pressed
    /// and released state of the button. Similarly for
    /// the dials assigns an event handler that takes
    /// it's current dial position.
    /// </summary>
    private void SubscribeToHudsonButtonEvents()
    {
#if ENABLE_WINMD_SUPPORT
        /*BEGIN PuckDPad*/
        hudsonInput.PuckDPadDown.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckDPadDown.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckDPadUp.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckDPadUp.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckDPadLeft.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckDPadLeft.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckDPadRight.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckDPadRight.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };
        /*END PuckDPad*/

        /*BEGIN ZOOM*/
        hudsonInput.PuckRockerDown.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckRockerDown.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckRockerUp.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckRockerUp.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };
        /*END ZOOM*/

        /*BEGIN CONTROL BUTTONS*/
        hudsonInput.PuckTopRocker.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckTopRocker.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckCenter.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckCenter.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckTopRight.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckTopRight.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckBottomRight.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckBottomRight.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckTopLeft.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckTopLeft.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.PuckSideButton.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.PuckSideButton.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.HudButtonLeft.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.HudButtonLeft.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };

        hudsonInput.HudButtonRight.ButtonPressed += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: true);
        };
        hudsonInput.HudButtonRight.ButtonReleased += (btn) =>
        {
            hudsonInput.PuckButtonEventHandler(btn.ButtonType, isPressed: false);
        };
        /*END CONTROL BUTTONS*/

        /*BEGIN DIALS*/
        hudsonInput.HudDialLeft.RotationChanged += (dial, args) =>
        {
            hudsonInput.DialEventHandler(dial.DialType, args.RotaryPosition);
        };
        hudsonInput.HudDialRight.RotationChanged += (dial, args) =>
        {
            hudsonInput.DialEventHandler(dial.DialType, args.RotaryPosition);
        };
        /*END DIALS*/
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }




}
