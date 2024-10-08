using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class GcCommandTests
{
    [TestMethod]
    public void GcCommand_ValidInputs_PropertiesAreValid()
    {
        // Act
        var gcCommand = new GcCommand(name: "TestCommand",
                                      category: "Test",
                                      new Action(() => { }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Assert
        Assert.IsNotNull(gcCommand);
        Assert.AreEqual(gcCommand.Name, "TestCommand");
        Assert.AreEqual(gcCommand.Category, "Test");
        Assert.AreEqual(gcCommand.Type, GcParameterType.Command);
        Assert.IsTrue(gcCommand.IsReadable);
        Assert.IsTrue(gcCommand.IsWritable);
        Assert.IsTrue(gcCommand.IsImplemented);
        Assert.AreEqual(gcCommand.Visibility, GcVisibility.Beginner);
        Assert.IsFalse(gcCommand.IsDone());
    }

    [TestMethod]
    public void GcCommand_InvalidName_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.ThrowsException<ArgumentException>(() => new GcCommand(name: "Name containing white spaces", category: "Test", method: () => { }));
    }

    [TestMethod]
    public void GcCommand_NonImplemented_IsImplementedIsFalse()
    {
        // Act
        var gcCommand = new GcCommand("NonimplementedParameter");

        // Assert
        Assert.IsTrue(gcCommand.Name == "NonimplementedParameter");
        Assert.IsFalse(gcCommand.IsImplemented);
    }

    [TestMethod]
    public void Execute_IsImplemented_ActionWasInvoked()
    {
        // Arrange
        bool actionWasInvoked = false;
        var gcCommand = new GcCommand(name: "TestCommand",
                                      category: "Test",
                                      new Action(() => { actionWasInvoked = true; }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        gcCommand.Execute();

        // Assert
        Assert.IsTrue(actionWasInvoked);
    }

    [TestMethod]
    public void Execute_NonImplemented_ThrowsInvalidOperationException()
    {
        // Act
        var gcCommand = new GcCommand("NonimplementedParameter");

        // Assert
        Assert.ThrowsException<InvalidOperationException>(gcCommand.Execute);
    }

    [TestMethod]
    public async Task IsDone_FalseWhileExecuting_TrueWhenFinished()
    {
        // Arrange
        bool actionWasInvoked = false;
        var gcCommand = new GcCommand(name: "TestCommand",
                                      category: "Test",
                                      new Action(() => { Task.Delay(100).Wait(); actionWasInvoked = true; }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        Task task = Task.Run(() => gcCommand.Execute());

        // Assert
        Assert.IsFalse(gcCommand.IsDone());
        Assert.IsFalse(actionWasInvoked);

        await task;

        // Assert
        Assert.IsTrue(gcCommand.IsDone());
        Assert.IsTrue(actionWasInvoked);
    }

    [TestMethod]
    public void ToString_IsImplemented_ReturnsName()
    {
        // Arrange
        var expectedString = "TestCommand";
        var gcCommand = new GcCommand(name: expectedString,
                                      category: "Test",
                                      new Action(() => { throw new InvalidOperationException(); }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        var actualString = gcCommand.ToString();

        // Assert
        Assert.AreEqual(expectedString, actualString);
    }

    [TestMethod]
    public void ToString_IsNotImplemented_ReturnsNotImplementedString()
    {
        // Arrange
        var gcCommand = new GcCommand(name: "TestCommand");
        var expectedString = $"{gcCommand.Name} is not implemented!";

        // Act
        var actualString = gcCommand.ToString();

        // Assert
        Assert.AreEqual(expectedString, actualString);
    }

    [TestMethod]
    public void FromString_ReturnsVoid()
    {
        // Arrange
        var gcCommand = new GcCommand(name: "TestCommand",
                                      category: "Test",
                                      new Action(() => { }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        gcCommand.FromString("HelloWorld");
    }

    [TestMethod]
    public void DeepCopy_AreEqualButNotSame()
    {
        // Arrange
        var gcCommand = new GcCommand(name: "TestCommand",
                                      category: "Test",
                                      new Action(() => { }),
                                      isReadable: true,
                                      isWritable: true,
                                      visibility: GcVisibility.Beginner,
                                      description: "This is a unit test parameter.");

        // Act
        var copyCommand = gcCommand.Copy();

        // Assert
        Assert.AreNotSame(gcCommand, copyCommand);
        Assert.AreEqual(gcCommand.Name, copyCommand.Name);
        Assert.AreEqual(gcCommand.DisplayName, copyCommand.DisplayName);
        Assert.AreEqual(gcCommand.Category, copyCommand.Category);
        Assert.AreEqual(gcCommand.Type, copyCommand.Type);
        Assert.AreEqual(gcCommand.IsReadable, copyCommand.IsReadable);
        Assert.AreEqual(gcCommand.IsWritable, copyCommand.IsWritable);
        Assert.AreEqual(gcCommand.IsImplemented, copyCommand.IsImplemented);
        Assert.AreEqual(gcCommand.Visibility, copyCommand.Visibility);
        Assert.AreEqual(gcCommand.Description, copyCommand.Description);
        Assert.IsTrue(gcCommand.SelectingParameters.SequenceEqual(copyCommand.SelectingParameters));
        Assert.IsTrue(gcCommand.SelectedParameters.SequenceEqual(copyCommand.SelectedParameters));
    }
}
