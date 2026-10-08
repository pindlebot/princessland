using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Every button the game listens to, in one place, for keyboard + mouse and gamepad alike.
// Scripts ask GameInput.CastHeld instead of checking Space and the X button themselves.
//
//   Action          Keyboard / mouse          Gamepad
//   Move            WASD / arrow keys         left stick / d-pad
//   Talk, open      E                         A (south)
//   Cast            Space / left click        X (west) / right trigger
//   Inventory       I                         Y (north)
//   Skill tree      K                         View / Select
//   Help            H                         RB (right shoulder)
//   Pause menu      Esc                       Start
//   Back            Esc                       B (east)
//   Confirm         Enter                     A
//   Try again       R                         A, once the game is over (GameManager)
//
// Built on the Input System package's device API (Keyboard.current, Gamepad.current), which
// works for any controller the Input System knows (Xbox, PlayStation, Switch Pro, ...).
public static class GameInput
{
    private const float StickDeadZone = 0.2f;

    private static int checkedFrame = -1;
    private static bool usingGamepad;

    // Did the player last touch the gamepad (rather than the keyboard or mouse)?
    // The HUD uses this to say "A: Open chest" instead of "E: Open chest".
    public static bool UsingGamepad
    {
        get
        {
            if (Time.frameCount != checkedFrame)
            {
                checkedFrame = Time.frameCount;
                var pad = Gamepad.current;
                if (pad == null)
                    usingGamepad = false; // unplugged: back to keyboard prompts
                else if ((AnyButton(pad) || pad.leftStick.ReadValue().magnitude > 0.5f))
                    usingGamepad = true;
                else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                         (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                    usingGamepad = false;
            }
            return usingGamepad;
        }
    }

    public static string InteractKey => UsingGamepad ? "A" : "E";
    public static string HelpKey => UsingGamepad ? "RB" : "H";

    // ---------- Movement ----------

    public static Vector2 Move
    {
        get
        {
            Vector2 move = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.magnitude > StickDeadZone) move += stick;
                move += pad.dpad.ReadValue();
            }
            return Vector2.ClampMagnitude(move, 1f); // diagonals aren't faster
        }
    }

    // ---------- Actions ----------

    public static bool InteractPressed => Down(Keyboard.current?.eKey) || Down(Gamepad.current?.buttonSouth);

    public static bool CastHeld =>
        Held(Keyboard.current?.spaceKey) || Held(Gamepad.current?.buttonWest) || Held(Gamepad.current?.rightTrigger);

    public static bool CastPressed =>
        Down(Keyboard.current?.spaceKey) || Down(Gamepad.current?.buttonWest) || Down(Gamepad.current?.rightTrigger);

    public static bool InventoryPressed => Down(Keyboard.current?.iKey) || Down(Gamepad.current?.buttonNorth);
    public static bool SkillTreePressed => Down(Keyboard.current?.kKey) || Down(Gamepad.current?.selectButton);
    public static bool HelpPressed => Down(Keyboard.current?.hKey) || Down(Gamepad.current?.rightShoulder);
    public static bool MenuPressed => Down(Keyboard.current?.escapeKey) || Down(Gamepad.current?.startButton);
    public static bool BackPressed => Down(Keyboard.current?.escapeKey) || Down(Gamepad.current?.buttonEast);
    public static bool RestartPressed => Down(Keyboard.current?.rKey);
    public static bool MutePressed => Down(Keyboard.current?.mKey);

    public static bool ConfirmPressed =>
        Down(Keyboard.current?.enterKey) || Down(Keyboard.current?.numpadEnterKey) || Down(Gamepad.current?.buttonSouth);

    // Next line of a conversation: any of the "yes / go on" buttons, or a click.
    public static bool AdvancePressed =>
        Down(Keyboard.current?.eKey) || Down(Keyboard.current?.spaceKey) || ConfirmPressed || ClickPressed;

    // Gameplay buttons are ignored while a conversation or the pause menu is open.
    public static bool GameplayBlocked => DialogueController.BlocksInput || PauseMenu.IsOpen;

    // Menu navigation: arrow keys, d-pad, or a flick of the stick.
    public static bool UpPressed =>
        Down(Keyboard.current?.upArrowKey) || Down(Gamepad.current?.dpad.up) || Down(Gamepad.current?.leftStick.up);
    public static bool DownPressed =>
        Down(Keyboard.current?.downArrowKey) || Down(Gamepad.current?.dpad.down) || Down(Gamepad.current?.leftStick.down);
    public static bool LeftPressed =>
        Down(Keyboard.current?.leftArrowKey) || Down(Gamepad.current?.dpad.left) || Down(Gamepad.current?.leftStick.left);
    public static bool RightPressed =>
        Down(Keyboard.current?.rightArrowKey) || Down(Gamepad.current?.dpad.right) || Down(Gamepad.current?.leftStick.right);

    // The number row: NumberPressed(1) is the "1" key.
    public static bool NumberPressed(int n)
    {
        var kb = Keyboard.current;
        if (kb == null || n < 0 || n > 9) return false;
        return Down(kb[n == 0 ? Key.Digit0 : Key.Digit1 + (n - 1)]);
    }

    // ---------- Mouse ----------

    public static bool ClickHeld => Held(Mouse.current?.leftButton);
    public static bool ClickPressed => Down(Mouse.current?.leftButton);

    // In screen pixels, y = 0 at the bottom (like the old Input.mousePosition).
    public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    // ---------- Helpers ----------

    private static bool Down(ButtonControl b) => b != null && b.wasPressedThisFrame;
    private static bool Held(ButtonControl b) => b != null && b.isPressed;

    private static bool AnyButton(Gamepad pad)
    {
        foreach (var control in pad.allControls)
            if (control is ButtonControl button && !control.synthetic && button.wasPressedThisFrame) return true;
        return false;
    }
}
