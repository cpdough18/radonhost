#region

using System;
using System.Text.RegularExpressions;

#endregion

namespace Radon.Services
{
    // source: https://github.com/DSharpPlus/DSharpPlus/

    /// <summary>
    ///     Contains markdown formatting helpers.
    /// </summary>
    public static class Formatter
    {
        private static Regex MdSanitizeRegex { get; } =
            new Regex(@"([`\*_~<>\[\]\(\)""@\!\&#:])", RegexOptions.ECMAScript);

        private static Regex MdStripRegex { get; } =
            new Regex(@"([`\*_~\[\]\(\)""]|<@\!?\d+>|<#\d+>|<@\&\d+>|<:[a-zA-Z0-9_\-]:\d+>)", RegexOptions.ECMAScript);

        /// <summary>
        ///     Creates a block of code.
        /// </summary>
        /// <param name="content">Contents of the block.</param>
        /// <param name="language">Language to use for highlighting.</param>
        /// <returns>Formatted block of code.</returns>
        public static string BlockCode(this string content, string language = "")
        {
            return $"```{language}\n{content}\n```";
        }

        /// <summary>
        ///     Creates inline code snippet.
        /// </summary>
        /// <param name="content">Contents of the snippet.</param>
        /// <returns>Formatted inline code snippet.</returns>
        public static string InlineCode(this string content)
        {
            return $"`{content}`";
        }

        /// <summary>
        ///     Creates bold text.
        /// </summary>
        /// <param name="content">Text to bolden.</param>
        /// <returns>Formatted text.</returns>
        public static string Bold(this string content)
        {
            return $"**{content}**";
        }

        /// <summary>
        ///     Creates italicized text.
        /// </summary>
        /// <param name="content">Text to italicize.</param>
        /// <returns>Formatted text.</returns>
        public static string Italic(this string content)
        {
            return $"*{content}*";
        }

        /// <summary>
        ///     Creates underlined text.
        /// </summary>
        /// <param name="content">Text to underline.</param>
        /// <returns>Formatted text.</returns>
        public static string Underline(this string content)
        {
            return $"__{content}__";
        }

        /// <summary>
        ///     Creates strikethrough text.
        /// </summary>
        /// <param name="content">Text to strikethrough.</param>
        /// <returns>Formatted text.</returns>
        public static string Strike(this string content)
        {
            return $"~~{content}~~";
        }

        /// <summary>
        ///     Creates a URL that won't create a link preview.
        /// </summary>
        /// <param name="url">Url to prevent from being previewed.</param>
        /// <returns>Formatted url.</returns>
        public static string EmbedlessUrl(this Uri url)
        {
            return $"<{url}>";
        }

        /// <summary>
        ///     Creates a masked link. This link will display as specified text, and alternatively provided alt text. This can only
        ///     be used in embeds.
        /// </summary>
        /// <param name="text">Text to display the link as.</param>
        /// <param name="url">Url that the link will lead to.</param>
        /// <param name="altText">Alt text to display on hover.</param>
        /// <returns>Formatted url.</returns>
        public static string MaskedUrl(this string text, Uri url, string altText = "")
        {
            return $"[{text}]({url}{(!string.IsNullOrWhiteSpace(altText) ? $" \"{altText}\"" : "")})";
        }

        /// <summary>
        ///     Escapes all markdown formatting from specified text.
        /// </summary>
        /// <param name="text">Text to sanitize.</param>
        /// <returns>Sanitized text.</returns>
        public static string Sanitize(this string text)
        {
            return MdSanitizeRegex.Replace(text, m => $"\\{m.Groups[1].Value}");
        }

        /// <summary>
        ///     Removes all markdown formatting from specified text.
        /// </summary>
        /// <param name="text">Text to strip of formatting.</param>
        /// <returns>Formatting-stripped text.</returns>
        public static string Strip(this string text)
        {
            return MdStripRegex.Replace(text, m => string.Empty);
        }

        /// <summary>
        ///     Creates a url for using attachments in embeds. This can only be used as an Image URL, Thumbnail URL, Author icon
        ///     URL or Footer icon URL.
        /// </summary>
        /// <param name="filename">Name of attached image to display</param>
        /// <returns></returns>
        public static string AttachedImageUrl(this string filename)
        {
            return $"attachment://{filename}";
        }
        /// <summary>
        ///     Truncates strings to specified length
        /// </summary>
        /// <param name="value">Input string</param>
        /// <param name="maxLength">Maxmimum length allowed</param>
        /// <returns>Truncated string</returns>
        public static string WithMaxLength(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
    }
}