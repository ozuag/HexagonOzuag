    public interface IEdgeCollider
    {
        HexaFall.Basics.HexagonEdge Edge { get; }

        HexaFall.Basics.LinkedObject LinkedHexagon { get; }

        void SetColliderEnabledState(bool _state);

        bool ActiveEdge { get; }
    }
