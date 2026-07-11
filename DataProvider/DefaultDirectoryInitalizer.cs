namespace DataProvider
{
    public class DefaultDirectoryInitalizer : IDirectoryInitalizer
    {
        public void Initialize(string baseFolder)
        {
            var aidFolder = Path.Combine(baseFolder, Constants.BIN_NAZOV_PODADRESARA_AID);
            if (!Directory.Exists(aidFolder))
                Directory.CreateDirectory(aidFolder);

            var lckFolder = Path.Combine(baseFolder, Constants.BIN_NAZOV_PODADRESARA_LCK);
            if (!Directory.Exists(lckFolder))
                Directory.CreateDirectory(lckFolder);

            var usrFolder = Path.Combine(baseFolder, Constants.BIN_NAZOV_PODADRESARA_USR);
            if (!Directory.Exists(usrFolder))
                Directory.CreateDirectory(usrFolder);
        }
    }

}
