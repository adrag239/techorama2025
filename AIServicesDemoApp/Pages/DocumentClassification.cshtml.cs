using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace AIServicesDemoApp.Pages
{
    public class DocumentClassificationModel(IAmazonComprehend comprehendClient, IAmazonTextract textractClient,
            IWebHostEnvironment hostEnvironment)
        : PageModel
    {
        [BindProperty]
        public IFormFile? FormFile { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;


        private const string DOCUMENT_CLASSIFIER_ENDPOINT_NAME = "ReInvent-IDP-Insurance-Demo-Endpoint";

        public void OnGet()
        {
        }
        

        public async Task OnPostDetectDocumentTypeAsync()
        {
            if (FormFile == null)
                return;
            
            var client = new AmazonSecurityTokenServiceClient();
            var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest()); 
            var account = response.Account;
            
            // save document image to display it
            var formDocument = await Helpers.SaveImage(FormFile, hostEnvironment.WebRootPath);
            FileName = formDocument.FileName;
            
            var classifyDocumentRequest = new ClassifyDocumentRequest
            {
                Bytes = formDocument.MemoryStream,
                EndpointArn = $"arn:aws:comprehend:eu-west-1:{account}:document-classifier-endpoint/{DOCUMENT_CLASSIFIER_ENDPOINT_NAME}",
                DocumentReaderConfig = new DocumentReaderConfig
                {
                    DocumentReadAction = DocumentReadAction.TEXTRACT_DETECT_DOCUMENT_TEXT,
                    DocumentReadMode = DocumentReadMode.FORCE_DOCUMENT_READ_ACTION
                }
            };

            var classifyDocumentResponse = await comprehendClient.ClassifyDocumentAsync(classifyDocumentRequest);

            var stringBuilder = new StringBuilder();
            
            foreach (var documentClass in classifyDocumentResponse.Classes)
            {
                stringBuilder.AppendFormat(
                    "Type: <b>{0}</b>, Confidence score: <b>{1}</b><br>",
                    documentClass.Name,
                    documentClass.Score);
            }

            Result = stringBuilder.ToString();
        }
        
        public async Task OnPostDetectDocumentTypeBedrockAsync()
        {
            if (FormFile == null)
                return;
            
            // save document image to display it
            var formDocument = await Helpers.SaveImage(FormFile, hostEnvironment.WebRootPath);
            FileName = formDocument.FileName;
            
            var runtime = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);

            var prompt = @"
     Classify the document into the following classes
     <classes>
         CMS1500
         LICENSE
         PASSPORT
         INSURANCE_ID
         INVOICE_RECEIPT
         MEDICAL_TRANSCRIPTION
     </classes>
     
     return only the CLASS_NAME with no preamble or explanation.";
            
            var request = new ConverseRequest
            {
                ModelId = "anthropic.claude-3-5-sonnet-20240620-v1:0",
                Messages = [
                    new Message
                    {
                        Role = ConversationRole.User,
                        Content = [
                            new ContentBlock
                            {
                                Text = prompt
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
