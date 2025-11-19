using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage barcode generation and assignment for workers
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BarCodeController : ControllerBase
    {
        private readonly IBarcodeRepository _barcodeRepository;
        private readonly ILogger<BarCodeController> _logger;
        public BarCodeController(IBarcodeRepository barcodeRepository, ILogger<BarCodeController> logger)
        {
                _barcodeRepository = barcodeRepository;
                _logger = logger;
        }

        /// <summary>
        /// Generate and download barcode for a worker
        /// </summary>
        /// <param name="workerId">Worker identifier</param>
        /// <returns>Barcode image file</returns>
        [HttpGet("generate-download-barcode/{workerId}")]
         [AllowAnonymous]
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

        /// <summary>
        /// Assign a barcode to a worker
        /// </summary>
        /// <param name="qrCodeId">Barcode identifier</param>
        /// <param name="workerId">Worker identifier</param>
        /// <returns>Assignment result</returns>
        [HttpPut("assign-worker-barcode")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignUserToQRCode([FromQuery] Guid qrCodeId, [FromQuery] Guid workerId)
        {
            try
            {
                await _barcodeRepository.AssignWorkerToBarCodeAsync(qrCodeId, workerId);
                _logger.LogInformation("Assigned QR code {qrCodeId} to worker {workerId}", qrCodeId,workerId);
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

        /// <summary>
        /// Disable barcode for a worker
        /// </summary>
        /// <param name="workerId">Worker identifier</param>
        /// <returns>Disable result</returns>
        [HttpDelete("disable-worker-barcode/{workerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DisableWorkerQRCodes(Guid workerId)
        {
            try
            {
                await _barcodeRepository.DisableBarCodeAsync(workerId);
                _logger.LogInformation("Disabled QR codes for worker {workerId}", workerId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling QR codes.");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get barcode details by ID
        /// </summary>
        /// <param name="qrCodeId">Barcode identifier</param>
        /// <returns>Barcode information</returns>
        [HttpGet("get-worker-barcode/{qrCodeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid qrCodeId)
        {
            var qrCode = await _barcodeRepository.GetBarCodeByIdAsync(qrCodeId);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code {qrCodeId} not found.", qrCodeId);
                return NotFound("QR code not found.");
            }

            _logger.LogInformation("Retrieved QR code {qrCodeId}", qrCodeId);
            return Ok(qrCode);
        }

    }
}
