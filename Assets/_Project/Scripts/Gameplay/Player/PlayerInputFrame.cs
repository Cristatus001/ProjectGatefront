namespace ProjectGatefront.Gameplay.Player
{
    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
            bool pointerDown,
            bool pointerHeld,
            bool pointerUp,
            float horizontalDelta)
        {
            PointerDown = pointerDown;
            PointerHeld = pointerHeld;
            PointerUp = pointerUp;
            HorizontalDelta = horizontalDelta;
        }

        public bool PointerDown { get; }

        public bool PointerHeld { get; }

        public bool PointerUp { get; }

        public float HorizontalDelta { get; }
    }
}
