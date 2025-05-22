using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Api.Http.Extensions;


public class MakeAllPropertiesRequiredProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        foreach (var schema in context.Document.Definitions.Values)
        {
            var requiredProperties = schema.Properties
                .Where(property => !property.Key.Equals("nickname", StringComparison.OrdinalIgnoreCase))
                .Select(property => property.Key);
                
            foreach (var propertyKey in requiredProperties)
            {
                schema.RequiredProperties.Add(propertyKey);
            }
        }
    } 
}