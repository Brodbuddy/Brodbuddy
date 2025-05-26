using Application.Models.DTOs;

namespace Application.Interfaces.Communication.Publishers;

public interface IOtaPublisher
{
    Task PublishStartOtaAsync(OtaStartCommand command);
    Task PublishOtaChunkAsync(OtaChunkCommand command);
}