namespace FormBuilder.Models.DTO;

public record SaveMappingRequest(string Code, MappingRequestEntry[] Mappings);

public record MappingRequestEntry(string Source, string Target);

public record SaveClientRequest(string Name, string ConnectionString);

public record CreateFormRequest(int Id, string Code, string Name, Dictionary<string, object> Parameters);