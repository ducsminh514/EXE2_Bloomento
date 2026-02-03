using System.Net.Http.Json;
using ADHDChecklist.Client.Infrastructure.Services;

namespace ADHDChecklist.Client.Features.Family.Services;

public interface IFamilyService
{
    Task<FamilyResponse?> GetFamilyAsync();
    Task<Guid> CreateFamilyAsync(string name);
    Task<string> InviteMemberAsync(string email);
    Task<Guid> JoinFamilyAsync(string inviteCode);
}

public class FamilyService : IFamilyService
{
    private readonly IApiClient _apiClient;

    public FamilyService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<FamilyResponse?> GetFamilyAsync()
    {
        try
        {
            return await _apiClient.GetAsync<FamilyResponse>("api/family");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Guid> CreateFamilyAsync(string name)
    {
        var result = await _apiClient.PostAsync<Guid>("api/family", new { Name = name });
        if (result == Guid.Empty) throw new Exception("Failed to create family");
        return result;
    }

    public async Task<string> InviteMemberAsync(string email)
    {
        var result = await _apiClient.PostAsync<InviteResponse>("api/family/invite", new { Email = email });
        return result?.Code ?? throw new Exception("Failed to invite member");
    }

    public async Task<Guid> JoinFamilyAsync(string inviteCode)
    {
        var result = await _apiClient.PostAsync<Guid>("api/family/join", new { InviteCode = inviteCode });
        if (result == Guid.Empty) throw new Exception("Failed to join family");
        return result;
    }
}


public record FamilyResponse(
    Guid Id,
    string Name,
    Guid OwnerId, 
    bool IsOwner,
    List<FamilyMemberResponse> Members,
    List<PendingInvitationResponse> PendingInvitations
);

public record PendingInvitationResponse(
    Guid Id,
    string Email,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

public record FamilyMemberResponse(
    Guid UserId, 
    string FullName, 
    string Role, 
    string? Nickname,
    string? AvatarUrl,
    string? Color
);

public record InviteResponse(string Code);
public record ErrorResponse(string Message);
