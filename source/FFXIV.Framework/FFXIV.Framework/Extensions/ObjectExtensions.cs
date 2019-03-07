using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FFXIV.Framework.Extensions
{
    public static class ObjectExtensions
    {
        public static T CopyTo<T>(
            this T src,
            T dest,
            bool compile = false)
        {
            if (!compile)
            {
                return CopyToDefault(src, dest);
            }
            else
            {
                return CopyToCompile(src, dest);
            }
        }

        private static T CopyToDefault<T>(T src, T dest)
        {
            if (src == null || dest == null)
            {
                return dest;
            }

            var srcProperties = src.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            var destProperties = dest.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            var properties = srcProperties.Join(
                destProperties,
                p => new { p.Name, p.PropertyType },
                p => new { p.Name, p.PropertyType },
                (p1, p2) => new { p1, p2 });

            foreach (var property in properties)
            {
                property.p2.SetValue(dest, property.p1.GetValue(src));
            }

            return dest;
        }

        private static readonly Dictionary<string, Type> Comp = new Dictionary<string, Type>();

        private static T2 CopyToCompile<T1, T2>(T1 src, T2 dest)
        {
            var className = GetClassName(typeof(T1), typeof(T2));

            GenerateCopyClass<T1, T2>();

            Comp[className].InvokeMember(
                "CopyProps",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                null,
                null,
                new object[] { src, dest });

            return dest;
        }

        private static void GenerateCopyClass<T1, T2>()
        {
            var sourceType = typeof(T1);
            var targetType = typeof(T2);
            var className = GetClassName(typeof(T1), typeof(T2));

            if (Comp.ContainsKey(className))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.Append("namespace Copy {\r\n");
            builder.Append("    public class ");
            builder.Append(className);
            builder.Append(" {\r\n");
            builder.Append("        public static void CopyProps(");
            builder.Append(sourceType.FullName);
            builder.Append(" source, ");
            builder.Append(targetType.FullName);
            builder.Append(" target) {\r\n");

            var map = GetMatchingProperties(sourceType, targetType);

            foreach (var item in map)
            {
                builder.Append("            target.");
                builder.Append(item.TargetProperty.Name);
                builder.Append(" = ");
                builder.Append("source.");
                builder.Append(item.SourceProperty.Name);
                builder.Append(";\r\n");
            }

            builder.Append("        }\r\n   }\r\n}");

            var compiler = CodeDomProvider.CreateProvider("CSharp");
            var param = new CompilerParameters();

            param.ReferencedAssemblies.Add(typeof(T1).Assembly.Location);
            param.ReferencedAssemblies.Add(typeof(T2).Assembly.Location);
            param.GenerateInMemory = true;

            var results = compiler.CompileAssemblyFromSource(
                param,
                builder.ToString());

            foreach (var line in results.Output)
            {
                Debug.WriteLine(line);
            }

            var copierType = results.CompiledAssembly.GetType("Copy." + className);

            Comp.Add(className, copierType);
        }

        private static string GetClassName(Type sourceType, Type targetType)
        {
            var className = "Copy_";
            className += sourceType.FullName.Replace(".", "_");
            className += "_";
            className += targetType.FullName.Replace(".", "_");
            return className;
        }

        private static IList<PropertyMap> GetMatchingProperties(Type sourceType, Type targetType)
        {
            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            var properties = (
                from s in sourceProperties
                from t in targetProperties
                where
                s.Name == t.Name &&
                s.CanRead &&
                t.CanWrite &&
                s.PropertyType.IsPublic &&
                t.PropertyType.IsPublic &&
                s.PropertyType == t.PropertyType &&
                (
                    (s.PropertyType.IsValueType && t.PropertyType.IsValueType) ||
                    (s.PropertyType == typeof(string) && t.PropertyType == typeof(string))
                )
                select new PropertyMap
                {
                    SourceProperty = s,
                    TargetProperty = t
                }).ToList();

            return properties;
        }

        private class PropertyMap
        {
            public PropertyInfo SourceProperty { get; set; }
            public PropertyInfo TargetProperty { get; set; }
        }
    }
}
