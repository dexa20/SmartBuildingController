using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBuildingController
{
    /// <summary>
    /// Manages the state and operations of a smart building, including doors, lights, fire alarms, and more.
    /// Allows state transitions according to predefined rules and performs actions based on the state changes.
    /// </summary>
    public class BuildingController
    {
        private string buildingID;
        private string currentState;
        private string historyState;
        private readonly HashSet<string> validStates = new HashSet<string>
        {
            "closed",
            "out of hours",
            "open",
            "fire drill",
            "fire alarm"
        };

        private readonly Dictionary<string, HashSet<string>> validTransitions = new Dictionary<string, HashSet<string>>
        {
            { "closed", new HashSet<string>{ "out of hours", "fire drill", "fire alarm" } },
            { "out of hours", new HashSet<string>{ "open", "closed", "fire drill", "fire alarm" } },
            { "open", new HashSet<string>{ "out of hours", "fire drill", "fire alarm" } },
        };

        private ILightManager lightManager;
        private IFireAlarmManager fireAlarmManager;
        private IDoorManager doorManager;
        private IWebService webService;
        private IEmailService emailService;

        /// <summary>
        /// Initializes a new instance of the BuildingController class with a default state.
        /// </summary>
        /// <param name="id">The unique identifier for the building.</param>
        public BuildingController(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = id.ToLower();
            }
            buildingID = id;

            currentState = "out of hours";
        }

        /// <summary>
        /// Initializes a new instance of the BuildingController class with a specified starting state.
        /// Throws an exception if the start state is not valid.
        /// </summary>
        /// <param name="id">The unique identifier for the building.</param>
        /// <param name="startState">The initial state of the building.</param>
        public BuildingController(string id, string startState)
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = id.ToLower();
            }
            buildingID = id;

            startState = startState.ToLower();

            if (validStates.Contains(startState))
            {
                currentState = startState;
                historyState = startState;
            }
            else
            {
                throw new ArgumentException("Argument Exception: BuildingController can only be initialised to the following states 'open', 'closed', 'out of hours'");
            }
        }

        /// <summary>
        /// Initializes a new instance of the BuildingController class with specified managers and services.
        /// </summary>
        /// <param name="id">The unique identifier for the building.</param>
        /// <param name="lightMgr">The light management service.</param>
        /// <param name="fireAlarmMgr">The fire alarm management service.</param>
        /// <param name="doorMgr">The door management service.</param>
        /// <param name="webSvc">The web service for logging and notifications.</param>
        /// <param name="emailSvc">The email service for sending alerts.</param>
        public BuildingController(string id, ILightManager lightMgr, IFireAlarmManager fireAlarmMgr, IDoorManager doorMgr, IWebService webSvc, IEmailService emailSvc)
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = id.ToLower();
            }
            buildingID = id;

            lightManager = lightMgr;
            fireAlarmManager = fireAlarmMgr;
            doorManager = doorMgr;
            webService = webSvc;
            emailService = emailSvc;
        }

        /// <summary>
        /// Gets the unique identifier of the building.
        /// </summary>
        /// <returns>The building ID.</returns>
        public string? GetBuildingID() => buildingID;

        /// <summary>
        /// Sets the unique identifier of the building.
        /// </summary>
        /// <param name="id">The new building ID.</param>
        public void SetBuildingID(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = id.ToLower();
            }

            buildingID = id;
        }

        /// <summary>
        /// Gets the current state of the building.
        /// </summary>
        /// <returns>The current state.</returns>
        public string GetCurrentState() => currentState;


        /// <summary>
        /// Attempts to set the current state of the building. Validates the transition and performs necessary actions based on the state.
        /// </summary>
        /// <param name="state">The new state to transition to.</param>
        /// <returns>True if the state transition is successful, otherwise false.</returns>
        public bool SetCurrentState(string state)
        {
            string lowerCaseState = state.ToLower();
            if (!validStates.Contains(lowerCaseState))
                return false;

            if (lowerCaseState == currentState)
                return true;

            if (!IsTransitionValid(currentState, lowerCaseState))
                return false;

            if ((lowerCaseState == "fire drill" || lowerCaseState == "fire alarm") &&
                (currentState == "open" || currentState == "closed" || currentState == "out of hours"))
            {
                historyState = currentState;
            }

            switch (lowerCaseState)
            {
                case "open":
                    if (currentState != "fire drill" && currentState != "fire alarm")
                    {
                        if (doorManager == null || !doorManager.OpenAllDoors())
                            return false;
                    }
                    break;
                case "closed":
                    if (doorManager != null)
                        doorManager.LockAllDoors();
                    if (lightManager != null)
                        lightManager.SetAllLights(false);
                    break;
                case "fire alarm":
                    if (fireAlarmManager == null || doorManager == null || lightManager == null)
                        return false;

                    fireAlarmManager.SetAlarm(true);
                    doorManager.OpenAllDoors();
                    lightManager.SetAllLights(true);
                    try
                    {
                        webService.LogFireAlarm("Fire alarm activated");
                    }
                    catch (Exception ex)
                    {
                        emailService.SendMail("smartbuilding@uclan.ac.uk", "failed to log alarm", ex.Message);
                    }
                    break;
                case "fire drill":
                    break;
            }

            if ((currentState == "fire drill" || currentState == "fire alarm") &&
                (lowerCaseState == "open" || lowerCaseState == "closed" || lowerCaseState == "out of hours"))
            {
                currentState = historyState;
                historyState = null;
            }
            else
            {
                currentState = lowerCaseState;
            }

            return true;
        }

        /// <summary>
        /// Validates if a state transition is allowed from the current state to the proposed state.
        /// </summary>
        /// <param name="fromState">The current state.</param>
        /// <param name="toState">The proposed new state.</param>
        /// <returns>True if the transition is valid, otherwise false.</returns>
        private bool IsTransitionValid(string fromState, string toState)
        {
            switch (fromState)
            {
                case "closed":
                    switch (toState)
                    {
                        case "out of hours":
                        case "fire drill":
                        case "fire alarm":
                            return true;
                        default:
                            break;
                    }
                    break;
                case "out of hours":
                    switch (toState)
                    {
                        case "open":
                        case "closed":
                        case "fire drill":
                        case "fire alarm":
                            return true;
                        default:
                            break;
                    }
                    break;
                case "open":
                    switch (toState)
                    {
                        case "out of hours":
                        case "fire drill":
                        case "fire alarm":
                            return true;
                        default:
                            break;
                    }
                    break;
                case "fire drill":
                case "fire alarm":
                    if (toState == historyState)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Generates a status report of the building, including the status of lights, doors, and fire alarms.
        /// Logs engineer requirement if any faults are detected.
        /// </summary>
        /// <returns>A string representing the status report of the building.</returns>
        public string GetStatusReport()
        {
            string statusReport = "";

            if (lightManager != null && doorManager != null && fireAlarmManager != null)
            {
                string lightStatus = lightManager.GetStatus();
                string doorStatus = doorManager.GetStatus();
                string fireAlarmStatus = fireAlarmManager.GetStatus();

                statusReport = $"{lightStatus}{doorStatus}{fireAlarmStatus}";

                var faultyDevices = new List<string>();

                if (lightStatus.Contains("FAULT")) faultyDevices.Add("Lights");
                if (doorStatus.Contains("FAULT")) faultyDevices.Add("Doors");
                if (fireAlarmStatus.Contains("FAULT")) faultyDevices.Add("FireAlarm");

                if (faultyDevices.Count > 0 && webService != null)
                {
                    string faultMessage = faultyDevices.Count == 1 ? faultyDevices.First() : string.Join(",", faultyDevices) + ",";

                    webService.LogEngineerRequired(faultMessage);
                }
            }
            return statusReport;
        }

    }
}
