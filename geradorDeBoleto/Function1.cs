using Azure.Messaging.ServiceBus;
using BarcodeStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnGeradorBoletos
{
    public class GeradorCodigoBarras(ILogger<GeradorCodigoBarras> logger)
    {
        private readonly ILogger<GeradorCodigoBarras> _logger = logger;
        private readonly string _serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString") 
            ?? throw new InvalidOperationException("ServiceBusConnectionString environment variable is not set.");
        private readonly string _queueName = "gerador-de-boleto-queue";

        [Function("barcode-generate")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("Received request body: {RequestBody}", requestBody);
                
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return new BadRequestObjectResult("Request body cannot be null or empty.");
                }

                dynamic? deserializedData;
                try
                {
                    deserializedData = JsonConvert.DeserializeObject(requestBody);
                    if (deserializedData == null)
                    {
                        return new BadRequestObjectResult("Invalid JSON in request body.");
                    }
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(ex, "JSON parsing error in request body: {RequestBody}", requestBody);
                    return new BadRequestObjectResult($"Invalid JSON format: {ex.Message}");
                }

                string? valor = deserializedData?.valor;
                string? dataVencimento = deserializedData?.dataVencimento;

                if (string.IsNullOrEmpty(valor) || string.IsNullOrEmpty(dataVencimento))
                {
                    return new BadRequestObjectResult("Os campos valor e dataVencimento s�o obrigat�rios");
                }

                string barcodeData;

                //Validate the format of the data
                if (!DateTime.TryParseExact(dataVencimento, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
                {
                    return new BadRequestObjectResult("Data de Vencimento inv�lida");
                }

                string dateStr = dateObj.ToString("yyyyMMdd");

                //Conv value to cents and format it to 8 digits

                if (!decimal.TryParse(valor, out decimal valorDecimal))
                {
                    return new BadRequestObjectResult("Valor inv�lido");
                }
                int valorCentavos = (int)(valorDecimal * 10);
                string valorStr = valorCentavos.ToString("D8");

                string bankCode = "273";
                string baseCode = string.Concat(bankCode, valorStr, dateStr);

                // filling the barcode with zeros to make it 44 characters long

                barcodeData = baseCode.Length < 44 ? baseCode.PadRight(44, '0') : baseCode.Substring(0, 44);
                _logger.LogInformation($"Generated barcode: {barcodeData}");

                Barcode barcode = new Barcode();
                var skImage = barcode.Encode(BarcodeStandard.Type.Code128, barcodeData);

                using (var encodeData = skImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    var imageBytes = encodeData.ToArray();

                    string base64String = Convert.ToBase64String(imageBytes);

                    var resultObject = new
                    {
                        barcode = barcodeData,
                        valorOriginal = valorDecimal,
                        DataVencimento = DateTime.Now.AddDays(5),
                        ImagemBase64 = base64String
                    };

                    await SendFileFallback(resultObject, _serviceBusConnectionString, _queueName);

                    return new OkObjectResult(resultObject);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }

        private async Task SendFileFallback(object resultObject, string serviceBusConnectionString, string queueName)
        {
            await using var client = new ServiceBusClient(serviceBusConnectionString);

            ServiceBusSender sender = client.CreateSender(queueName);

            string messageBody = JsonConvert.SerializeObject(resultObject);

            ServiceBusMessage message = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(message);

            _logger.LogInformation($"Message sent to queue: {queueName}");

        }
    }
}