using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoleEditorGenerator;

[Generator]
public class REGenerator: ISourceGenerator
{
    public static string[] BannedTypes = { ".ctor" };

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var roleTypes = GetAllTypes(context.Compilation).Where(t => t.Interfaces.Any(i => i.Name == "IModdable"));

        foreach (var type in roleTypes) GenerateSource(type, context);
    }

    //namespace TownOfHost.Roles.Modifiers.{type.Name}Modifier;

    private void GenerateSource(INamedTypeSymbol type, GeneratorExecutionContext context)
    {
        var fields = type.GetMembers()
            .Where(t => t.Kind is SymbolKind.Field)
            .Select(t => (IFieldSymbol)t)
            .Where(t => !t.IsStatic)
            .Where(f => !BannedTypes.Contains(f.Type.ToString()))
            .ToList();

        var methods = type.GetMembers()
            .Where(t => t.Kind is SymbolKind.Method)
            .Select(t => (IMethodSymbol)t)
            .Where(t => !t.IsStatic && !BannedTypes.Contains(t.ToString()))
            .ToList();

        string source = $@"#nullable enable
using System;
using VentLib.Logging;

namespace {type.ContainingNamespace};

public partial class {type.Name}
{{
    public abstract class {type.Name}Modifier: RoleEditor {{
        private {type} MyStaticRole => ({type})ModdedRole;
        protected {type}? MyRole => ({type}?)RoleInstance;
        protected PlayerControl MyPlayer => MyRole?.MyPlayer!;

        internal {type.Name}Modifier({type} role): base(role) {{
        }}
";


        foreach (var field in fields)
        {
            string fieldName = field.Name;

            source += $@"
        // {String.Join(", ", field)}
        public {field.Type} {fieldName} {{
            get => (MyRole ?? MyStaticRole).{fieldName};
            set => (MyRole ?? MyStaticRole).{fieldName} = value;
        }}";
        }

        foreach (var method in methods)
        {
            if (method.Name.StartsWith(".ctor") || method.Name.StartsWith("set_") || method.Name.StartsWith("get_")) continue;
            string returnString = method.ReturnType.ToString() != "void" ? "return " : "";
            source += $@"
        public {method.ReturnType} {method.Name}({String.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))}) {{
            {returnString}MyRole!.{method.Name}({String.Join(", ", method.Parameters.Select(p => p.Name))});
        }}
";
        }

        source += @"    }
}
";
        context.AddSource($"{type.Name}Modifier.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation) =>
        GetAllTypes(compilation.GlobalNamespace);

    private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var nestedType in @namespace.GetTypeMembers().SelectMany(GetNestedTypes))
            yield return nestedType;

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        foreach (var type in GetAllTypes(nestedNamespace))
            yield return type;
    }

    private IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nestedType in type.GetTypeMembers().SelectMany(GetNestedTypes))
            yield return nestedType;
    }
}