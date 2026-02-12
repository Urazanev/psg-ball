using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputAdapter
{
    static bool initialized;
    static InputAction leftFlipper;
    static InputAction rightFlipper;
    static InputAction plunger;
    static InputAction pause;
    static InputAction nudgeLeft;
    static InputAction nudgeRight;
    static InputAction useItem;

    static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        InputActionAsset controls = Resources.Load<InputActionAsset>("PSG1Controls");
        if (controls == null)
        {
            Debug.LogWarning("InputAdapter: PSG1Controls.inputactions was not found in Assets/Resources. Falling back to keyboard only.");
            return;
        }

        InputActionMap gameplay = controls.FindActionMap("Gameplay", false);
        if (gameplay == null)
        {
            Debug.LogWarning("InputAdapter: Gameplay action map was not found in PSG1Controls.");
            return;
        }

        leftFlipper = gameplay.FindAction("LeftFlipper", false);
        rightFlipper = gameplay.FindAction("RightFlipper", false);
        plunger = gameplay.FindAction("Plunger", false);
        pause = gameplay.FindAction("Pause", false);
        nudgeLeft = gameplay.FindAction("NudgeLeft", false);
        nudgeRight = gameplay.FindAction("NudgeRight", false);
        useItem = gameplay.FindAction("UseItem", false);

        gameplay.Enable();
    }

    static bool IsPsg1Connected()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (ContainsPsg1(gamepad.displayName) ||
                ContainsPsg1(gamepad.name) ||
                ContainsPsg1(gamepad.description.product))
                return true;
        }

        return false;
    }

    static bool ContainsPsg1(string value) =>
        !string.IsNullOrEmpty(value) && value.IndexOf("PSG1", StringComparison.OrdinalIgnoreCase) >= 0;

    static bool ReadPressedThisFrame(InputAction action, Func<bool> keyboardFallback)
    {
        EnsureInitialized();
        bool actionValue = action != null && action.WasPressedThisFrame();

        if (IsPsg1Connected())
            return action != null && actionValue;

        return actionValue || keyboardFallback();
    }

    static bool ReadIsPressed(InputAction action, Func<bool> keyboardFallback)
    {
        EnsureInitialized();
        bool actionValue = action != null && action.IsPressed();

        if (IsPsg1Connected())
            return action != null && actionValue;

        return actionValue || keyboardFallback();
    }

    static bool ReadReleasedThisFrame(InputAction action, Func<bool> keyboardFallback)
    {
        EnsureInitialized();
        bool actionValue = action != null && action.WasReleasedThisFrame();

        if (IsPsg1Connected())
            return action != null && actionValue;

        return actionValue || keyboardFallback();
    }

    public static bool PlungerPressedThisFrame() =>
        ReadPressedThisFrame(plunger, () => Input.GetKeyDown(KeyCode.Space));

    public static bool PlungerHeld() =>
        ReadIsPressed(plunger, () => Input.GetKey(KeyCode.Space));

    public static bool PlungerReleasedThisFrame() =>
        ReadReleasedThisFrame(plunger, () => Input.GetKeyUp(KeyCode.Space));

    public static bool RightFlipperPressedThisFrame() =>
        ReadPressedThisFrame(rightFlipper, () => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow));

    public static bool RightFlipperHeld() =>
        ReadIsPressed(rightFlipper, () => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));

    public static bool RightFlipperReleasedThisFrame() =>
        ReadReleasedThisFrame(rightFlipper, () => Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow));

    public static bool LeftFlipperPressedThisFrame() =>
        ReadPressedThisFrame(leftFlipper, () => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow));

    public static bool LeftFlipperHeld() =>
        ReadIsPressed(leftFlipper, () => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow));

    public static bool LeftFlipperReleasedThisFrame() =>
        ReadReleasedThisFrame(leftFlipper, () => Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow));

    public static bool PausePressedThisFrame() =>
        ReadPressedThisFrame(pause, () => Input.GetKeyDown(KeyCode.Escape));

    public static bool NudgeLeftPressedThisFrame() =>
        ReadPressedThisFrame(nudgeLeft, () => Input.GetKeyDown(KeyCode.LeftShift));

    public static bool NudgeRightPressedThisFrame() =>
        ReadPressedThisFrame(nudgeRight, () => Input.GetKeyDown(KeyCode.RightShift));

    public static bool UseItemPressedThisFrame() =>
        ReadPressedThisFrame(useItem, () => Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftApple));
}
