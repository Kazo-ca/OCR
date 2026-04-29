var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.KazoOCR_Web>("kazoocr-web");

builder.AddProject<Projects.KazoOCR_Api>("kazoocr-api");

builder.Build().Run();
