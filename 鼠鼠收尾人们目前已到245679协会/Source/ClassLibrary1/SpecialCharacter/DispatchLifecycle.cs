using System;

namespace XIYUNTE.SpecialCharacter
{
    public enum DispatchStage { Waiting, InFlight, Arrived, Failed }

    // 到达通知只允许在本次派遣由飞行状态转为完成状态时提交。
    public sealed class DispatchLifecycle
    {
        public DispatchStage stage;
        public int arrivalTick;
        public bool arrivalLetterSent;

        public bool IsDue(int tick) => stage == DispatchStage.Waiting && tick >= arrivalTick;

        public void Launch()
        {
            if (stage != DispatchStage.Waiting)
                throw new InvalidOperationException("Dispatch is not waiting for launch.");
            stage = DispatchStage.InFlight;
        }

        public bool Complete()
        {
            if (stage == DispatchStage.Arrived && arrivalLetterSent) return false;
            if (stage != DispatchStage.InFlight || arrivalLetterSent)
                throw new InvalidOperationException("Dispatch cannot complete from its current state.");
            stage = DispatchStage.Arrived;
            arrivalLetterSent = true;
            return true;
        }

        public void Fail()
        {
            if (stage == DispatchStage.Arrived)
                throw new InvalidOperationException("A completed dispatch cannot fail.");
            stage = DispatchStage.Failed;
        }
    }
}
