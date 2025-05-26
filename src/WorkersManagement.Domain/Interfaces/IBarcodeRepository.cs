using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Domain.Interfaces
{
    public interface IBarcodeRepository
    {
        Task<QRCode> GenerateAndSaveBarcodeAsync(Guid workerId);
        Task<QRCode> GetBarCodeByIdAsync(Guid id);
        Task AssignWorkerToBarCodeAsync(Guid qrCodeId, Guid workerId);
        Task DisableBarCodeAsync(Guid workerId);
        Task<QRCode> GetBarcodeByWorkerIdAsync(Guid workerId);
    }
}
