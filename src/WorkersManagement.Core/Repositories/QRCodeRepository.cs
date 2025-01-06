using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Text.Json;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class QRCodeRepository : IQRCodeRepository
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<QRCodeRepository> _logger;
        public QRCodeRepository(WorkerDbContext workerDbContext, ILogger<QRCodeRepository> logger)
        {
           _context = workerDbContext;
            _logger = logger;
        }
        public async Task AssignUserToQRCodeAsync(Guid qrCodeId, Guid userId)
        {
            _logger.LogInformation($"Assigning user {userId} to QR Code {qrCodeId}");
            try
            {
                var qrCode = await GetQRCodeByIdAsync(qrCodeId);
                if (qrCode == null)
                    throw new ArgumentException("QR Code not found");

                qrCode.UserId = userId;
                _context.QRCodes.Update(qrCode);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
               _logger.LogError(ex.Message);
            }

        }

        public async Task DisableQRCodeAsync(Guid userId)
        {

            _logger.LogInformation($"Disabling QR Code for user {userId}");
            try
            {
                var qrCode = await GetQRCodeByIdAsync(userId);
                if (qrCode == null)
                    throw new ArgumentException("QR Code not found for the user");

                qrCode.IsDisabled = true;
                _context.QRCodes.Update(qrCode);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task<QRCode> GenerateQRCodeAsync(Guid userId)
        {

            _logger.LogInformation($"Generating QR Code for user {userId}");
            try
            {
                var user = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    throw new Exception($"User with ID {userId} not found");

                var existingQRCode = await GetQRCodeByIdAsync(userId);

                if (existingQRCode != null)
                    return existingQRCode;

                var qrData = GenerateQRCodeData(user);
                var qrCodeBase64 = GenerateQRCodeImage(qrData);

                var qrCodeEntity = new QRCode
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QRCodeData = qrCodeBase64,
                    CreatedAt = DateTime.UtcNow,
                    IsDisabled = false
                };

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.QRCodes.Add(qrCodeEntity);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"QR Code generated successfully for user {userId}");
                    return qrCodeEntity;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Failed to generate QR Code for user {userId}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }

        }
        private string GenerateQRCodeData(User user)
        {
            var data = new
            {
                UserId = user.Id,
                WorkerId = user?.Id,
                DepartmentId = user?.DepartmentId,
                Timestamp = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(data);
        }

        private string GenerateQRCodeImage(string data)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new BitmapByteQRCode(qrCodeData);
                var bitmapBytes = qrCode.GetGraphic(20);

                return Convert.ToBase64String(bitmapBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }

        public async Task<QRCode?> GetQRCodeByIdAsync(Guid id)
        {
            _logger.LogInformation($"Getting QR Code with ID {id}");
            try
            {
                return await _context.QRCodes
                    .FirstOrDefaultAsync(q => q.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }

        }
    }
}
