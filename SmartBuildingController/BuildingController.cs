using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBuildingController
{
    public class BuildingController
    {
        private string _buildingID;
        private string _currentState;
        private string _historyState;
        private readonly HashSet<string> _validStates = new HashSet<string> { "closed", "out of hours", "open", "fire drill", "fire alarm" };

        private ILightManager _lightManager;
        private IFireAlarmManager _fireAlarmManager;
        private IDoorManager _doorManager;
        private IWebService _webService;
        private IEmailService _emailService;

        public BuildingController(string id)
        {
            _buildingID = id.ToLower();
            _currentState = "out of hours";
            _historyState = _currentState;
        }

        public BuildingController(string id, string startState) : this(id)
        {
            SetInitialState(startState.ToLower());
        }

        public BuildingController(
            string id,
            ILightManager lightManager,
            IFireAlarmManager fireAlarmManager,
            IDoorManager doorManager,
            IWebService webService,
            IEmailService emailService) : this(id)
        {
            _lightManager = lightManager;
            _fireAlarmManager = fireAlarmManager;
            _doorManager = doorManager;
            _webService = webService;
            _emailService = emailService;
        }

        private void SetInitialState(string startState)
        {
            if (startState == "closed" || startState == "out of hours" || startState == "open")
            {
                _currentState = startState;
                _historyState = _currentState;
            }
            else
            {
                throw new ArgumentException("Argument Exception: BuildingController can only be initialised to the following states 'open', 'closed', 'out of hours'");
            }
        }


        public string GetBuildingID() => _buildingID;

        public void SetBuildingID(string id) => _buildingID = id.ToLower();

        public string GetCurrentState() => _currentState;

        public bool SetCurrentState(string newState)
        {
            newState = newState.ToLower();

            if (_currentState.ToLower() == newState)
            {
                return true;
            }

            if (!_validStates.Contains(newState) || !IsValidTransition(newState))
            {
                return false;
            }

            if (_validStates.Contains(_currentState) && (newState == "fire drill" || newState == "fire alarm"))
            {
                _historyState = _currentState;
            }

            if (_currentState == "fire drill" || _currentState == "fire alarm")
            {
                newState = _historyState;
            }

            if (!ApplyStateTransitionEffects(newState))
            {
                return false;
            }

            _currentState = newState;
            return true;
        }


        private bool IsValidTransition(string newState)
        {
            if (_validStates.Contains(newState))
            {
                switch (_currentState)
                {
                    case "closed":
                        return newState == "out of hours" || newState == "fire drill" || newState == "fire alarm";
                    case "out of hours":
                        return newState == "open" || newState == "closed" || newState == "fire drill" || newState == "fire alarm";
                    case "open":
                        return newState == "out of hours" || newState == "fire drill" || newState == "fire alarm";
                    case "fire drill":
                    case "fire alarm":
                        return newState == _historyState;
                    default:
                        return false;
                }
            }
            return false;
        }



        private bool ApplyStateTransitionEffects(string newState)
        {
            try
            {
                switch (newState)
                {
                    case "open":
                        if (_doorManager == null || !_doorManager.OpenAllDoors())
                        {
                            return false;
                        }
                        break;
                    case "closed":
                        _doorManager?.LockAllDoors();
                        _lightManager?.SetAllLights(false);
                        break;
                    case "out of hours":
                        break;
                    case "fire drill":
                        _fireAlarmManager?.SetAlarm(false);
                        _doorManager?.OpenAllDoors();
                        _lightManager?.SetAllLights(true);
                        break;
                    case "fire alarm":
                        _fireAlarmManager?.SetAlarm(true);
                        _doorManager?.OpenAllDoors();
                        _lightManager?.SetAllLights(true);

                        try
                        {
                            _webService?.LogFireAlarm("fire alarm");
                        }
                        catch (Exception logEx)
                        {
                            _emailService?.SendMail("smartbuilding@uclan.ac.uk", "failed to log alarm", logEx.Message);
                        }
                        break;
                }
                _currentState = newState;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }



        public string GetStatusReport()
        {
            var statusBuilder = new StringBuilder();
            var faultyDevices = new List<string>();

            string lightStatus = _lightManager?.GetStatus();
            if (!string.IsNullOrWhiteSpace(lightStatus))
            {
                statusBuilder.Append(lightStatus);
                if (lightStatus.Contains("FAULT"))
                {
                    faultyDevices.Add("Lights");
                }
            }


            string doorStatus = _doorManager?.GetStatus();
            if (!string.IsNullOrWhiteSpace(doorStatus))
            {
                statusBuilder.Append(doorStatus);
                if (doorStatus.Contains("FAULT"))
                {
                    faultyDevices.Add("Doors");
                }
            }

            string fireAlarmStatus = _fireAlarmManager?.GetStatus();
            if (!string.IsNullOrWhiteSpace(fireAlarmStatus))
            {
                statusBuilder.Append(fireAlarmStatus);
                if (fireAlarmStatus.Contains("FAULT"))
                {
                    faultyDevices.Add("FireAlarm");
                }
            }


            if (faultyDevices.Any())
            {
                string faultReport = string.Join(",", faultyDevices) + ",";
                _webService?.LogEngineerRequired(faultReport);
            }

            return statusBuilder.ToString();
        }

    }
}
