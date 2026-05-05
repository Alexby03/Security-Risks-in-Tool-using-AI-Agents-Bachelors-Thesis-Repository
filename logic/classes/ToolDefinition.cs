public record ToolDefinition(string Name, string Domain, string[] Keywords, string ActionDescription);

public static class RBACConstants
{
    public static readonly List<ToolDefinition> Tools = new()
    {
        new("own_document_tool", "DOCUMENT", new[] { "OWN" }, "manage your own documents"),
        new("shared_document_tool", "DOCUMENT", new[] { "SHARED" }, "interact with shared documents"),
        new("others_document_tool", "DOCUMENT", new[] { "OTHERS", "OTHER" }, "administratively manage others' documents"),
        new("own_email_tool", "EMAIL", new[] { "OWN" }, "handle your own emails"),
        new("others_email_tool", "EMAIL", new[] { "OTHERS", "OTHER", "AS_OTHER" }, "manage emails on behalf of others"),
        new("own_managed_customer_tool", "CUSTOMER", new[] { "OWN", "MANAGED", "CREATE" }, "handle customers you manage"),
        new("others_customer_tool", "CUSTOMER", new[] { "OTHERS", "OTHER" }, "handle customers managed by others"),
        new("own_event_tool", "CALENDAR", new[] { "OWN" }, "manage your own calendar events"),
        new("participating_event_tool", "CALENDAR", new[] { "PARTICIPATING", "INVITED" }, "handle events you are invited to")
    };
}