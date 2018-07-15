using Unity.Entities;

namespace AnimationBaker.Interfaces
{
    public interface IUnitState : IComponentData
    {
        float Runtime { get; set; }
        float StateTimer { get; set; }
        int CurrentState { get; set; }
        int PreviousState { get; set; }
        float this [int index] { get; set; }
    }
}
