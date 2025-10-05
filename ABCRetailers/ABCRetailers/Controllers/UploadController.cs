using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storage;

        public UploadController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model, IFormFile? proofOfPayment)
        {
            if (!ModelState.IsValid) return View(model);

            if (proofOfPayment == null || proofOfPayment.Length == 0)
            {
                ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                return View(model);
            }

            try
            {
                // Upload to Azure File Share in the payments directory
                var fileName = await _storage.UploadToFileShareAsync(proofOfPayment, "contracts", "payments");

                // Send notification to queue
                var message = $"Payment proof uploaded: {fileName} for Order: {model.OrderId ?? "N/A"}, Customer: {model.CustomerName ?? "N/A"}";
                await _storage.SendMessageAsync("order-notifications", message);

                TempData["Message"] = $"File '{fileName}' uploaded successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Upload failed: {ex.Message}");
                return View(model);
            }
        }
    }
}