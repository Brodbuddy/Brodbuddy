using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Api.Http.Extensions;


public class MakeAllPropertiesRequiredProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        foreach (var schema in context.Document.Definitions.Values)
        {
            foreach (var property in schema.Properties)
            {
                schema.RequiredProperties.Add(property.Key);
            }
        }
    } 
}