using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace AIServicesDemoApp.Pages
{
    public class DetectPIIModel(IAmazonTextract textractClient, IAmazonComprehend comprehendClient,
            IWebHostEnvironment hostEnvironment)
        : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;

        public void OnGet()
        {
        }
        
        
        public async Task OnPostDetectPIIAsync()
        {
            if (FormFile == null)
            {
                return;
            }
            // save document image to display it
            var formDocument = await Helpers.SaveImage(FormFile, hostEnvironment.WebRootPath);
            FileName = formDocument.FileName;
            
            // get all text from the document
 
            var request = new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = formDocument.MemoryStream }
            };
            var response = await textractClient.DetectDocumentTextAsync(request);
            var blocks = response.Blocks;
                
            // extract all lines from the text
            var lines = blocks.Where(block => block.BlockType.Value == "LINE")
                .Select(block => block.Text).ToList();
            var text = string.Join("\n", lines);
            
            // detect PII entities
            var detectEntitiesRequest = new DetectPiiEntitiesRequest
            {
                Text = text,
                LanguageCode = "en"
            };
            var detectEntitiesResponse = await comprehendClient.DetectPiiEntitiesAsync(detectEntitiesRequest);
            var entities = detectEntitiesResponse.Entities;
            // display the list
 
            var sb = new StringBuilder();
            foreach (var entity in entities)
            {
                if (entity.BeginOffset.HasValue && entity.EndOffset.HasValue)
                {
                    sb.AppendLine($"Score: {entity.Score}, Type: {entity.Type},");
                    sb.AppendLine($"Text: {text.Substring(entity.BeginOffset.Value, entity.EndOffset.Value - entity.BeginOffset.Value)} <br>");
                }
            }

            Result = sb.ToString();
        }
        
        public async Task OnPostDetectPIIBedrockAsync()
        {
            if (FormFile == null)
            {
                return;
            }
            // save document image to display it
            var formDocument = await Helpers.SaveImage(FormFile, hostEnvironment.WebRootPath);
            FileName = formDocument.FileName;
            
            var runtime = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

            var request = new ConverseRequest
            {
                ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
                Messages = [
                    new Message
                    {
                        Role = ConversationRole.User,
                        Content = [
                            new ContentBlock
                            {
                                Text = $"Detect all PII information, output list of found entities as JSON with value and type of PII"

                            },
                            new ContentBlock
                            {
                                Image = new ImageBlock
                                {
                                    Format = ImageFormat.Jpeg,
                                    Source = new ImageSource
                                    {
                                        Bytes = formDocument.MemoryStream
                                    }
                                }
                            }
                        ]
                    }
                ]
            };
            
            var response = await runtime.ConverseAsync(request);


            Result = response.Output.Message.Content[0].Text;
            
        }
       
    }
}
