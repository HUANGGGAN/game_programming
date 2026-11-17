using System;
using UnityEngine;

public class IceLakeManager : MonoBehaviour
{
    [Serializable]
    public class IceGate
    {
        public FrozenEnemies firstIce;
        public FrozenEnemies secondIce;
        public GameObject invisibleWall;
    }

    [Tooltip("Pairs 1+2, 3+4, and 5+6, with their corresponding walls.")]
    [SerializeField] private IceGate[] gates = new IceGate[3];

    private void OnEnable()
    {
        if (gates == null) return;
        foreach (IceGate gate in gates)
        {
            if (gate == null) continue;
            if (gate.firstIce != null) gate.firstIce.Broken += OnIceBroken;
            if (gate.secondIce != null) gate.secondIce.Broken += OnIceBroken;
        }
        RefreshWalls();
    }

    private void OnIceBroken(FrozenEnemies ice) => RefreshWalls();

    private void RefreshWalls()
    {
        if (gates == null) return;
        foreach (IceGate gate in gates)
            if (gate != null && gate.firstIce != null && gate.secondIce != null &&
                gate.firstIce.IsBroken && gate.secondIce.IsBroken && gate.invisibleWall != null)
                gate.invisibleWall.SetActive(false);
    }

    private void OnDisable()
    {
        if (gates == null) return;
        foreach (IceGate gate in gates)
        {
            if (gate == null) continue;
            if (gate.firstIce != null) gate.firstIce.Broken -= OnIceBroken;
            if (gate.secondIce != null) gate.secondIce.Broken -= OnIceBroken;
        }
    }
}
