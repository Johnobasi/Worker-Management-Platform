using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IQRCodeRepository
    {
        Task<QRCode> GenerateQRCodeAsync(Guid userId);
        Task<QRCode> GetQRCodeByIdAsync(Guid id);
        Task AssignUserToQRCodeAsync(Guid qrCodeId, Guid userId);
        Task DisableQRCodeAsync(Guid userId);
    }
}
