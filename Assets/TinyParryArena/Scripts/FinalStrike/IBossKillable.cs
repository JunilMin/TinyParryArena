namespace TinyParryArena.FinalStrike
{
    public interface IBossKillable
    {
        bool IsAlive { get; }
        void KillByFinalStrike();
    }
}
