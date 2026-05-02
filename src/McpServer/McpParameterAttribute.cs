using System.ComponentModel;

namespace McpServer;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class McpParameterAttribute : Attribute
{
    public string Description { get; }
    public bool Required { get; set; }

    public McpParameterAttribute(string description)
    {
        Description = description;
    }
}
