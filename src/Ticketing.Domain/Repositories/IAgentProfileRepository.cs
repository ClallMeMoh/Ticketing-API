using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Repositories;

public interface IAgentProfileRepository
{
    Task<AgentProfile?> GetByUserIdAsync(Guid userId);
    Task<AgentProfileLoadSnapshot?> GetByUserIdWithLoadAsync(Guid userId);
    Task<List<AgentProfile>> GetAllAsync();
    Task<List<AgentProfileLoadSnapshot>> GetAllWithLoadAsync();
    Task<List<AgentLoadSnapshot>> GetAssignableAgentsWithActiveLoadAsync();
    Task AddAsync(AgentProfile profile);
    void Update(AgentProfile profile);
}
