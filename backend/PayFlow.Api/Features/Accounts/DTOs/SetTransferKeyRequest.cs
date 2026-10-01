using PayFlow.Domain.Entities;
namespace PayFlow.Features.Accounts.DTOs;
public record SetTransferKeyRequest(TransferKeyType Type, string Value);
