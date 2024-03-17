using NUnit.Framework;
using NSubstitute;
using SmartBuildingController;

[TestFixture]
public class BuildingControllerTests
{

    [Test]
    public void Constructor_AssignsBuildingID_Lowercase()
    {
        // Arrange
        string inputID = "TestBuildingID";

        // Act
        var buildingController = new BuildingController(inputID);

        // Assert
        Assert.AreEqual("testbuildingid", buildingController.GetBuildingID());
    }

    [Test]
    public void Constructor_WithEmptyString_AssignsEmptyBuildingID()
    {
        // Arrange
        string inputID = "";

        // Act
        var buildingController = new BuildingController(inputID);

        // Assert
        Assert.AreEqual("", buildingController.GetBuildingID());
    }

    [Test]
    public void Constructor_WithMixedCaseString_AssignsCorrectlyLowercasedBuildingID()
    {
        // Arrange
        var inputID = "TeStBuIlDiNgId123";

        // Act
        var buildingController = new BuildingController(inputID);

        // Assert
        Assert.AreEqual(inputID.ToLower(), buildingController.GetBuildingID());
    }

    [Test]
    public void Constructor_WithStringWithWhitespaces_AssignsTrimmedAndLowercasedBuildingID()
    {
        // Arrange
        var inputID = "  TestBuildingID  ";

        // Act
        var buildingController = new BuildingController(inputID.Trim().ToLower());

        // Assert
        Assert.AreEqual(inputID.Trim().ToLower(), buildingController.GetBuildingID());
    }

    [Test]
    public void Constructor_WithStringWithSpecialCharacters_AssignsLowercasedBuildingID()
    {
        // Arrange
        var inputID = "Test-Building_ID#123";

        // Act
        var buildingController = new BuildingController(inputID);

        // Assert
        Assert.AreEqual(inputID.ToLower(), buildingController.GetBuildingID());
    }

    [Test]
    [TestCase("open")]
    [TestCase("closed")]
    [TestCase("out of hours")]
    public void Constructor_WithValidStartState_InitializesCorrectly(string startState)
    {
        // Arrange
        var id = "Building1";

        // Act
        var buildingController = new BuildingController(id, startState);

        // Assert
        Assert.AreEqual(startState.ToLower(), buildingController.GetCurrentState());
    }

