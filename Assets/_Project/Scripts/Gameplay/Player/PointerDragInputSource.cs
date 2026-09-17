using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProjectGatefront.Gameplay.Player
{
    public sealed class PointerDragInputSource : IPlayerInputSource
    {
        public PlayerInputFrame ReadFrame()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                ButtonControl touchPress = touchscreen.primaryTouch.press;
                if (touchPress.isPressed
                    || touchPress.wasPressedThisFrame
                    || touchPress.wasReleasedThisFrame)
                {
                    float touchDelta = ReadNormalizedDelta(
                        touchscreen.primaryTouch.delta.ReadValue().x,
                        touchPress.wasPressedThisFrame,
                        touchPress.isPressed);

                    return new PlayerInputFrame(
                        touchPress.wasPressedThisFrame,
                        touchPress.isPressed,
                        touchPress.wasReleasedThisFrame,
                        touchDelta);
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return default;
            }

            ButtonControl mousePress = mouse.leftButton;
            float mouseDelta = ReadNormalizedDelta(
                mouse.delta.ReadValue().x,
                mousePress.wasPressedThisFrame,
                mousePress.isPressed);

            return new PlayerInputFrame(
                mousePress.wasPressedThisFrame,
                mousePress.isPressed,
                mousePress.wasReleasedThisFrame,
                mouseDelta);
        }

        private static float ReadNormalizedDelta(float pixelDelta, bool pressedThisFrame, bool isPressed)
        {
            if (!isPressed || pressedThisFrame)
            {
                return 0f;
            }

            return Mathf.Clamp(pixelDelta / Mathf.Max(Screen.width, 1), -1f, 1f);
        }
    }
}
