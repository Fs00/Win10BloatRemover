using System;

namespace Win10BloatRemover
{
    /**
     *  UWPAppRemover
     *  Removes the UWP which are passed into the constructor
     *  Once removal is performed, the class instance can not be reused
     */
    class UWPAppRemover
    {
        private readonly string[] appsToRemove;
        private bool removalPerformed = false;

        public UWPAppRemover(string[] appsToRemove)
        {
            this.appsToRemove = appsToRemove;
        }

        public void PerformRemoval()
        {
            if (removalPerformed)
                throw new InvalidOperationException("Apps have been already removed!");
            // TBD
        }
    }
}
