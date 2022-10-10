using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OvOSheets.Models.Api;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SharedStatus
{
    [EnumMember(Value = "Manually Shared")]
    ManuallyShared,
    [EnumMember(Value = "Not Ready")] NotReady
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ReservationType
{
    [EnumMember(Value = "Basic Jaeger Accounts")]
    BasicJaegerAccounts,

    [EnumMember(Value = "Observer Accounts")]
    ObserverAccounts
}

public class CreateSheet
{
    [JsonPropertyName("requestDatetime")] public DateTime Start { get; set; }
    [JsonPropertyName("repEmails")] public string[] Emails { get; set; }
    [JsonPropertyName("sharedStatus")] public SharedStatus SharedStatus { get; set; }
    [JsonPropertyName("reservationType")] public ReservationType ReservationType { get; set; }

    [JsonPropertyName("groupName")] public string GroupName { get; set; }
}