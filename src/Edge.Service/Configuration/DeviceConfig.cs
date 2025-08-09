namespace Edge.Service.Configuration;

public class DeviceConfig
{
    public const string SectionName = "DeviceConfig";

    public string MacAddress { get; set; } = string.Empty;
}