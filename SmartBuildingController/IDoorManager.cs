namespace SmartBuildingController
{
    public interface IDoorManager
    {
        bool OpenDoor(int doorID);
        bool LockDoor(int doorID);
        bool OpenAllDoors();
        bool LockAllDoors();
        string GetStatus();
    }
}
