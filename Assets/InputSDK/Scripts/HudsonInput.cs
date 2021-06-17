using System;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using MixedReality.Devices.AtlasButton;
#endif


public class HudsonInput
{
#if ENABLE_WINMD_SUPPORT
    public static readonly int HudsonNumButtons = Enum.GetNames(typeof(AtlasButtonType)).Length;
    public static readonly int DialPositions = 7;

    private AtlasButtonController atlasButtonController;

    public AtlasButton PuckDPadDown;
    public AtlasButton PuckDPadUp;
    public AtlasButton PuckDPadLeft;
    public AtlasButton PuckDPadRight;

    public AtlasButton PuckRockerUp;
    public AtlasButton PuckRockerDown;

    public AtlasButton PuckTopRocker;

    public AtlasButton PuckTopRight;
    public AtlasButton PuckTopLeft;
    public AtlasButton PuckCenter;
    public AtlasButton PuckBottomRight;
    public AtlasButton PuckSideButton;

    public AtlasButton HudButtonLeft;
    public AtlasButton HudButtonRight;

    public AtlasDial HudDialLeft;
    public AtlasDial HudDialRight;

    public bool[] ButtonStates;
    public uint LeftDialState;
    public uint RightDialState;
#endif
    private bool isSetUp = false;
    public bool IsSetUp => isSetUp;

    public HudsonInput()
    {
        try
        {
            InitializeHudsonInput();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"Unable to initialize the AtlasButtonController. {exception.Message}");
            isSetUp = false;
        }
    }

    /// <summary>
    /// Assigns and maps the respective button to the variable
    /// </summary>
    private void InitializeHudsonInput()
    {
#if ENABLE_WINMD_SUPPORT
        atlasButtonController = new AtlasButtonController();

        PuckDPadDown = atlasButtonController.GetButton(AtlasButtonType.Button1);
        PuckDPadUp = atlasButtonController.GetButton(AtlasButtonType.Button2);
        PuckDPadLeft = atlasButtonController.GetButton(AtlasButtonType.Button3);
        PuckDPadRight = atlasButtonController.GetButton(AtlasButtonType.Button4);

        PuckRockerUp = atlasButtonController.GetButton(AtlasButtonType.Button5);
        PuckRockerDown = atlasButtonController.GetButton(AtlasButtonType.Button6);

        PuckTopRocker = atlasButtonController.GetButton(AtlasButtonType.Button7);

        PuckTopRight = atlasButtonController.GetButton(AtlasButtonType.Button8);
        PuckTopLeft = atlasButtonController.GetButton(AtlasButtonType.Button9);
        PuckCenter = atlasButtonController.GetButton(AtlasButtonType.Button10);
        PuckBottomRight = atlasButtonController.GetButton(AtlasButtonType.Button11);
        PuckSideButton = atlasButtonController.GetButton(AtlasButtonType.Button12);

        HudButtonLeft = atlasButtonController.GetButton(AtlasButtonType.HudButton1);
        HudButtonRight = atlasButtonController.GetButton(AtlasButtonType.HudButton2);

        HudDialLeft = atlasButtonController.GetDial(AtlasDialType.Dial1);
        LeftDialState = HudDialLeft.GetRotaryPosition();
        HudDialRight = atlasButtonController.GetDial(AtlasDialType.Dial2);
        RightDialState = HudDialRight.GetRotaryPosition();

        ButtonStates = new bool[HudsonNumButtons];
        ButtonStates[(int)PuckTopRocker.ButtonType] = PuckTopRocker.IsPressed();
#endif
        isSetUp = true;
    }

#if ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Simply sets the bool to true if pressed and false if otherwise
    /// </summary>
    /// <param name="buttonType">The button being pressed/released.</param>
    /// <param name="isPressed"></param>
    public void PuckButtonEventHandler(in AtlasButtonType buttonType, in bool isPressed)
    {
        // Not related to Unity thread so we shouldn't
        // need to throw it on the unity main thread
        switch(buttonType)
        {

            case AtlasButtonType.Button1: //DPad Down
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button2: //DPad Up
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button3: //DPad Left
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button4: //DPad Down
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button5: //Rocker Up
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button6: //Rocker Down
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button7: //Top Rocker
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button8: //Top Right
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button9: //Top Left
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button10: //Center Button
            {
                if(isPressed) UIManager.PuckCenterPressed();
                else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button11: //Bottom Right
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.Button12: //Side Button
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.HudButton1: //HUD Button Left
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }
            case AtlasButtonType.HudButton2: //HUD Button Right
            {
                // if(isPressed) UIManager.PuckCenterPressed();
                // else UIManager.PuckCenterReleased();
                break;
            }

            default: break;

        }

        
    }

    /// <summary>
    /// Sets the dial state to the current dial position
    /// </summary>
    /// <param name="dialType">Whether this is the right or left dial. Left = Dial1, Right = Dial2</param>
    /// <param name="dialPosition"></param>
    public void DialEventHandler(in AtlasDialType dialType, in uint dialPosition)
    {
        switch (dialType)
        {
            case AtlasDialType.Dial1:
                LeftDialState = dialPosition;
                break;
            case AtlasDialType.Dial2:
                RightDialState = dialPosition;
                break;
        }
    }
#endif
}
