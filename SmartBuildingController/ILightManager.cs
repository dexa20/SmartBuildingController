namespace SmartBuildingController
{
    public interface ILightManager
    {
        void SetLight(bool isOn, int lightID);
        void SetAllLights(bool isOn);
        string GetStatus();
    }
}
