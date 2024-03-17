namespace SmartBuildingController
{
    public interface IFireAlarmManager
    {
        void SetAlarm(bool isActive);
        string GetStatus();
    }
}
