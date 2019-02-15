using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class DataProtectionSettings
    {
        [AzureBlobCheck]
        public string BlobStorageConnString { get; set; }
        public string ContainerName { get; set; }
        public string AppName { get; set; }
        public string RelativePath { get; set; }
    }
}
