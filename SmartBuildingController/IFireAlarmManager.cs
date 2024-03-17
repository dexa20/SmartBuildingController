namespace SmartBuildingController
{
    public interface IFireAlarmManager : IManager
    {
        bool SetAlarm(bool isActive);
    }
}
