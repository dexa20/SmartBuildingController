namespace SmartBuildingController
{
    public interface ILightManager : IManager
    {
        bool SetAllLights(bool isOn);
    }
}
