namespace Core.Contracts.Push
{
    public class PushDeviceRegisterDto
    {
        public string ExpoPushToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
    }
}
