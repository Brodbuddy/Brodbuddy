using System.Globalization;
using System.Text;

namespace Api.Websocket.Spec;

public static class TypeScriptGenerator
{
    public static void Generate(WebSocketSpec spec, string templatesDirectory, string outputDirectory)
    {
        var typesContent = GenerateTypeDefinitions(spec);
        var methodsContent = GenerateClientMethods(spec);
        
        CombineWithTemplate(
            Path.Combine(templatesDirectory, "websocket-client-template.ts"), 
            Path.Combine(outputDirectory, "websocket-client.ts"),
            typesContent,
            methodsContent
        );
    }
    
    private static string GenerateTypeDefinitions(WebSocketSpec spec)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.InvariantCulture;
        
        sb.AppendLine("// Message type constants");
        sb.AppendLine("export const MessageType = {");
        foreach (var (key, value) in spec.MessageTypes)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Base message interface");
        sb.AppendLine("export interface BaseMessage {");
        sb.AppendLine("    requestId?: string;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("// Message interfaces");
        foreach (var (typeName, typeDefinition) in spec.Types)
        {
            sb.AppendLine(string.Format(culture, "export interface {0} extends BaseMessage {{", typeName));
            foreach (var (propName, propDef) in typeDefinition.Properties)
            {
                // Behold PascalCase for property navne
                var optional = propDef.IsRequired ? "" : "?";
                sb.AppendLine(string.Format(culture, "    {0}{1}: {2};", propName, optional, propDef.Type));
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }
        
        sb.AppendLine("// Request-response type mapping");
        sb.AppendLine("export type RequestResponseMap = {");
        foreach (var (messageType, mapping) in spec.RequestResponses)
        {
            var key = spec.MessageTypes.First(x => x.Value == messageType).Key;
            sb.AppendLine(string.Format(culture, "    [MessageType.{0}]: [{1}, {2}];", key, mapping.RequestType, mapping.ResponseType));
        }
        sb.AppendLine("};");
        sb.AppendLine();
        
        return sb.ToString();
    }
    
    private static string GenerateClientMethods(WebSocketSpec spec)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.InvariantCulture;
        
        sb.AppendLine("send = {");
        foreach (var (messageType, mapping) in spec.RequestResponses)
        {
            var key = spec.MessageTypes.First(x => x.Value == messageType).Key;
            sb.AppendLine(string.Format(culture, "    {0}: (payload: Omit<{1}, 'requestId'>): Promise<{2}> => {{", key, mapping.RequestType, mapping.ResponseType));
            sb.AppendLine(string.Format(culture, "        return this.sendRequest<{0}>('{1}', payload);", mapping.ResponseType, messageType));
            sb.AppendLine("    },");
        }
        sb.AppendLine("};");
        
        return sb.ToString();
    }
    
    private static void CombineWithTemplate(string templatePath, string outputPath, string typesContent, string methodsContent)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }
        
        var template = File.ReadAllText(templatePath);
        
        if (!template.Contains("/* GENERATED_IMPORTS */"))
        {
            throw new InvalidOperationException("Template file does not contain the marker /* GENERATED_IMPORTS */");
        }
        
        if (!template.Contains("/* GENERATED_SEND_METHODS */"))
        {
            throw new InvalidOperationException("Template file does not contain the marker /* GENERATED_SEND_METHODS */");
        }
        
        var result = template.Replace("/* GENERATED_IMPORTS */", typesContent)
                             .Replace("/* GENERATED_SEND_METHODS */", methodsContent);
        
        File.WriteAllText(outputPath, result);
    }
}