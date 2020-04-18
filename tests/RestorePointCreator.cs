using System;
using System.Management;
using Xunit;

namespace Win10BloatRemover.Tests
{
    /*
     * This is made so that every time we run the tests including a class that belongs to this collection,
     * a system restore point gets created
     */
    [CollectionDefinition("ModifiesSystemState")]
    public class TestsPrecededByRestorePoint : ICollectionFixture<RestorePointCreator> {}

    public class RestorePointCreator : IDisposable
    {
        enum EventType
        {
            BeginChange = 100,
            EndChange = 101
        }

        private const int MODIFY_SETTINGS = 12;

        public RestorePointCreator() => SetRestorePoint(EventType.BeginChange);

        public void Dispose() => SetRestorePoint(EventType.EndChange);

        private void SetRestorePoint(EventType eventType)
        {
            using var systemRestoreClass = new ManagementClass {
                Scope = new ManagementScope(@"\\localhost\root\default"),
                Path = new ManagementPath("SystemRestore"),
                Options = new ObjectGetOptions()
            };
            using ManagementBaseObject parameters = systemRestoreClass.GetMethodParameters("CreateRestorePoint");
            parameters["Description"] = "Win10BloatRemover_TestRun";
            parameters["EventType"] = (int) eventType;
            parameters["RestorePointType"] = MODIFY_SETTINGS;
            systemRestoreClass.InvokeMethod("CreateRestorePoint", parameters, null);
        }
    }
}
