using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BarCodeController : ControllerBase
    {
        private readonly IBarcodeRepository _barcodeRepository;
        private readonly ILogger<BarCodeController> _logger;
        public BarCodeController(IBarcodeRepository barcodeRepository, ILogger<BarCodeController> logger)
        {
                _barcodeRepository = barcodeRepository;
                _logger = logger;
        }

        [HttpGet("generate-download-barcode/{workerId}")]
        public async Task<IActionResult> DownloadWorkerBarcode(Guid workerId)
        {
            try
            {
                var qrCode = await _barcodeRepository.GenerateAndSaveBarcodeAsync(workerId);

                var fileName = $"barcode_{workerId.ToString().Substring(0, 6)}.png";
                return File(qrCode.QRCodeImage, "image/png", fileName); // triggers download
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("assign-worker-barcode")]
        public async Task<IActionResult> AssignUserToQRCode([FromQuery] Guid qrCodeId, [FromQuery] Guid workerId)
        {
            try
            {
                await _barcodeRepository.AssignWorkerToBarCodeAsync(qrCodeId, workerId);
                _logger.LogInformation($"Assigned QR code {qrCodeId} to worker {workerId}");
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Assignment failed.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning QR code.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("disable-worker-barcode/{workerId}")]
        public async Task<IActionResult> DisableWorkerQRCodes(Guid workerId)
        {
            try
            {
                await _barcodeRepository.DisableBarCodeAsync(workerId);
                _logger.LogInformation($"Disabled QR codes for worker {workerId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling QR codes.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("get-worker-barcode/{qrCodeId}")]
        public async Task<IActionResult> GetById(Guid qrCodeId)
        {
            var qrCode = await _barcodeRepository.GetBarCodeByIdAsync(qrCodeId);
            if (qrCode == null)
            {
                _logger.LogWarning($"QR code {qrCodeId} not found.");
                return NotFound("QR code not found.");
            }

            _logger.LogInformation($"Retrieved QR code {qrCodeId}");
            return Ok(qrCode);
        }

    }
}
