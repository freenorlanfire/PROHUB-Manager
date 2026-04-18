namespace PROHUB.API.Server.Dtos;

public record TagDto(Guid Id, Guid ProjectId, string Tag);

public record AddTagRequest(string Tag);
