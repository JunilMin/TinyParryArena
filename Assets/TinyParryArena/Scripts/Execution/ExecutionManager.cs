using System;
using TinyParryArena.BossFramework;
using UnityEngine;

namespace TinyParryArena.Execution
{
    [DisallowMultipleComponent]
    public sealed class ExecutionManager : MonoBehaviour
    {
        public bool IsExecuting { get; private set; }

        public event Action<ExecutionType> ExecutionStarted;
        public event Action ExecutionFinished;

        public bool TryBegin(ExecutionType executionType)
        {
            if (IsExecuting)
                return false;

            IsExecuting = true;
            ExecutionStarted?.Invoke(executionType);
            return true;
        }

        public void Finish()
        {
            if (!IsExecuting)
                return;

            IsExecuting = false;
            ExecutionFinished?.Invoke();
        }

        public void Cancel()
        {
            IsExecuting = false;
        }
    }
}
