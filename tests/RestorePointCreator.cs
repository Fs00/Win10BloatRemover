using Xunit;

namespace Win10BloatRemover.Tests
{
    /*
     * This is made so that every time we run the tests including a class that belongs to this collection,
     * a system restore point gets created
     */
    [CollectionDefinition("ModifiesSystemState")]
    public class TestsPrecededByRestorePoint : ICollectionFixture<RestorePointCreator> {}

    public class RestorePointCreator
    {
        public RestorePointCreator()
        {
            // TODO: create a system restore point
        }
    }
}
