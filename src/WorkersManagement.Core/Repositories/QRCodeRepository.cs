using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;
using ZXing;
using ZXing.Common;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;

namespace WorkersManagement.Core.Repositories
{
    public class QRCodeRepository : IBarcodeRepository
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<QRCodeRepository> _logger;

        public QRCodeRepository(WorkerDbContext context, ILogger<QRCodeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QRCode> GenerateAndSaveBarcodeAsync(Guid workerId)
        {
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.Id == workerId);
            if (worker == null)
                throw new ArgumentException("Worker not found.");

            string firstName = worker.FirstName?.Trim().Replace("\u00A0", " ").Replace("\u2002", " ") ?? "";
            string lastName = worker.LastName?.Trim().Replace("\u00A0", " ").Replace("\u2002", " ") ?? "";
            string fullName = $"{firstName} {lastName}".Trim();

            string workerNumber = worker.WorkerNumber?.Trim() ?? "";
            string content = $"{workerNumber} {fullName}".Trim();
            string labelText = content;

            // Generate pixel data using ZXing
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 600,
                    Height = 150,
                    Margin = 10,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content);

            // Create barcode image
            using var barcodeBitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
            var bitmapData = barcodeBitmap.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            barcodeBitmap.UnlockBits(bitmapData);

            // Combine barcode and label
            int padding = 20;
            int totalHeight = barcodeBitmap.Height + padding + 40;

            using var finalImage = new Bitmap(barcodeBitmap.Width, totalHeight);
            using var graphics = Graphics.FromImage(finalImage);
            graphics.Clear(Color.White);
            graphics.DrawImage(barcodeBitmap, new Point(0, 0));

            using var font = new Font("Arial", 18, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            var textSize = graphics.MeasureString(labelText, font);
            float textX = (finalImage.Width - textSize.Width) / 2;
            graphics.DrawString(labelText, font, brush, textX, barcodeBitmap.Height + padding);

            // Save image to byte array
            using var ms = new MemoryStream();
            finalImage.Save(ms, ImageFormat.Png);
            var barcodeBytes = ms.ToArray();

            // Check if a QR code already exists for this worker
            var existingQrCode = await _context.QRCodes.FirstOrDefaultAsync(q => q.WorkerId == workerId);

            if (existingQrCode != null)
            {
                // Update existing barcode image and metadata
                existingQrCode.QRCodeImage = barcodeBytes;
                existingQrCode.CreatedAt = DateTime.UtcNow;
                existingQrCode.IsActive = true;

                _context.QRCodes.Update(existingQrCode);
                await _context.SaveChangesAsync();
                return existingQrCode;
            }
            else
            {
                // Insert new QR code
                var qrCode = new QRCode
                {
                    Id = Guid.NewGuid(),
                    WorkerId = workerId,
                    QRCodeImage = barcodeBytes,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.QRCodes.Add(qrCode);
                await _context.SaveChangesAsync();
                return qrCode;
            }
        }


        public async Task<QRCode?> GetBarCodeByIdAsync(Guid id)
        {
            return await _context.QRCodes
                .Include(q => q.WorkerId)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);
        }

        public async Task<QRCode?> GetBarcodeByWorkerIdAsync(Guid workerId)
        {
            return await _context.QRCodes
                .Where(q => q.WorkerId == workerId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AssignWorkerToBarCodeAsync(Guid qrCodeId, Guid workerId)
        {
            _logger.LogInformation($"Assigning QRCode {qrCodeId} to worker {workerId}");

            var qrCode = await _context.QRCodes.FirstOrDefaultAsync(q => q.Id == qrCodeId);
            if (qrCode == null)
                throw new ArgumentException("QR Code not found");

            qrCode.WorkerId = workerId;
            _context.QRCodes.Update(qrCode);
            await _context.SaveChangesAsync();
        }

        public async Task DisableBarCodeAsync(Guid workerId)
        {
            var qrCodes = await _context.QRCodes
                .Where(q => q.WorkerId == workerId && q.IsActive)
                .ToListAsync();

            foreach (var qrCode in qrCodes)
            {
                qrCode.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }
    }
}
