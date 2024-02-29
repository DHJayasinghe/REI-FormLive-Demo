namespace FormBuilder.Models.DTO;

public record SaveMappingRequest(MappingRequestEntry[] Mappings);

public record MappingRequestEntry(string Source, string Target);

public record SaveClientRequest(string Name, string ConnectionString);