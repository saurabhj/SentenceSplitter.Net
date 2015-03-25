/*
* Sentence splitter
* By Marcia Munoz
*
* This program checks candidates to see if they are valid sentence boundaries.
* Its input is a text file, and its output is another text file where each text
* line corresponds to one sentence.
* 
* Ported to C# by Saurabh Jain on 21-Mar-2015
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SentenceSplitter {
    public class SentenceSplitter {

        #region Properties

        private List<string> Honorofics = null;

        #endregion

        #region ctor

        public SentenceSplitter(string honoroficsFile) {
            if (File.Exists(honoroficsFile)) {
                Honorofics = File.ReadAllLines(honoroficsFile).ToList();
            }
            else {
                throw new Exception("Honorofics file does not exist.");
            }

        }

        #endregion

        /// <summary>
        /// Splits a given string into an array of strings split by sentences.
        /// </summary>
        /// <param name="inputText">The input text.</param>
        /// <returns>
        /// Array of strings each containing one sentence
        /// </returns>
        public List<string> SplitString(string inputText) {
            var result = new List<string>();

            // Breaking down the input into lines
            var lines = inputText.Split(new char[] { '\n' });
            var paragraph = String.Empty;

            // Running through each line in the input string
            foreach (var t in lines) {
                var line = t;

                // Check if next line is empty
                if (Regex.IsMatch(line, @"^\s+$")) {
                    ProcessParagraph(paragraph, result);
                    paragraph = String.Empty;
                }
                else {
                    // Next line is not empty
                    // Trimming any leading whitespaces
                    line = line.TrimStart();

                    // Taking care of the hyphens
                    paragraph = paragraph.Trim();
                    if (paragraph.Length >= 2 && paragraph[paragraph.Length - 1] == '-' &&
                        paragraph[paragraph.Length - 2] != '-') {
                        paragraph = String.Join(String.Empty, paragraph.Substring(0, paragraph.Length - 1), line);
                    }
                    else {
                        paragraph = String.Join(" ", paragraph, line);
                    }
                }
            }

            ProcessParagraph(paragraph, result);

            return result;
        }

        /// <summary>
        /// Processes the paragraph.
        /// </summary>
        /// <param name="paragraph">The paragraph.</param>
        /// <param name="sb">The sb.</param>
        private void ProcessParagraph(string paragraph, List<string> result) {
            // Splitting the paragraph into words
            var words = paragraph.Split(new char[] { ' ' });
            var sentence = String.Empty;

            for (var index = 0; index < words.Length; index++) {
                var w = words[index];

                // Checking the existence of a candidate
                var periodPos = w.LastIndexOf(".", System.StringComparison.Ordinal);
                var questionPos = w.LastIndexOf("?", System.StringComparison.Ordinal);
                var exclaimPos = w.LastIndexOf("!", System.StringComparison.Ordinal);

                // Determine the position of the rightmost candidate in the word
                var pos = periodPos;
                var candidate = ".";

                if (questionPos > periodPos) {
                    pos = questionPos;
                    candidate = "?";
                }
                if (exclaimPos > pos) {
                    pos = exclaimPos;
                    candidate = "!";
                }

                // Do the following only if the word has a candidate
                if (pos != -1) {
                    string wm1 = null;
                    string wm1C = null;
                    string wm2 = null;
                    string wm2C = null;

                    string wp1 = null;
                    string wp1C = null;
                    string wp2 = null;
                    string wp2C = null;

                    // Check the previous word
                    if (index - 1 < 0) {
                        wm1 = "NP";
                        wm1C = "NP";
                        wm2 = "NP";
                        wm2C = "NP";
                    }
                    else {
                        wm1 = words[index - 1];
                        wm1C = Capital(wm1);

                        // Check the word before the previous one
                        if (index - 2 < 0) {
                            wm2 = "NP";
                            wm2C = "NP";
                        }
                        else {
                            wm2 = words[index - 2];
                            wm2C = Capital(wm2);
                        }
                    }

                    // Check the next word
                    if (index + 1 >= words.Length) {
                        wp1 = "NP";
                        wp1C = "NP";
                        wp2 = "NP";
                        wp2C = "NP";
                    }
                    else {
                        wp1 = words[index + 1];
                        wp1C = Capital(wp1);

                        // Check the word after the next one
                        if (index + 2 >= words.Length) {
                            wp2 = "NP";
                            wp2C = "NP";
                        }
                        else {
                            wp2 = words[index + 2];
                            wp2C = Capital(wp2);
                        }
                    }

                    // Define the prefix
                    string prefix = null;
                    string suffix = null;

                    if (pos == 0) {
                        prefix = "sp";
                    }
                    else {
                        prefix = w.Substring(0, pos);
                    }
                    var prC = Capital(prefix);

                    if (pos == w.Length - 1) {
                        suffix = "sp";
                    }
                    else {
                        suffix = w.Substring(pos + 1, w.Length - pos -1);
                    }
                    var suC = Capital(suffix);

                    // Call the sentence boundary subroutine
                    var prediction = Boundary(candidate, wm2, wm1, prefix, suffix,
                        wp1, wp2, wm2C, wm1C, prC, suC, wp1C, wp2C);

                    // Append the word to the sentence
                    sentence = String.Join(" ", sentence, w);
                    if (prediction == "Y") {
                        // Eliminate any leading whitespace
                        sentence = sentence.Substring(1);
                        result.Add(sentence);
                        sentence = String.Empty;
                    }
                }
                else {
                    // If the word doesn't have a candidate, then append the word to the sentence
                    sentence = String.Join(" ", sentence, w);
                }
            }

            if (sentence != String.Empty) {
                // Eliminate any leading whitespace
                sentence = sentence.Substring(1);
                result.Add(sentence);
                sentence = String.Empty;
            }
        }

        /// <summary>
        /// Returns Y if the argument starts with a capital letter
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private string Capital(string text) {
            if (String.IsNullOrEmpty(text)) {
                return "N";
            }

            return (Regex.IsMatch(text[0].ToString(), @"[A-Z]")) ? "Y" : "N";
        }

        /// <summary>
        /// This subroutine does all the boundary determination stuff
        /// </summary>
        /// <param name="candidate">The candidate.</param>
        /// <param name="wm2">The WM2.</param>
        /// <param name="wm1">The WM1.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="suffix">The suffix.</param>
        /// <param name="wp1">The WP1.</param>
        /// <param name="wp2">The WP2.</param>
        /// <param name="wm2C">The WM2 c.</param>
        /// <param name="wm1C">The WM1 c.</param>
        /// <param name="prC">The pr c.</param>
        /// <param name="suC">The su c.</param>
        /// <param name="wp1C">The WP1 c.</param>
        /// <param name="wp2C">The WP2 c.</param>
        /// <returns>"Y" if it determines the candidate to be a sentence boundary, N otherwise</returns>
        private string Boundary(string candidate, string wm2, string wm1,
            string prefix, string suffix, string wp1, string wp2, string wm2C, string wm1C,
            string prC, string suC, string wp1C, string wp2C) {

            // Check if the candidate was a question mark or an exclamation mark
            if (candidate == "?" || candidate == "!") {
                // Check for the end of the file
                if (wp1 == "NP" && wp2 == "NP") {
                    return "Y";
                }

                // Check for the case of a question mark followed by a capitalized word
                if (suffix == "sp" && wp1C == "Y") {
                    return "Y";
                }

                if (suffix == "sp" && StartsWithQuote(wp1)) {
                    return "Y";
                }

                if (suffix == "sp" && wp1 == "--" && wp2C == "Y") {
                    return "Y";
                }

                if (suffix == "sp" && wp1 == "-RBR-" && wp2C == "Y") {
                    return "Y";
                }

                /*
                 * This rule takes into account vertical ellipses, as shown in the
                 * training corpus. We are assuming that horizontal ellipses are
                 * represented by a continuous series of periods. If this is not a
                 * vertical ellipsis, then it's a mistake in how the sentences were
                 * separated.
                 * */
                if (suffix == "sp" && wp1 == ".") {
                    return "Y";
                }

                if (IsRightEnd(suffix) && IsLeftStart(wp1)) {
                    return "Y";
                }
                else {
                    return "N";
                }
            }
            else {
                // Check for the end of the file
                if (wp1 == "NP" && wp2 == "NP") {
                    return "Y";
                }

                if (suffix == "sp" && StartsWithQuote(wp1)) {
                    return "Y";
                }

                if (suffix == "sp" && StartsWithLeftParen(wp1)) {
                    return "Y";
                }

                if (suffix == "sp" && wp1 == "-RBR-" && wp2 == "--") {
                    return "N";
                }

                if (suffix == "sp" && IsRightParen(wp1)) {
                    return "Y";
                }

                /*
                 * This rule takes into account vertical ellipses, as shown in the
                 * training corpus. We are assuming that horizontal ellipses are
                 * represented by a continuous series of periods. If this is not a
                 * vertical ellipsis, then it's a mistake in how the sentences were
                 * separated.
                 * */
                if (prefix == "sp" && suffix == "sp" && wp1 == ".") {
                    return "N";
                }
                if (suffix == "sp" && wp1 == ".") {
                    return "Y";
                }
                if (suffix == "sp" && wp1 == "--" && wp2C == "Y" && EndsInQuote(prefix)) {
                    return "N";
                }
                if (suffix == "sp" && wp1 == "--" && (wp2C == "Y" || StartsWithQuote(wp2))) {
                    return "Y";
                }
                if (suffix == "sp" && wp1C == "Y" &&
                    (String.Compare(prefix, "p.m", true) == 0 || String.Compare(prefix, "a.m", true) == 0) &&
                    IsTimeZone(wp1)) {
                    return "N";
                }

                // Check for the case when a capitalized word follows a period,
                // and the prefix is a honorific
                if (suffix == "sp" && wp1C == "Y" && IsHonorific(prefix + ".")) {
                    return "N";
                }

                // Check for the case when a capitalized word follows a period,
                // and the prefix is a honorific
                if (suffix == "sp" && wp1C == "Y" && StartsWithQuote(prefix)) {
                    return "N";
                }

                // This rule checks for prefixes that are terminal abbreviations
                if (suffix == "sp" && wp1C == "Y" && IsTerminal(prefix)) {
                    return "Y";
                }

                // Check for the case when a capitalized word follows a period and the
                // prefix is a single capital letter
                if (suffix == "sp" && wp1C == "Y" && Regex.IsMatch(prefix, @"^([A-Z]\.)*[A-Z]$")) {
                    return "N";
                }

                // Check for the case when a capitalized word follows a period
                if (suffix == "sp" && wp1C == "Y") {
                    return "Y";
                }
                if (IsRightEnd(suffix) && IsLeftStart(wp1)) {
                    return "Y";
                }
            }

            return "N";
        }

        /// <summary>
        /// Checks to see if the input string is equal to an element of the Honorofics List
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        private bool IsHonorific(string p) {
            return Honorofics.Contains(p);
        }

        /// <summary>
        /// Checks to see if the string is a terminal abbreviation.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private bool IsTerminal(string prefix) {
            var terminals = new List<string>() { "Esq", "Jr", "Sr", "M.D" };
            return terminals.Contains(prefix);
        }

        /// <summary>
        /// Checks if the string is a standard representation of a Timezone
        /// </summary>
        /// <param name="wp1">The WP1.</param>
        /// <returns></returns>
        private bool IsTimeZone(string wp1) {
            // The most common timezones are fed into this list
            // From: http://users.telenet.be/mm011/time%20zone%20abbreviations.html
            var timezones = new List<string>() { "UTC", "UT", "TAI", "GMT", "BST", "IST", "WET", "WEST", "CET", "CEST", "EET", "EEST", "MSK", "MSD", "AST", "ADT", "EST", "EDT", "ET", "CST", "CDT", "CT", "MST", "MDT", "MT", "PST", "PDT", "PT", "HST", "AKST", "AKDT", "AEST", "AEDT", "ACST", "ACDT", "AWST" };
            return timezones.Contains(wp1);
        }

        /// <summary>
        /// Checks to see if the input word ends in a closing double quote
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        private bool EndsInQuote(string prefix) {
            return (prefix.EndsWith("'") || prefix.EndsWith("\""));
        }

        /// <summary>
        /// Checks to see if a given word starts with one or more quotes
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns></returns>
        private bool StartsWithQuote(string prefix) {
            return (prefix.StartsWith("'") || prefix.StartsWith("\"") || prefix.StartsWith("`"));
        }

        /// <summary>
        /// Checks to see if a word starts with a left parenthesis
        /// </summary>
        /// <param name="wp1">The WP1.</param>
        /// <returns></returns>
        private bool StartsWithLeftParen(string wp1) {
            return wp1.StartsWith("<") || wp1.StartsWith("[") || wp1.StartsWith("{") || wp1.StartsWith("(") || wp1.StartsWith("-LBR-");
        }

        /// <summary>
        /// Checks to see if a word ends with a right parenthesis
        /// </summary>
        /// <param name="wp1">The WP1.</param>
        /// <returns></returns>
        private bool EndsWithRightParen(string wp1) {
            return wp1.EndsWith("}") || wp1.EndsWith(")") || wp1.EndsWith(">") || wp1.EndsWith("]") ||
                   wp1.EndsWith("-LBR-");
        }

        /// <summary>
        /// Checks to see if a word starts with a left quote
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns></returns>
        private bool StartsWithLeftQuote(string word) {
            return word.StartsWith("`") || word.StartsWith("\"") || word.StartsWith("\"`");
        }

        /// <summary>
        /// Determines whether [is right end] [the specified suffix].
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        /// <returns></returns>
        private bool IsRightEnd(string suffix) {
            return IsRightParen(suffix) || IsRightQuote(suffix);
        }

        /// <summary>
        /// Determines whether [is left start] [the specified WP1].
        /// </summary>
        /// <param name="wp1">The WP1.</param>
        /// <returns></returns>
        private bool IsLeftStart(string wp1) {
            return StartsWithLeftQuote(wp1) || StartsWithLeftParen(wp1) || Capital(wp1) == "Y";
        }

        /// <summary>
        /// Checks to see if a word is a right parenthesis
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns></returns>
        private bool IsRightParen(string word) {
            var paren = new List<string>() { "}", ")", "-RBR-" };
            return paren.Contains(word);
        }

        /// <summary>
        /// Checks to see if the word is a right quote
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        /// <returns></returns>
        private bool IsRightQuote(string suffix) {
            var quotes = new List<string>() { "'", "''", "'''", "\"", "'\"" };
            return quotes.Contains(suffix);
        }
    }
}
