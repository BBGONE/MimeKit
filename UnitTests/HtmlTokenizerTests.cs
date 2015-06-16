﻿//
// HtmlTokenizerTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013-2015 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;

using MimeKit.Text;
using MimeKit.Utils;

using NUnit.Framework;

namespace UnitTests {
	[TestFixture]
	public class HtmlTokenizerTests
	{
		static void VerifyHtmlTokenizerOutput (string path)
		{
			var tokens = Path.ChangeExtension (path, ".tokens");
			var expected = File.Exists (tokens) ? File.ReadAllText (tokens) : string.Empty;
			var actual = new StringBuilder ();

			using (var textReader = File.OpenText (path)) {
				var tokenizer = new HtmlTokenizer (textReader);
				HtmlToken token;

				Assert.AreEqual (HtmlTokenizerState.Data, tokenizer.TokenizerState);

				while (tokenizer.ReadNextToken (out token)) {
					actual.AppendFormat ("{0}: ", token.Kind);

					switch (token.Kind) {
					case HtmlTokenKind.ScriptData:
					case HtmlTokenKind.CData:
					case HtmlTokenKind.Data:
						var text = (HtmlDataToken) token;

						for (int i = 0; i < text.Data.Length; i++) {
							switch (text.Data[i]) {
							case '\f': actual.Append ("\\f"); break;
							case '\t': actual.Append ("\\t"); break;
							case '\r': actual.Append ("\\r"); break;
							case '\n': actual.Append ("\\n"); break;
							default: actual.Append (text.Data[i]); break;
							}
						}
						actual.AppendLine ();
						break;
					case HtmlTokenKind.Tag:
						var tag = (HtmlTagToken) token;

						actual.AppendFormat ("<{0}{1}", tag.IsEndTag ? "/" : "", tag.Name);

						foreach (var attribute in tag.Attributes) {
							if (attribute.Value != null)
								actual.AppendFormat (" {0}={1}", attribute.Name, MimeUtils.Quote (attribute.Value));
							else
								actual.AppendFormat (" {0}", attribute.Name);
						}

						actual.Append (tag.IsEmptyElement ? "/>" : ">");

						actual.AppendLine ();
						break;
					case HtmlTokenKind.Comment:
						var comment = (HtmlCommentToken) token;
						actual.AppendLine (comment.Comment);
						break;
					case HtmlTokenKind.DocType:
						var doctype = (HtmlDocTypeToken) token;

						if (doctype.ForceQuirksMode)
							actual.Append ("<!-- force quirks mode -->");

						actual.Append ("<!DOCTYPE");

						if (doctype.Name != null)
							actual.AppendFormat (" {0}", doctype.Name.ToUpperInvariant ());

						if (doctype.PublicIdentifier != null) {
							actual.AppendFormat (" PUBLIC {0}", MimeUtils.Quote (doctype.PublicIdentifier));
							if (doctype.SystemIdentifier != null)
								actual.AppendFormat (" {0}", MimeUtils.Quote (doctype.SystemIdentifier));
						} else if (doctype.SystemIdentifier != null) {
							actual.AppendFormat (" SYSTEM {0}", MimeUtils.Quote (doctype.SystemIdentifier));
						}

						actual.Append (">");
						actual.AppendLine ();
						break;
					default:
						Assert.Fail ("Unhandled token type: {0}", token.Kind);
						break;
					}
				}

				Assert.AreEqual (HtmlTokenizerState.EndOfFile, tokenizer.TokenizerState);
			}

			if (!File.Exists (tokens))
				File.WriteAllText (tokens, actual.ToString ());

			Assert.AreEqual (expected, actual.ToString (), "The token stream does not match the expected tokens.");
		}

		[Test]
		public void TestGoogleSignInAttemptBlocked ()
		{
			VerifyHtmlTokenizerOutput (Path.Combine ("..", "..", "TestData", "html", "blocked.html"));
		}

		[Test]
		public void TestXamarin3SampleHtml ()
		{
			VerifyHtmlTokenizerOutput (Path.Combine ("..", "..", "TestData", "html", "xamarin3.html"));
		}

		[Test]
		public void TestTokenizer ()
		{
			VerifyHtmlTokenizerOutput (Path.Combine ("..", "..", "TestData", "html", "test.html"));
		}

		[Test]
		public void TestHtmlDecode ()
		{
			const string encoded = "&lt;&pound;&euro;&cent;&yen;&nbsp;&copy;&reg;&gt;";
			const string expected = "<£€¢¥\u00a0©®>";

			var decoded = HtmlUtils.HtmlDecode (encoded);

			Assert.AreEqual (expected, decoded);
		}
	}
}