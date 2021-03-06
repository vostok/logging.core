﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Helpers;
using Vostok.Logging.Core.Helpers;

namespace Vostok.Logging.Core
{
    /// <summary>
    /// A helper class to render log messages. See <see cref="FormatMessage"/> for details.
    /// </summary>
    [PublicAPI]
    public static class LogMessageFormatter
    {
        // TODO(krait): Can't it render messages directly to TextWriter? But let's not change it yet..
        /// <summary>
        /// <para>Renders the template to a fully formed log message, replacing placeholders with values from <paramref name="properties"/>.</para>
        /// <para>See <see cref="LogEvent.MessageTemplate"/> for details on <paramref name="template"/> format.</para>
        /// <para>This method never throws exceptions.</para>
        /// </summary>
        /// <param name="template">A message template with zero or more placeholders to substitute.</param>
        /// <param name="properties">A dictionary of properties to be used for substitution.</param>
        public static string FormatMessage([CanBeNull] string template, [CanBeNull] IReadOnlyDictionary<string, object> properties)
        {
            if (template == null)
                return null;

            if (properties == null)
                return template;

            var resultBuilder = StringBuilderCache.Acquire(template.Length*2);
            var tokenBuilderChars = CharArrayCache.Acquire(template.Length);
            var tokenBuilder = new TokenBuilder(tokenBuilderChars);

            for (var i = 0; i < template.Length; i++)
            {
                var currentChar = template[i];

                if (currentChar != '{' && currentChar != '}')
                {
                    tokenBuilder.Add(currentChar);
                    continue;
                }

                if (!tokenBuilder.IsEmpty)
                    tokenBuilder.MoveToResult(resultBuilder);

                if (i == template.Length - 1)
                {
                    tokenBuilder.Add(currentChar);
                    continue;
                }

                var nextChar = template[i + 1];
                if (currentChar == nextChar)
                {
                    tokenBuilder.Add(currentChar);
                    i++;
                    continue;
                }

                if (currentChar == '}')
                {
                    tokenBuilder.Add(currentChar);
                    continue;
                }

                var findTokenResult = tokenBuilder.TryFindToken(template, i);

                i += tokenBuilder.Length - 1;

                if (findTokenResult)
                {
                    var key = tokenBuilder.GetKeyFromBuffer();
                    if (properties.TryGetValue(key, out var value))
                    {
                        resultBuilder.Append(FormatPropertyValue(value));
                        tokenBuilder.Clear();
                    }
                }
            }

            if (!tokenBuilder.IsEmpty)
                tokenBuilder.MoveToResult(resultBuilder);

            CharArrayCache.Return(tokenBuilderChars);

            return StringBuilderCache.GetStringAndRelease(resultBuilder);
        }

        private static string FormatPropertyValue(object value)
        {
            if (value is IFormattable formattableValue)
                return formattableValue.ToString(null, CultureInfo.InvariantCulture);

            return value?.ToString() ?? "null";
        }

        private struct TokenBuilder
        {
            private readonly char[] chars;

            public TokenBuilder(char[] chars)
            {
                this.chars = chars;

                Length = 0;
            }

            public int Length { get; private set; }

            public bool IsEmpty => Length == 0;

            public bool TryFindToken(string template, int startIndex)
            {
                if (startIndex < 0 || startIndex > template.Length - 1)
                    return false;

                var currentChar = template[startIndex];
                Add(currentChar);

                if (currentChar != '{')
                    return false;

                for (var i = startIndex + 1; i < template.Length; i++)
                {
                    currentChar = template[i];

                    if (currentChar == '{')
                        return false;

                    Add(currentChar);

                    if (currentChar == '}')
                        return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void MoveToResult(StringBuilder result)
            {
                if (Length == 0)
                    return;

                result.Append(chars, 0, Length);
                Length = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string GetKeyFromBuffer()
            {
                return Length <= 2
                    ? string.Empty
                    : new string(chars, 1, Length - 2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(char c)
            {
                chars[Length++] = c;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear()
            {
                Length = 0;
            }
        }
    }
}