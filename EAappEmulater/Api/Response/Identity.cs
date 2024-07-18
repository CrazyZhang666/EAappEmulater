namespace EAappEmulater.Api;

public class Identity
{
    public Personas personas { get; set; }
}

public class Personas
{
    public List<PersonaItem> persona { get; set; }
}

public class PersonaItem
{
    public long personaId { get; set; }
    public long pidId { get; set; }
    public string displayName { get; set; }
    public string name { get; set; }
    public string namespaceName { get; set; }
    public bool isVisible { get; set; }
    public string status { get; set; }
    public string statusReasonCode { get; set; }
    public string showPersona { get; set; }
    public string dateCreated { get; set; }
    public string lastAuthenticated { get; set; }
}
