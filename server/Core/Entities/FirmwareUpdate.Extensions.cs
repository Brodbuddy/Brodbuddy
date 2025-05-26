namespace Core.Entities;

public partial class FirmwareUpdate
{
    public static class OtaStatus
    {
        public const string Started = "started";
        public const string Downloading = "downloading";
        public const string Applying = "applying";
        public const string Complete = "complete";
        public const string Failed = "failed";
    }
    
    public bool IsActive => Status is OtaStatus.Started or OtaStatus.Downloading or OtaStatus.Applying;
    public bool IsFinished => Status is OtaStatus.Complete or OtaStatus.Failed;
}