using NUnit.Framework;
using System.Reflection;
using SmartBuildingController;
using System.Globalization;
using NSubstitute;

namespace BuildingControllerTests
{
    /// <summary>
    /// Test Fixture for the class <see cref="BuildingController"/>.
    /// </summary>
    [TestFixture]
    public class BuildingControllerTests
    {
        /// <summary>
        /// Valid states for the <see cref="BuildingController"/>
        /// </summary>
        struct BuildingState
        {
            public const string closed = "closed";
            public const string outOfHours = "out of hours";
            public const string open = "open";
            public const string fireDrill = "fire drill";
            public const string fireAlarm = "fire alarm";
        }

        /// <summary>
        /// Argument names for the <see cref="BuildingController"/> constructor.
        /// </summary>
        struct ControllerArgumentNames
        {
            public const string buildingID = "id";
            public const string startState = "startState";
            public const string lightManager = "iLightManager";
            public const string fireAlarmManager = "iFireAlarmManager";
            public const string doorManager = "iDoorManager";
            public const string webService = "iWebService";
            public const string emailService = "iEmailService";
        }

        /// <summary>
        /// Store expected strings for <see cref="BuildingController"/> tests.
        /// </summary>
        struct ExpectedTexts
        {
            public const string initialStateException = "Argument Exception: BuildingController can only be initialised "
                + "to the following states 'open', 'closed', 'out of hours'";
            public const string emailSubject = "failed to log alarm";
            public const string emailAddress = "smartbuilding@uclan.ac.uk";
        }

        /// <summary>
        /// Testing strings for managers.
        /// </summary>
        struct ManagerStatus
        {
            public const string lights = "Lights";
            public const string doors = "Doors";
            public const string alarm = "FireAlarm";

            public const string lightsWithComma = lights + ",";
            public const string doorsWithComma = doors + ",";
            public const string alarmWithComma = alarm + ",";

            public const string oneDeviceOk = "OK,";
            public const string oneDeviceFaulty = "FAULT,";
            public const string twoDevicesOk = oneDeviceOk + oneDeviceOk;
            public const string twoDevicesFaulty = oneDeviceFaulty + oneDeviceFaulty;
            public const string oneDeviceOkOneDeviceFaulty = oneDeviceOk + oneDeviceFaulty;
            public const string threeDevicesOk = oneDeviceOk + twoDevicesOk;
            public const string threeDevicesFaulty = oneDeviceFaulty + twoDevicesFaulty;
            public const string twoDevicesOkOneDeviceFaulty = oneDeviceOk + oneDeviceFaulty + oneDeviceOk;
            public const string oneDeviceOkTwoDevicesFaulty = oneDeviceFaulty + oneDeviceOk + oneDeviceFaulty;
            public const string fiveDevicesOk = twoDevicesOk + threeDevicesOk;
            public const string fiveDevicesFaulty = threeDevicesFaulty + twoDevicesFaulty;
            public const string tenDevicesOk = fiveDevicesOk + fiveDevicesOk;
            public const string tenDevicesFaulty = fiveDevicesFaulty + fiveDevicesFaulty;
            public const string fiveDevicesOkFiveDevicesFaulty = oneDeviceOkOneDeviceFaulty +
                oneDeviceOkOneDeviceFaulty + oneDeviceOkOneDeviceFaulty + oneDeviceOkOneDeviceFaulty +
                oneDeviceOkOneDeviceFaulty;
            public const string fiftyDevicesOk = tenDevicesOk + tenDevicesOk + tenDevicesOk + tenDevicesOk + tenDevicesOk;
            public const string fiftyDevicesFaulty = tenDevicesFaulty + tenDevicesFaulty + tenDevicesFaulty +
                tenDevicesFaulty + tenDevicesFaulty;
            public const string fiftyDevicesOkFaultyMixed = fiveDevicesOkFiveDevicesFaulty + fiveDevicesOkFiveDevicesFaulty +
                fiveDevicesOkFiveDevicesFaulty + fiveDevicesOkFiveDevicesFaulty + fiveDevicesOkFiveDevicesFaulty;

        }

