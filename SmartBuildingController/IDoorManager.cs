namespace SmartBuildingController
{
    public interface IDoorManager : IManager
    {
        bool OpenAllDoors();
        bool LockAllDoors();
    }
}
