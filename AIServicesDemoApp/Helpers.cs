﻿using Amazon.Runtime.Internal;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace AIServicesDemoApp;

public class FormDocument
{
    public string FileName { get; set; } = "";
    public string FileNameWithPath { get; set; } = "";
    public MemoryStream MemoryStream { get; } = new();
}

public static class Helpers
{
    public static string AsBase64String(this MemoryStream memoryStream)
    {
        return Convert.ToBase64String(memoryStream.ToArray());
    }
    public static async Task<FormDocument> SaveImage(IFormFile formFile, string webRootPath)
    {
        var result = new FormDocument();

        result.FileName = $"{Guid.NewGuid().ToString()}{System.IO.Path.GetExtension(formFile.FileName)}";
        result.FileNameWithPath = System.IO.Path.Combine(webRootPath, "uploads", result.FileName);

        await using (var stream = new FileStream(result.FileNameWithPath , FileMode.Create))
        {
            await formFile.CopyToAsync(stream);
        }
        
        await formFile.CopyToAsync(result.MemoryStream);

        return result;
    }
    public static void DrawRectangleUsingBoundingBox(this Image image,
        Amazon.Rekognition.Model.BoundingBox boundingBox)
    {
        if (boundingBox is { Left: not null, Top: not null, Width: not null, Height: not null })
        {
            // Draw the rectangle using the bounding box values
            // They are percentages so scale them to picture
            image.Mutate(x => x.DrawLine(
                Rgba32.ParseHex("FF0000"),
                15,
                new PointF[]
                {
                    new PointF(image.Width * boundingBox.Left.Value, image.Height * boundingBox.Top.Value),
                    new PointF(image.Width * (boundingBox.Left.Value + boundingBox.Width.Value),
                        image.Height * boundingBox.Top.Value),
                    new PointF(image.Width * (boundingBox.Left.Value + boundingBox.Width.Value),
                        image.Height * (boundingBox.Top.Value + boundingBox.Height.Value)),
                    new PointF(image.Width * boundingBox.Left.Value,
                        image.Height * (boundingBox.Top.Value + boundingBox.Height.Value)),
                    new PointF(image.Width * boundingBox.Left.Value, image.Height * boundingBox.Top.Value),
                }
            ));
        }
    }

    public static void DrawRectangleUsingBoundingBox(this Image image,
        Amazon.Textract.Model.BoundingBox boundingBox)
    {
        if (boundingBox is { Left: not null, Top: not null, Width: not null, Height: not null })
        {
            // Draw the rectangle using the bounding box values
            // They are percentages so scale them to picture
            image.Mutate(x => x.DrawLine(
                Rgba32.ParseHex("FF0000"),
                15,
                new PointF[]
                {
                    new PointF(image.Width * boundingBox.Left.Value, image.Height * boundingBox.Top.Value),
                    new PointF(image.Width * (boundingBox.Left.Value + boundingBox.Width.Value),
                        image.Height * boundingBox.Top.Value),
                    new PointF(image.Width * (boundingBox.Left.Value + boundingBox.Width.Value),
                        image.Height * (boundingBox.Top.Value + boundingBox.Height.Value)),
                    new PointF(image.Width * boundingBox.Left.Value,
                        image.Height * (boundingBox.Top.Value + boundingBox.Height.Value)),
                    new PointF(image.Width * boundingBox.Left.Value, image.Height * boundingBox.Top.Value),
                }
            ));
        }
    }
}

public class MyBedrockRequest
{
    public List<MyBedrockMessage> messages { get; set; } = new();
    public int max_tokens { get; set; } = 500;
    public double temperature { get; set; } = 1;
    public double top_p { get; set; } = 0.999;
    public double top_k { get; set; }
    public string anthropic_version { get; set; } = "bedrock-2023-05-31";
    public List<string> stop_sequences { get; set; } = ["Human:"];
}
public class MyBedrockMessage
{
    public string role { get; set; } = "user";
    public List<MyBedrockContent> content { get; set; } = new();
}
public class MyBedrockContent
{
    public string type { get; set; } = "text";
    public string text { get; set; } = "";
}

public class MyBedrockResponse
{
    public string id { get; set; } = "";
    public string type { get; set; } = "";
    public string role { get; set; } = "";
    public List<MyBedrockContent> content { get; set; } = new();
}