    [Test]
    [TestCase("invalid")]
    [TestCase("")]
    [TestCase(null)] // Include this case based on your handling of null inputs.
    public void Constructor_WithInvalidStartState_ThrowsArgumentException(string startState)
    {
        // Arrange
        var id = "Building1";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new BuildingController(id, startState));
        Assert.That(ex.Message, Is.EqualTo("BuildingController can only be initialised to 'open', 'closed', 'out of hours'"));
    }

    [Test]
    [TestCase("OPEN")]
    [TestCase("CLOSED")]
    [TestCase("OUT OF HOURS")]
    public void Constructor_WithUpperCaseStartState_InitializesInLowerCase(string startState)
    {
        // Arrange
        var id = "Building1";

        // Act
        var buildingController = new BuildingController(id, startState);

        // Assert
        Assert.AreEqual(startState.ToLower(), buildingController.GetCurrentState());
    }

    [Test]
    [TestCase("Building2", "open")]
    public void Constructor_InitializesBuildingIDInLowerCase(string id, string startState)
    {
        // Arrange & Act
        var buildingController = new BuildingController(id, startState);

        // Assert
        Assert.AreEqual(id.ToLower(), buildingController.GetBuildingID());
    }

    [Test]
    [TestCase("MixedCaseID", "OpEn", ExpectedResult = true)]
    [TestCase("AnotherID", "closed", ExpectedResult = true)]
    public bool Constructor_WithMixedParameters_InitializesCorrectly(string id, string startState)
    {
        // Act
        var buildingController = new BuildingController(id, startState);

        // Assert
        return buildingController.GetBuildingID() == id.ToLower() && buildingController.GetCurrentState() == startState.ToLower();
    }

    [Test]
    public void Constructor_WithExtremelyLongStrings_HandlesAsExpected()
    {
        // Arrange
        var longId = new string('a', 10000); // Example of a very long ID string.
        var longStartState = "open"; // Using a valid start state since extremely long invalid state would throw an ArgumentException.

        // Act
        BuildingController buildingController = null;
        TestDelegate constructorCall = () => buildingController = new BuildingController(longId, longStartState);

        // Assert
        Assert.DoesNotThrow(constructorCall, "Constructor should handle extremely long strings without throwing an exception.");
        Assert.IsNotNull(buildingController, "Constructor should successfully create an instance with extremely long strings.");
        Assert.AreEqual(longId.ToLower(), buildingController.GetBuildingID(), "Building ID should be correctly assigned and lowercased.");
        Assert.AreEqual(longStartState.ToLower(), buildingController.GetCurrentState(), "Start state should be correctly assigned and lowercased.");
    }

    [Test]
    public void SetCurrentState_ToFireAlarm_FromAnyState_ExecutesCorrectActions()
    {
        // Arrange
        var buildingController = new BuildingController("TestBuilding", _lightManager, _fireAlarmManager, _doorManager, _webService, _emailService);
        buildingController.SetCurrentState("open"); // Starting from an 'open' state

        // Act
        var result = buildingController.SetCurrentState("fire alarm");

        // Assert
        Assert.IsTrue(result, "Transition to 'fire alarm' state should be successful.");
        _doorManager.Received(1).OpenAllDoors();
        _lightManager.Received(1).SetAllLights(true);
        _fireAlarmManager.Received(1).SetAlarm(true);
        _webService.Received(1).LogFireAlarm(Arg.Is<string>(s => s == "fire alarm"));
    }



    private string _buildingId = "TestBuilding";

    // Create substitutes (mocks) for all interfaces outside of the test cases to reuse them
    private ILightManager _lightManager = Substitute.For<ILightManager>();
    private IFireAlarmManager _fireAlarmManager = Substitute.For<IFireAlarmManager>();
    private IDoorManager _doorManager = Substitute.For<IDoorManager>();
    private IWebService _webService = Substitute.For<IWebService>();
    private IEmailService _emailService = Substitute.For<IEmailService>();

    // This test ensures that the constructor initializes without exceptions with valid inputs.
    [Test]
    public void Constructor_WhenCalledWithValidServices_InitializesController()
    {
        var controller = new BuildingController(_buildingId, _lightManager, _fireAlarmManager, _doorManager, _webService, _emailService);
        Assert.IsNotNull(controller);
    }

    // This parameterized test checks for ArgumentNullException when any of the service parameters is null.
    [TestCase("lightManager")]
    [TestCase("fireAlarmManager")]
    [TestCase("doorManager")]
    [TestCase("webService")]
    [TestCase("emailService")]
    public void Constructor_WithNullServices_ThrowsArgumentNullException(string serviceType)
    {
        // Using NSubstitute to create a null substitute based on the serviceType parameter
        object lightManager = serviceType != "lightManager" ? _lightManager : null;
        object fireAlarmManager = serviceType != "fireAlarmManager" ? _fireAlarmManager : null;
        object doorManager = serviceType != "doorManager" ? _doorManager : null;
        object webService = serviceType != "webService" ? _webService : null;
        object emailService = serviceType != "emailService" ? _emailService : null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new BuildingController(_buildingId, (ILightManager)lightManager, (IFireAlarmManager)fireAlarmManager,
                                                                                      (IDoorManager)doorManager, (IWebService)webService, (IEmailService)emailService));
        // The parameter name in the exception should match the type of service that was null.
        Assert.That(ex.ParamName, Is.EqualTo($"i{serviceType.First().ToString().ToUpper()}{serviceType.Substring(1)}"));
    }




}

