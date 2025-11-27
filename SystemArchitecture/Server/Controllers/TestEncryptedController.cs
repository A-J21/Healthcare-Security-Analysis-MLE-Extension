using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Research.SEAL;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

using CDTS_PROJECT.Models;
using CDTS_PROJECT.Services;
using CDTS_PROJECT.Logics;
using CDTS_PROJECT.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace CDTS_PROJECT.Api.Controllers
{
    [Route("api/test/[controller]")]
    [ApiController]
    public class TestEncryptedController : ControllerBase
    {
        private readonly ILogger<TestEncryptedController> _logger;
        private readonly IencryptedOperationsService _encryptedOperationsService;
        private readonly IContextManager _contextManager;

        public TestEncryptedController(ILogger<TestEncryptedController> logger, IencryptedOperationsService encryptedOperationsService, IContextManager contextManager)
        {
            _logger = logger;
            _encryptedOperationsService = encryptedOperationsService;
            _contextManager = contextManager;
        }

        // POST /api/test/testencrypted
        // Accepts the same multipart form as EncryptedMLController and returns encrypted weighted sums
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Post()
        {
            // Parse multipart
            MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = EncryptedMLHelper.GetBoundary(contentType);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            // Read encrypted data part
            var section = await reader.ReadNextSectionAsync();
            if (section == null)
            {
                throw new HttpResponseException { Status = 400, Value = "0 content parts received, expected 2" };
            }
            MemoryStream encryptedStream = new MemoryStream();
            await section.Body.CopyToAsync(encryptedStream);
            encryptedStream.Seek(0, SeekOrigin.Begin);

            // Read sizes part
            section = await reader.ReadNextSectionAsync();
            if (section == null)
            {
                throw new HttpResponseException { Status = 400, Value = "Missing columnSizes part" };
            }
            MemoryStream sizesStream = new MemoryStream();
            await section.Body.CopyToAsync(sizesStream);
            sizesStream.Seek(0, SeekOrigin.Begin);
            long[] columnSizes;
            try
            {
                // Sizes are sent as a UTF-8 comma-separated string
                sizesStream.Seek(0, SeekOrigin.Begin);
                var sizesBytes = sizesStream.ToArray();
                var sizesStr = System.Text.Encoding.UTF8.GetString(sizesBytes);
                var partsSizes = sizesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                columnSizes = Array.ConvertAll(partsSizes, s => long.Parse(s));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException { Status = 400, Value = "Error parsing column sizes: " + ex.ToString() };
            }
            sizesStream.Close();

            // Reconstruct encrypted feature values
            List<List<Ciphertext>> encryptedFeatureValues;
            try
            {
                encryptedFeatureValues = await EncryptedMLHelper.extract2DCipherListFromStreamAsync(encryptedStream, columnSizes, _contextManager.Context);
            }
            catch (Exception ex)
            {
                throw new HttpResponseException { Status = 400, Value = "Error extracting encrypted features: " + ex.ToString() };
            }
            encryptedStream.Close();

            // Load model from ModelTraining/coefs.csv (fallback for testing without MongoDB)
            string coefsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ModelTraining", "coefs.csv"));
            if (!System.IO.File.Exists(coefsPath))
            {
                throw new HttpResponseException { Status = 500, Value = "coefs.csv not found at: " + coefsPath };
            }

            string line = System.IO.File.ReadAllText(coefsPath).Trim();
            string[] parts = line.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            double[] weights = Array.ConvertAll(parts, s => double.Parse(s));

            Model model = new Model
            {
                Type = "FinancialFraud",
                Precision = 1000,
                M_classes = 1,
                N_weights = weights.Length,
                Weights = new List<double[]> { weights }
            };

            // Build Query
            Query query = new Query { encryptedFeatureValues = encryptedFeatureValues };

            // Compute encrypted weighted sums
            List<List<Ciphertext>> encryptedWeightedSums;
            try
            {
                encryptedWeightedSums = await _encryptedOperationsService.calculateWeightedSumAsync(model, query);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // Serialize results back to response stream
            MemoryStream outStream = new MemoryStream();
            long[] sizes = await EncryptedMLHelper.convert2DCipherListToStreamAsync(encryptedWeightedSums, outStream);
            UInt64 totalSize = (UInt64)outStream.Length;
            // Append sizes array as UTF-8 comma-separated string
            string sizesStrOut = string.Join(",", sizes);
            byte[] sizesOutBytes = System.Text.Encoding.UTF8.GetBytes(sizesStrOut);
            outStream.Write(sizesOutBytes, 0, sizesOutBytes.Length);
            // Append total size as 8 bytes
            BinaryWriter writer = new BinaryWriter(outStream);
            writer.Seek(0, SeekOrigin.End);
            writer.Write(totalSize);
            outStream.Seek(0, SeekOrigin.Begin);

            return File(outStream, "application/octet-stream");
        }
    }
}