        private static readonly object[] validStates =
        {
            BuildingState.closed,
            BuildingState.outOfHours,
            BuildingState.open,
            BuildingState.fireAlarm,
            BuildingState.fireDrill
        };

        private static readonly object[] normalStates =
        {
            BuildingState.closed,
            BuildingState.outOfHours,
            BuildingState.open
        };

        private static readonly object[] InvalidBuildingStates =
        {
            "out of service",
            "invalid"
        };

        /// <summary>
        /// Array containing a variety of strings to test against.
        /// </summary>
        private static readonly object?[] TestStrings =
        {
            null,
            "",
            "null",
            "abcdefghijklmnopqrstuvwxyz",
            "01234567890",
            "Testing 1 testing 2 testing 3",
            "卐卐卐卐卐卐卐卐卐卐卐卐卐卐卐卐",
            "@, !, #, $, &, *, (, ), _, +",
            "'",
            ",",
            "\"",
            "!@#$%^&*(){}?+_:;/=-]['",
            "   ",
            "\n",
            "\r",
            "\0X12FAE6",
            "\x13",
            "Society's needs come before individuals' needs."
        };

        private static readonly object?[] OkManagerStatuses =
        {
            ManagerStatus.oneDeviceOk,
            ManagerStatus.threeDevicesOk,
            ManagerStatus.fiveDevicesOk,
            ManagerStatus.tenDevicesOk,
            ManagerStatus.fiftyDevicesOk,
        };

        private static readonly object?[] FaultyManagerStatuses =
        {
            ManagerStatus.oneDeviceFaulty,
            ManagerStatus.twoDevicesOkOneDeviceFaulty,
            ManagerStatus.tenDevicesFaulty,
            ManagerStatus.fiveDevicesOkFiveDevicesFaulty,
            ManagerStatus.fiftyDevicesOkFaultyMixed,
            ManagerStatus.fiftyDevicesFaulty
        };

        /// <summary>
        /// Example return values for <see cref="ILightManager"/> stubs.
        /// </summary>
        private static readonly object?[] LightManagerStatuses =
        {
            ManagerStatus.lightsWithComma,
            ManagerStatus.lightsWithComma + ManagerStatus.twoDevicesOkOneDeviceFaulty,
            ManagerStatus.lightsWithComma + ManagerStatus.tenDevicesOk,
            ManagerStatus.lightsWithComma + ManagerStatus.tenDevicesFaulty,
            ManagerStatus.lightsWithComma + ManagerStatus.fiftyDevicesOk,
        };

        /// <summary>
        /// Example return values for <see cref="IDoorManager"/> stubs.
        /// </summary>
        private static readonly object?[] DoorManagerStatuses =
        {
            ManagerStatus.doorsWithComma,
            ManagerStatus.doorsWithComma + ManagerStatus.twoDevicesOkOneDeviceFaulty,
            ManagerStatus.doorsWithComma + ManagerStatus.tenDevicesOk,
            ManagerStatus.doorsWithComma + ManagerStatus.tenDevicesFaulty,
            ManagerStatus.doorsWithComma + ManagerStatus.fiftyDevicesOk,
        };

        /// <summary>
        /// Example return values for <see cref="IFireAlarmManager"/> stubs.
        /// </summary>
        private static readonly object?[] AlarmManagerStatuses =
        {
            ManagerStatus.alarmWithComma,
            ManagerStatus.alarmWithComma + ManagerStatus.twoDevicesOkOneDeviceFaulty,
            ManagerStatus.alarmWithComma + ManagerStatus.tenDevicesOk,
            ManagerStatus.alarmWithComma + ManagerStatus.tenDevicesFaulty,
            ManagerStatus.alarmWithComma + ManagerStatus.fiftyDevicesOk,
        };


        // LEVEL 1 TESTS //

