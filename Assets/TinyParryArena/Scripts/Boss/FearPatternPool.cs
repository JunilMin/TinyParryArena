using System;
using System.Collections.Generic;
using TinyParryArena.Fear;
using UnityEngine;

namespace TinyParryArena.Boss
{
    [Serializable]
    public sealed class FearPatternPool
    {
        [SerializeField] private FearState fearState;
        [SerializeField] private List<BossAttackPattern> patterns = new();

        public FearState FearState => fearState;
        public IReadOnlyList<BossAttackPattern> Patterns => patterns;
    }
}
