using ProjectGatefront.Data;

namespace ProjectGatefront.Gameplay.Gates
{
    public readonly struct GateResolution
    {
        public GateResolution(
            int startingValue,
            int finalValue,
            int squadBefore,
            int squadAfter,
            GateKind selectedGateType)
        {
            StartingValue = startingValue;
            FinalValue = finalValue;
            SquadBefore = squadBefore;
            SquadAfter = squadAfter;
            SelectedGateType = selectedGateType;
        }

        public int StartingValue { get; }
        public int FinalValue { get; }
        public int SquadBefore { get; }
        public int SquadAfter { get; }
        public GateKind SelectedGateType { get; }
    }
}
