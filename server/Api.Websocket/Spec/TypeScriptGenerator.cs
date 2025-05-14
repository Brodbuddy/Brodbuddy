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
        
        foreach (var (enumName, enumDef) in spec.Enums)
        {
            sb.AppendLine(string.Format(culture, "export enum {0} {{", enumName));
            foreach (var (name, value) in enumDef.Values)
            {
                sb.AppendLine(string.Format(culture, "    {0} = \"{1}\",", name, value));
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }
        
        sb.AppendLine("// WebSocket Error Codes");
        sb.AppendLine("export const ErrorCodes = {");
        foreach (var (key, value) in spec.ErrorCodes)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Request type constants");
        sb.AppendLine("export const Requests = {");
        foreach (var (key, value) in spec.RequestTypes)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Response type constants");  
        sb.AppendLine("export const Responses = {");
        foreach (var (key, value) in spec.ResponseTypes)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Broadcast type constants");
        sb.AppendLine("export const Broadcasts = {");
        foreach (var (key, value) in spec.BroadcastTypes)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Subscription methods");
        sb.AppendLine("export const SubscriptionMethods = {");
        foreach (var (key, value) in spec.SubscriptionMethods)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
    
        sb.AppendLine("// Unsubscription methods");
        sb.AppendLine("export const UnsubscriptionMethods = {");
        foreach (var (key, value) in spec.UnsubscriptionMethods)
        {
            sb.AppendLine(string.Format(culture, "    {0}: \"{1}\",", key, value));
        }
        sb.AppendLine("} as const;");
        sb.AppendLine();
        
        sb.AppendLine("// Base interfaces");
        sb.AppendLine("export interface BaseRequest {");
        sb.AppendLine("    requestId?: string;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("export interface BaseResponse {");
        sb.AppendLine("    requestId?: string;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("export interface BaseBroadcast {");
        sb.AppendLine("    // Broadcasts don't have requestId");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("// Message interfaces");
        foreach (var (typeName, typeDefinition) in spec.Types)
        {
            string baseInterface = "BaseBroadcast"; 
            
            if (spec.RequestTypes.ContainsValue(typeName))
                baseInterface = "BaseRequest";
            else if (spec.ResponseTypes.ContainsValue(typeName))
                baseInterface = "BaseResponse";
            
            sb.AppendLine(string.Format(culture, "export interface {0} extends {1} {{", typeName, baseInterface));
            foreach (var (propName, propDef) in typeDefinition.Properties)
            {
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
            var key = spec.RequestTypes.First(x => x.Value == messageType).Key;
            sb.AppendLine(string.Format(culture, "    [Requests.{0}]: [{1}, {2}];", key, mapping.RequestType, mapping.ResponseType));
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
            var key = spec.RequestTypes.First(x => x.Value == messageType).Key;
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