        /// <summary>
        /// Test if a valid constructor exists for <see cref="BuildingController"/> through reflection.
        /// Satisfies <strong>L1R1</strong>.
        /// </summary>
        [Test]
        public void Constructor_WhenSingleParameter_HasCorrectSignature()
        {
            string? parameterName = null;
            ConstructorInfo? constructorInfoObject;
            Type[] argTypes = new Type[] { typeof(string) };

            // Lookup constructor with specified parameter
            constructorInfoObject = typeof(BuildingController).GetConstructor(argTypes);
            Assume.That(constructorInfoObject, Is.Not.Null);

            if (constructorInfoObject != null)
            {
                // Verify parameter name
                ParameterInfo[] constructorParams = constructorInfoObject.GetParameters();
                ParameterInfo firstParam = constructorParams.First();
                parameterName = firstParam.Name;
            }

            Assert.That(parameterName, Is.EqualTo(ControllerArgumentNames.buildingID));
        }

        /// <summary>
        /// Test initialisation of <c>buildingID</c> when constructor parameter set.
        /// Satisfies <strong>L1R2</strong>, <strong>L1R3</strong>.
        /// </summary>
        [TestCase("Building ID")]
        [TestCaseSource(nameof(TestStrings))]
        public void Constructor_WhenSet_InitialisesBuildingID(string buildingID)
        {
            BuildingController controller;

            controller = new BuildingController(buildingID);
            string result = controller.GetBuildingID();


            string expected = buildingID;
            if (!string.IsNullOrEmpty(buildingID))
            {
                expected = expected.ToLower();
            }

            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Test <c>buildingID</c> setter.
        /// Satisfies <strong>L1R4</strong>.
        /// </summary>
        [TestCase("Building ID")]
        [TestCaseSource(nameof(TestStrings))]
        public void SetBuildingID_WhenSet_SetsID(string buildingID)
        {
            BuildingController controller = new("");

            controller.SetBuildingID(buildingID);
            string result = controller.GetBuildingID();

            string expected = buildingID;
            if (!string.IsNullOrEmpty(buildingID))
            {
                expected = expected.ToLower();
            }

            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Test default initialisation of <c>currentState</c>.
        /// Satisfies <strong>L1R5</strong>, <strong>L1R6</strong>.
        /// </summary>
        [Test]
        public void Constructor_ByDefault_InitialisesCurrentState()
        {
            BuildingController controller;

            controller = new BuildingController("");
            string result = controller.GetCurrentState();

            Assert.That(result, Is.EqualTo(BuildingState.outOfHours));
        }


        // LEVEL 2 TESTS //

        /// <summary>
        /// Test <see cref="BuildingController.SetCurrentState"/> when setting the same state.
        /// </summary>
        /// For L2R1.
        [TestCase(BuildingState.outOfHours)]
        [TestCase(BuildingState.open)]
        [TestCase(BuildingState.closed)]
        public void SetCurrentState_WhenCurrentStateSame_SetsState(string state)
        {
            BuildingController controller = new("", state);
            bool result = controller.SetCurrentState(state);

            Assert.That(result, Is.True);
        }


        ////////////////////////////////


        /// <summary>
        /// Test that a two-parameter constructor for <see cref="BuildingController"/> exists.
        /// Satisfies <strong>L2R1</strong>, <strong>L2R2</strong>.
        /// </summary>
        [Test]
        public void Constructor_WhenTwoParameters_HasCorrectSignature()
        {
            string? firstArgName = null;
            string? secondArgName = null;
            ConstructorInfo? constructorInfoObj;
            Type[] argTypes = new Type[] { typeof(string), typeof(string) };

            // Lookup two parameter constructor
            constructorInfoObj = typeof(BuildingController).GetConstructor(argTypes);
            Assume.That(constructorInfoObj, Is.Not.Null);

            if (constructorInfoObj != null)
            {
                ParameterInfo[] constructorParams = constructorInfoObj.GetParameters();
                ParameterInfo firstParam = constructorParams.ElementAt(0);
                ParameterInfo secondParam = constructorParams.ElementAt(1);

                // Verify parameter names
                firstArgName = firstParam.Name;
                secondArgName = secondParam.Name;
            }

            Assert.That(firstArgName, Is.EqualTo(ControllerArgumentNames.buildingID));
            Assert.That(secondArgName, Is.EqualTo(ControllerArgumentNames.startState));
        }

        /// <summary>
        /// Test constructor when using startState argument with a normal state.
        /// Satisfies <strong>L2R3</strong>.
        /// </summary>
        [Test, TestCaseSource(nameof(normalStates))]
        public void Constructor_WhenNormalState_SetsStartState(string state)
        {
            BuildingController controller;

            controller = new BuildingController("", state);
            string result = controller.GetCurrentState();

            Assert.That(result, Is.EqualTo(state));
        }

        /// <summary>
        /// Test constructor when using startState argument with a normal state in capital letters.
        /// Satisfies <strong>L2R3</strong>.
        /// </summary>
        [TestCaseSource(nameof(normalStates))]
        public void Constructor_WhenNormalStateCapitals_SetsStartState(string state)
        {
            BuildingController controller;

            controller = new BuildingController("", state.ToUpper());
            string result = controller.GetCurrentState();

            Assert.That(result, Is.EqualTo(state));
        }

        /// <summary>
        /// Test constructor when using startState argument with a normal state in title case.
        /// Satisfies <strong>L2R3</strong>.
        /// </summary>
        [TestCaseSource(nameof(normalStates))]
        public void Constructor_WhenNormalStateMixedCapitals_SetsStartState(string state)
        {
            BuildingController controller;
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;

            controller = new BuildingController("", ti.ToTitleCase(state));
            string result = controller.GetCurrentState();

            Assert.That(result, Is.EqualTo(state));
        }



        // LEVEL 3 TESTS //


        /// <summary>
        /// Test the <see cref="BuildingController.SetCurrentState"/>
        /// when moving to <c>open</c> state if already there.
        /// Satisfies <strong>L3R4</strong>.
        /// </summary>
        [Test]
        public void SetCurrentState_WhenMovingFromOpenToOpen_DoesNotCallOpenAllDoors()
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            doorManager.OpenAllDoors().Returns(true);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            // Since the state will be open already
            // there is no need to call the OpenAllDoors method again
            controller.SetCurrentState(BuildingState.open);
            doorManager.ClearReceivedCalls();
            controller.SetCurrentState(BuildingState.open);

            doorManager.DidNotReceive().OpenAllDoors();
        }

        // LEVEL 4 TESTS //


        // L4R1

        /// <summary>
        /// Test that <see cref="BuildingController.GetStatusReport"/>
        /// calls the <see cref="IWebService.LogEngineerRequired"/>
        /// method if a fault was detected.
        /// Satisfies <strong>L4R1</strong>, <strong>L4R2</strong>.
        /// </summary>
        [TestCase(ManagerStatus.twoDevicesOkOneDeviceFaulty, ManagerStatus.twoDevicesOkOneDeviceFaulty, ManagerStatus.twoDevicesOkOneDeviceFaulty)]
        [TestCase(ManagerStatus.fiftyDevicesOk, ManagerStatus.fiftyDevicesOk, ManagerStatus.oneDeviceFaulty)]
        [TestCase(ManagerStatus.oneDeviceFaulty, ManagerStatus.fiftyDevicesOk, ManagerStatus.oneDeviceOk)]
        [TestCase(ManagerStatus.tenDevicesFaulty, ManagerStatus.tenDevicesFaulty, ManagerStatus.tenDevicesFaulty)]
        [TestCase(ManagerStatus.oneDeviceFaulty, ManagerStatus.oneDeviceFaulty, ManagerStatus.oneDeviceFaulty)]
        public void GetStatusReport_WhenFindsFaults_CallsLogEngineerRequired(
            string lightStatus, string doorStatus, string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            // Test part one of the requirement
            webService.Received(1).LogEngineerRequired(Arg.Any<string>());
        }

        /// <summary>
        /// Test that <see cref="BuildingController.GetStatusReport"/>
        /// does not call the <see cref="IWebService.LogEngineerRequired"/>
        /// method if a fault was not detected.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenAllOk_DoesNotCallLogEngineerRequired(
            [ValueSource(nameof(OkManagerStatuses))] string lightStatus,
            [ValueSource(nameof(OkManagerStatuses))] string doorStatus,
            [ValueSource(nameof(OkManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.DidNotReceive().LogEngineerRequired(Arg.Any<string>());
        }

        /// <summary>
        /// Test that <see cref="BuildingController.GetStatusReport"/>
        /// calls the <see cref="IWebService.LogEngineerRequired"/>
        /// method if a fault was detected in the lights manager.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsSingleManagerInLights_CallsLogEngineerRequired(
            [ValueSource(nameof(FaultyManagerStatuses))] string lightStatus,
            [ValueSource(nameof(OkManagerStatuses))] string doorStatus,
            [ValueSource(nameof(OkManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(ManagerStatus.lights);
        }

        /// <summary>
        /// Test that <see cref="BuildingController.GetStatusReport"/>
        /// calls the <see cref="IWebService.LogEngineerRequired"/>
        /// method if a fault was detected in the doors manager.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsSingleManagerInDoors_CallsLogEngineerRequired(
            [ValueSource(nameof(OkManagerStatuses))] string lightStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string doorStatus,
            [ValueSource(nameof(OkManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(ManagerStatus.doors);
        }

        /// <summary>
        /// Test that <see cref="BuildingController.GetStatusReport"/>
        /// calls the <see cref="IWebService.LogEngineerRequired"/>
        /// method if a fault was detected in the fire alarm manager.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsSingleManagerInAlarm_CallsLogEngineerRequired(
            [ValueSource(nameof(OkManagerStatuses))] string lightStatus,
            [ValueSource(nameof(OkManagerStatuses))] string doorStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(ManagerStatus.alarm);
        }

        /// <summary>
        /// Test the <see cref="BuildingController.GetStatusReport"/> method using stubs.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsAllManagers_CallsLogEngineerRequired(
            [ValueSource(nameof(FaultyManagerStatuses))] string lightStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string doorStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(string.Format("{0}{1}{2}",
                ManagerStatus.lightsWithComma, ManagerStatus.doorsWithComma, ManagerStatus.alarmWithComma));
        }

        /// <summary>
        /// Test the <see cref="BuildingController.GetStatusReport"/> method using stubs.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsLightsAndDoors_CallsLogEngineerRequired(
            [ValueSource(nameof(FaultyManagerStatuses))] string lightStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string doorStatus,
            [ValueSource(nameof(OkManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(string.Format("{0}{1}",
                ManagerStatus.lightsWithComma, ManagerStatus.doorsWithComma));
        }

        /// <summary>
        /// Test the <see cref="BuildingController.GetStatusReport"/> method using stubs.
        /// Satisfies <strong>L4R3</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsLightsAndAlarm_CallsLogEngineerRequired(
            [ValueSource(nameof(FaultyManagerStatuses))] string lightStatus,
            [ValueSource(nameof(OkManagerStatuses))] string doorStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(string.Format("{0}{1}",
                ManagerStatus.lightsWithComma, ManagerStatus.alarmWithComma));
        }

        /// <summary>
        /// Test the <see cref="BuildingController.GetStatusReport"/> method using stubs.
        /// Satisfies <strong>L4R4</strong>.
        /// </summary>
        [Test]
        public void GetStatusReport_WhenFindsFaultsDoorsAndAlarm_CallsLogEngineerRequired(
            [ValueSource(nameof(OkManagerStatuses))] string lightStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string doorStatus,
            [ValueSource(nameof(FaultyManagerStatuses))] string alarmStatus)
        {
            ILightManager lightManager = Substitute.For<ILightManager>();
            IFireAlarmManager fireAlarmManager = Substitute.For<IFireAlarmManager>();
            IDoorManager doorManager = Substitute.For<IDoorManager>();
            IWebService webService = Substitute.For<IWebService>();
            IEmailService emailService = Substitute.For<IEmailService>();
            lightManager.GetStatus().Returns(ManagerStatus.lightsWithComma + lightStatus);
            doorManager.GetStatus().Returns(ManagerStatus.doorsWithComma + doorStatus);
            fireAlarmManager.GetStatus().Returns(ManagerStatus.alarmWithComma + alarmStatus);

            BuildingController controller = new("", lightManager, fireAlarmManager, doorManager, webService, emailService);

            controller.GetStatusReport();

            webService.Received(1).LogEngineerRequired(string.Format("{0}{1}",
               ManagerStatus.doorsWithComma, ManagerStatus.alarmWithComma));
        }

    }
}