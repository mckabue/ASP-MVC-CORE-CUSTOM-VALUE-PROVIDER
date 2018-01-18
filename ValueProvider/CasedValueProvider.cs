using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValueProvider
{
    public class CasedValueProvider : IValueProvider
    {
        private IDictionary<string, StringValues> _query;

        public CasedValueProvider(IDictionary<string, StringValues> query)
        {
            _query = query;
        }

        public bool ContainsPrefix(string prefix)
        {
            //return ModelStateDictionary.StartsWithPrefix(prefix, Key);
            return false;
        }

        public ValueProviderResult GetValue(string key)
        {
            var value = _query?.Where(q =>
                //EXACTLY EQUAL
                q.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                //EXACTLY EQUAL BUT DECODED
                || q.Key.UrlDecode().Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)


                //EQUAL WITHOUT HYPHEN
                || q.Key.Replace("-", String.Empty).Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.Replace("-", String.Empty).Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Replace("-", String.Empty).Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Replace("-", String.Empty).Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                //EQUAL WITHOUT HYPHEN BUR DECODED
                || q.Key.UrlDecode().Replace("-", String.Empty).Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().Replace("-", String.Empty).Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Replace("-", String.Empty).Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Replace("-", String.Empty).Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)


                //EQUAL WITH HYPHEN REPLACED BY EMPTY UNDERSCORE
                || q.Key.Replace("-", "_").Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.Replace("-", "_").Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Replace("-", "_").Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.RemoveWhitespace().Replace("-", "_").Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                //EQUAL WITH HYPHEN REPLACED BY EMPTY UNDERSCORE BUT DECODED
                || q.Key.UrlDecode().Replace("-", "_").Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().Replace("-", "_").Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Replace("-", "_").Equals(key, StringComparison.OrdinalIgnoreCase)
                || q.Key.UrlDecode().RemoveWhitespace().Replace("-", "_").Equals(key.RemoveWhitespace(), StringComparison.OrdinalIgnoreCase)
            );
            if (value?.Any() ?? false)
            {
                return new ValueProviderResult(value.FirstOrDefault(v => v.Value.Any(s => s.HasValue())).Value, culture: CultureInfo.InvariantCulture);
            }
            else
            {
                return ValueProviderResult.None;
            }
        }
    }

    public class CasedValueProviderFactory_Query : IValueProviderFactory
    {
        private readonly string _separator;
        private readonly string _key;

        public CasedValueProviderFactory_Query()
        {
        }

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.QueryString.HasValue)
            {
                return AddValueProviderAsync(context);
            }
            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            var valueProvider = new CasedValueProvider(request.Query.ToDictionary(i => i.Key, i => i.Value));
            //context.ValueProviders.Insert(0, new CasedValueProvider(request.Query.ToDictionary(i => i.Key, i => i.Value)));
            context.ValueProviders.Add(valueProvider);
        }
    }

    /// <summary>
    /// https://github.com/aspnet/Mvc/blob/760c8f38678118734399c58c2dac981ea6e47046/src/Microsoft.AspNetCore.Mvc.Core/ModelBinding/JQueryFormValueProviderFactory.cs
    /// </summary>
    public class CasedValueProviderFactory_jQueryForm : IValueProviderFactory
    {
        private readonly string _separator;
        private readonly string _key;

        public CasedValueProviderFactory_jQueryForm()
        {
        }

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                return AddValueProviderAsync(context);
            }
            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            var valueProvider = new CasedValueProvider(await GetValueCollectionAsync(request));

            context.ValueProviders.Add(valueProvider);
        }

        private static async Task<IDictionary<string, StringValues>> GetValueCollectionAsync(HttpRequest request)
        {
            var formCollection = await request.ReadFormAsync();

            var builder = new StringBuilder();
            var dictionary = new Dictionary<string, StringValues>(
                formCollection.Count,
                StringComparer.OrdinalIgnoreCase);
            foreach (var entry in formCollection)
            {
                var key = NormalizeJQueryToMvc(builder, entry.Key);
                builder.Clear();

                dictionary[key] = entry.Value;
            }

            return dictionary;
        }

        // This is a helper method for Model Binding over a JQuery syntax.
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys.
        // x[] --> x
        // [] --> ""
        // x[12] --> x[12]
        // x[field]  --> x.field, where field is not a number
        private static string NormalizeJQueryToMvc(StringBuilder builder, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var indexOpen = key.IndexOf('[');
            if (indexOpen == -1)
            {

                // Fast path, no normalization needed.
                // This skips string conversion and allocating the string builder.
                return key;
            }

            var position = 0;
            while (position < key.Length)
            {
                if (indexOpen == -1)
                {
                    // No more brackets.
                    builder.Append(key, position, key.Length - position);
                    break;
                }

                builder.Append(key, position, indexOpen - position); // everything up to "["

                // Find closing bracket.
                var indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw new ArgumentException(
                        message: $"Resources.FormatJQueryFormValueProviderFactory_MissingClosingBracket({key})", //Resources.FormatJQueryFormValueProviderFactory_MissingClosingBracket(key)
                        paramName: nameof(key));
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty brackets signify an array. Just remove.
                }
                else if (char.IsDigit(key[indexOpen + 1]))
                {
                    // Array index. Leave unchanged.
                    builder.Append(key, indexOpen, indexClose - indexOpen + 1);
                }
                else
                {
                    // Field name. Convert to dot notation.
                    builder.Append('.');
                    builder.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                }

                position = indexClose + 1;
                indexOpen = key.IndexOf('[', position);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// https://github.com/aspnet/Mvc/blob/760c8f38678118734399c58c2dac981ea6e47046/src/Microsoft.AspNetCore.Mvc.Core/ModelBinding/FormValueProviderFactory.cs
    /// </summary>
    public class CasedValueProviderFactory_Form : IValueProviderFactory
    {
        private readonly string _separator;
        private readonly string _key;

        public CasedValueProviderFactory_Form()
        {
        }

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                return AddValueProviderAsync(context);
            }
            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            var valueProvider = new CasedValueProvider((await request.ReadFormAsync()).ToDictionary(i => i.Key, i => i.Value));

            context.ValueProviders.Add(valueProvider);
        }
    }

    public static class ValueProviderHelpers
    {
        public static string RemoveWhitespace(this string str) => string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        public static string UrlDecode(this string value) => Uri.UnescapeDataString(value); //System.Net.WebUtility.UrlDecode(value);
        public static bool HasValue(this string _value) => !(string.IsNullOrEmpty(_value) || string.IsNullOrWhiteSpace(_value));
    }
}
