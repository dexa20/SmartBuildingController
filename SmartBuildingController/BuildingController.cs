using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartBuildingController
{
    public class BuildingController
    {
        private string buildingID;
        private string currentState = "out of hours";
        private string previousState = ""; // Used for handling the 'History' state
        private ILightManager lightManager;
        private IFireAlarmManager fireAlarmManager;
        private IDoorManager doorManager;
        private IWebService webService;
        private IEmailService emailService;

        private readonly Dictionary<string, List<string>> stateTransitions = new Dictionary<string, List<string>>
        {
            { "closed", new List<string> { "out of hours" } },
            { "out of hours", new List<string> { "open", "closed" } },
            { "open", new List<string> { "out of hours" } },
            // Special states that can transition from any normal state
            { "fire drill", new List<string> { "closed", "out of hours", "open" } },
            { "fire alarm", new List<string> { "closed", "out of hours", "open" } },
        };

        public BuildingController(string id)
        {
            buildingID = id.ToLower();
        }

        public BuildingController(string id, string startState)
            : this(id) // Call the single parameter constructor
        {
            // Add a null check for startState before attempting to call ToLower()
            if (startState == null || !IsInitialStateValid(startState.ToLower()))
            {
                throw new ArgumentException("BuildingController can only be initialised to 'open', 'closed', 'out of hours'");
            }
            currentState = startState.ToLower();
        }


        public BuildingController(string id, ILightManager iLightManager, IFireAlarmManager iFireAlarmManager,
                                  IDoorManager iDoorManager, IWebService iWebService, IEmailService iEmailService)
            : this(id) // Call the single parameter constructor
        {
            lightManager = iLightManager ?? throw new ArgumentNullException(nameof(iLightManager));
            fireAlarmManager = iFireAlarmManager ?? throw new ArgumentNullException(nameof(iFireAlarmManager));
            doorManager = iDoorManager ?? throw new ArgumentNullException(nameof(iDoorManager));
            webService = iWebService ?? throw new ArgumentNullException(nameof(iWebService));
            emailService = iEmailService ?? throw new ArgumentNullException(nameof(iEmailService));
        }

        public string GetBuildingID() => buildingID;
        public string GetCurrentState() => currentState;

        public void SetBuildingID(string id) => buildingID = id.ToLower();

        public bool SetCurrentState(string state)
        {
            string loweredState = state.ToLower();

            if (currentState == loweredState) return true; // No change

            if (loweredState == "fire alarm" || loweredState == "fire drill")
            {
                previousState = currentState; // Save the current state to return to it later (History state)
            }
            else if (!stateTransitions.ContainsKey(currentState) || !stateTransitions[currentState].Contains(loweredState))
            {
                return false; // Invalid transition for normal states
            }

            switch (loweredState)
            {
                case "closed":
                    doorManager?.LockAllDoors();
                    lightManager?.SetAllLights(false);
                    break;
                case "open":
                    var doorsOpened = doorManager?.OpenAllDoors() ?? false;
                    if (!doorsOpened)
                    {
                        return false; // Failed to open doors, remain in current state
                    }
                    break;
                case "fire alarm":
                    ExecuteFireAlarmActions();
                    break;
                case "fire drill":
                    // For fire drill, there might be specific actions to add here
                    break;
                case "out of hours":
                    // Optional: Add any specific actions for transitioning to 'out of hours'
                    break;
                case "history":
                    // Transition to the 'History' state (return to previous state)
                    currentState = previousState;
                    return SetCurrentState(previousState); // Recursive call to set the previous state
            }

            currentState = loweredState;
            return true;
        }

        private void ExecuteFireAlarmActions()
        {
            fireAlarmManager?.SetAlarm(true);
            doorManager?.OpenAllDoors();
            lightManager?.SetAllLights(true);
            try
            {
                webService?.LogFireAlarm("fire alarm");
            }
            catch (Exception ex)
            {
                emailService?.SendMail("smartbuilding@uclan.ac.uk", "failed to log alarm", ex.Message);
            }
        }

        public string GetStatusReport()
        {
            var lightStatus = lightManager?.GetStatus();
            var doorStatus = doorManager?.GetStatus();
            var fireAlarmStatus = fireAlarmManager?.GetStatus();

            var statuses = new[] { lightStatus, doorStatus, fireAlarmStatus };
            var faults = statuses.Where(s => s?.Contains("FAULT") == true).ToList();
            var faultDevices = faults.Select(s => s.Split(',')[0]);

            if (faults.Any())
            {
                webService?.LogEngineerRequired(string.Join(",", faultDevices) + ",");
            }

            return string.Join(",", statuses);
        }

        private bool IsInitialStateValid(string startState) => new[] { "closed", "out of hours", "open" }.Contains(startState);

    }
}
