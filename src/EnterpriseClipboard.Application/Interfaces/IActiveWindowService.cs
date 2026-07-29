namespace EnterpriseClipboard.Application.Interfaces;

public interface IActiveWindowService
{
    ActiveWindowDetails GetActiveWindowDetails();
}

public class ActiveWindowDetails
{
    public string ExecutableName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClass { get; set; } = string.Empty;
}
