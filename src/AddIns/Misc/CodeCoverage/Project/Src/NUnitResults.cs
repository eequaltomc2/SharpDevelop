// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ICSharpCode.CodeCoverage
{
	/// <summary>
	/// Reads the NUnit results file.
	/// </summary>
	public class NUnitResults
	{
		static readonly string TestCaseElementName = "test-case";
		static readonly string MessageElementName = "message";
		static readonly string StackTraceElementName= "stack-trace";
		
		List<Task> tasks = new List<Task>();
		
		public NUnitResults(string fileName) : this(new StreamReader(fileName, true))
		{
		}
		
		public NUnitResults(XmlTextReader reader)
		{
			ReadResults(reader);
		}
		
		public NUnitResults(TextReader reader) : this(new XmlTextReader(reader))
		{
		}
		
		public List<Task> Tasks {
			get {
				return tasks;
			}
		}
		
		void ReadResults(XmlTextReader reader)
		{
			using (reader) {
				while (reader.Read()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (IsTestCaseFailureElement(reader)) {
							ReadErrorTask(reader);
						}
					}
				}
			}
		}
		
		bool IsTestCaseFailureElement(XmlReader reader)
		{
			if (reader.Name == TestCaseElementName) {
				string success = reader.GetAttribute("success");
				if (success == "False") {
					return true;
				}
			}
			return false;
		}
		
		void ReadErrorTask(XmlReader reader)
		{
			string testCase = reader.GetAttribute("name");
			string message = String.Empty;
			
			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.Element) {
					if (reader.Name == MessageElementName) {
						message = reader.ReadElementString();
					} else if (reader.Name == StackTraceElementName) {
						string stackTrace = reader.ReadElementString();
						AddTask(GetDescription(testCase, message), stackTrace);
						return;
					}
				}
			}
		}
		
		/// <summary>
		/// Gets task error description.
		/// </summary>
		string GetDescription(string testCase, string message)
		{
			return StringParser.Parse("${res:NUnitPad.NUnitPadContent.TestTreeView.TestFailedMessage}", new string[,] {
				{"TestCase", testCase},
				{"Message", message}
			});
		}
		
		void AddTask(string description, string stackTrace)
		{
			FileLineReference lineRef = OutputTextLineParser.GetNUnitOutputFileLineReference(stackTrace, true);
			if (lineRef != null) {
				Task task = new Task(Path.GetFullPath(lineRef.FileName),
				                     description,
				                     lineRef.Column,
				                     lineRef.Line,
				                     TaskType.Error);
				tasks.Add(task);
			}
		}
	}
}
