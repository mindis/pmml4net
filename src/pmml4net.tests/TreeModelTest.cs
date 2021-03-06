﻿/*
pmml4net - easy lib to read and consume tree model in PMML file
Copyright (C) 2013  Damien Carol <damien.carol@gmail.com>

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Library General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Library General Public License for more details.

You should have received a copy of the GNU Library General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 51 Franklin St, Fifth Floor,
Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace pmml4net.tests
{
	/// <summary>
	/// Description of TreeModelTest.
	/// </summary>
	public class TreeModelTest
	{
		
		[TestCase("test-golfing1.xml", "golfing",
		          "temperature=75, humidity=55, windy=\"false\", outlook=\"overcast\"",
		          "may play", 0)]
		[TestCase("test-golfing2.xml", "golfing",
		          "temperature=45, humidity=60, outlook=\"sunny\"",
		          "no play", 0.6)] // Example 1 in PMML doc
		[TestCase("test-golfing2.xml", "golfing",
		          "outlook=\"sunny\"",
		          "will play", 0.8)] // Example 2 in PMML doc
		[TestCase("test-golfing2.xml", "golfing",
		          "",
		          "will play", 0.6)] // Example 3 in PMML doc
		[TestCase("BigML1.xml", "51794c13e4b024977881b628",
		          "000000=78, 000012=0.05, 000013=0.05",
		          "Non recurrent", 0)] // confidence = 83%
		[TestCase("BigML1.xml", "51794c13e4b024977881b628",
		          "000000=36, 00000c=3.12, 000013=0",
		          "Non recurrent", 0)] // confidence = 83%
		[TestCase("BigML1.xml", "51794c13e4b024977881b628",
		          "000000=4, 00000c=2.1441125, 000013=0.03677125, 00000f=0.0191225",
		          "Non recurrent", 0)] // confidence = 51%
		[TestCase("BigML1.xml", "51794c13e4b024977881b628",
		          "00001d=1, 00000c=1, 000013=0.01, 000019=0.16, 000018=900, 000000=25",
		          "Recurrent", 0)] // confidence = 83.18%
		public void ScoreTest(string pFilePath, string modelname, string paramList, string res, decimal confidence)
		{
			Pmml pmml = Pmml.loadModels(pFilePath);
			
			Assert.NotNull(pmml);
			
			ModelElement model = pmml.getByName(modelname);
			Assert.NotNull(model);
			
			Assert.IsInstanceOf(typeof(TreeModel), model);
			TreeModel tree = (TreeModel)model;
			
			Dictionary<string, object> lDict = parseParams(paramList);
			
			ScoreResult result = tree.Score(lDict);
			Assert.NotNull(result);
			
			
			/*foreach(Node item in result.Nodes)
			{
				Console.WriteLine("Node {0} = score {1}", item.Id, item.Score);
				
				foreach(ScoreDistribution it2 in item.ScoreDistributions)
					Console.WriteLine("\tScore Dist. {0} ({1}) = {2}", it2.Value, it2.RecordCount, it2.Confidence);
			}*/
			
			Assert.AreEqual(res, result.Value);
			Assert.AreEqual(confidence, result.Confidence);
		}
		
		[Test()]
		[Ignore("MissingValueStrategy.AggregateNodes is not implemented")]
		public void ScoreExample8Test()
		{
			string pFilePath = "test-golfing2.xml";
			string modelname = "golfing";
			string paramList = "temperature=45, humidity=90";
			string res = "may play";
			decimal confidence = 0.47M;
			
			Pmml pmml = Pmml.loadModels(pFilePath);
			
			Assert.NotNull(pmml);
			
			TreeModel tree = (TreeModel)pmml.getByName(modelname);
			Assert.NotNull(tree);
			
			// Modification for aggregateNode
			tree.MissingValueStrategy = MissingValueStrategy.AggregateNodes;
			
			Dictionary<string, object> lDict = parseParams(paramList);
			
			ScoreResult result = tree.Score(lDict);
			Assert.NotNull(result);
			
			
			/*foreach(Node item in result.Nodes)
			{
				Console.WriteLine("Node {0} = score {1}", item.Id, item.Score);
				
				foreach(ScoreDistribution it2 in item.ScoreDistributions)
					Console.WriteLine("\tScore Dist. {0} ({1}) = {2}", it2.Value, it2.RecordCount, it2.Confidence);
			}*/
			
			Assert.AreEqual(res, result.Value);
			Assert.AreEqual(confidence, result.Confidence);
		}
		
		[Test()]
		[Description("Example 7 in PMML doc")]
		public void ScoreExample7Test()
		{
			string pFilePath = "test-golfing2.xml";
			string modelname = "golfing";
			string paramList = "outlook=\"sunny\"";
			
			Pmml pmml = Pmml.loadModels(pFilePath);
			
			Assert.NotNull(pmml);
			
			TreeModel tree = (TreeModel)pmml.getByName(modelname);
			Assert.NotNull(tree);
			
			// Modification for NullPrediction
			tree.MissingValueStrategy = MissingValueStrategy.NullPrediction;
			
			Dictionary<string, object> lDict = parseParams(paramList);
			
			ScoreResult result = tree.Score(lDict);
			Assert.NotNull(result);
			
			Assert.IsNull(result.Value);
		}
		
		[TestCase("test-golfing1.xml")]
		public void loadTest(string pFilePath)
		{
			Pmml pmml = Pmml.loadModels(pFilePath);
			
			Assert.NotNull(pmml);
			
			TreeModel tree = (TreeModel)pmml.getByName("golfing");
			Assert.NotNull(tree);
			
			// Test first Node
			Node nodeFirst = tree.Node;
			Assert.NotNull(nodeFirst);
			Assert.NotNull(nodeFirst.Predicate);
			Assert.IsTrue(nodeFirst.Predicate is TruePredicate);
			Assert.AreEqual(2, nodeFirst.Nodes.Count);
			
			//
			Assert.AreEqual(2, tree.Node.Nodes[0].Nodes.Count);
			Assert.AreEqual(2, tree.Node.Nodes[1].Nodes.Count);
		}
		
		private Dictionary<string, object> parseParams(string parameters)
		{
			Dictionary<string, object> lDict = new Dictionary<string, object>();
			
			foreach (string item in parameters.Split( new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
			{
				lDict.Add(item.Split('=')[0].Trim(), item.Split('=')[1].Trim().Replace("\"", ""));
			}
			
			return lDict;
		}
		
		[TestCase("test-simpleset.xml", 
		          "in=5", 
		          "1")]
		public void SimpleSetPredicateTest(string pFilePath, string paramList, string res)
		{
			Pmml pmml = Pmml.loadModels(pFilePath);
			
			Assert.NotNull(pmml);
			
			TreeModel tree = (TreeModel)pmml.getByName("SimpleSetTest");
			Assert.NotNull(tree);
			
			Dictionary<string, object> lDict = parseParams(paramList);
			
			ScoreResult result = tree.Score(lDict);
			Assert.NotNull(result);
			
			
			Assert.AreEqual(2, result.Nodes.Count);
			
			Assert.AreEqual(res, result.Value);
			
		}
		
		/// <summary>
		/// Test operator isMissing for a simple predicate
		/// </summary>
		[TestCase()]
		public void SimplePredicateIsMissingTest()
		{
			string pmmlStr = @"<?xml version=""1.0"" ?>
<PMML version=""4.1"" xmlns=""http://www.dmg.org/PMML-4_1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<Header copyright=""www.dmg.org"" description=""A very small tree model to test isMissing operator.""/>
	<DataDictionary numberOfFields=""2"" >
		<DataField name=""in"" optype=""continuous"" dataType=""double""/>
		<DataField name=""out"" optype=""continuous"" dataType=""double""/>
	</DataDictionary>
	<TreeModel modelName=""SimpleIsMissingTest"" functionName=""classification"">
		<MiningSchema>
			<MiningField name=""in""/>
			<MiningField name=""out"" usageType=""predicted""/>
		</MiningSchema>
		<Node score=""0"">
			<True/>
			<Node score=""1"">
				<SimplePredicate field=""in"" operator=""isMissing"" />
			</Node>
		</Node>
	</TreeModel>
</PMML>
";
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(pmmlStr);
			Pmml pmml = Pmml.loadModels(xml);
			
			ScoreResult res = pmml.getByName("SimpleIsMissingTest").Score(new Dictionary<string, object>());
			
			Assert.AreEqual("1", res.Value);
		}
		
		/// <summary>
		/// Test operator isNotMissing for a simple predicate
		/// </summary>
		[TestCase()]
		public void SimplePredicateIsNotMissingTest()
		{
			string pmmlStr = @"<?xml version=""1.0"" ?>
<PMML version=""4.1"" xmlns=""http://www.dmg.org/PMML-4_1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<Header copyright=""www.dmg.org"" description=""A very small tree model to test isNotMissing operator.""/>
	<DataDictionary numberOfFields=""2"" >
		<DataField name=""in"" optype=""continuous"" dataType=""double""/>
		<DataField name=""out"" optype=""continuous"" dataType=""double""/>
	</DataDictionary>
	<TreeModel modelName=""SimpleIsNotMissingTest"" functionName=""classification"">
		<MiningSchema>
			<MiningField name=""in""/>
			<MiningField name=""out"" usageType=""predicted""/>
		</MiningSchema>
		<Node score=""0"">
			<True/>
			<Node score=""1"">
				<SimplePredicate field=""in"" operator=""isNotMissing"" />
			</Node>
		</Node>
	</TreeModel>
</PMML>
";
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(pmmlStr);
			Pmml pmml = Pmml.loadModels(xml);
			
			ScoreResult res = pmml.getByName("SimpleIsNotMissingTest").Score(parseParams("  in=\"foo\"  "));
			
			Assert.AreEqual("1", res.Value);
		}
		
		/// <summary>
		/// Test noTrueChildStrategy (default value).
		/// 
		/// In the following example, scoring reaches N1, but the case to be scored has a value for field prob1 which is 
		/// less than or equal to 0.33, the noTrueChildStrategy defined for the tree determines what action to take.
		/// 
		/// If no value is defined for noTrueChildStrategy then no prediction is returned (this is the default behaviour).
		/// </summary>
		[TestCase()]
		public void NoTrueChildStrategyDefaultTest()
		{
			string pmmlStr = @"<?xml version=""1.0"" ?>
<PMML version=""4.1"" xmlns=""http://www.dmg.org/PMML-4_1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<Header copyright="""" description=""A very small tree model to test noTrueChildStrategy attribute.""/>
	<DataDictionary numberOfFields=""2"" >
		<DataField name=""prob1"" optype=""continuous"" dataType=""double""/>
		<DataField name=""out"" optype=""continuous"" dataType=""double""/>
	</DataDictionary>
	<TreeModel modelName=""Test"" functionName=""classification"">
		<MiningSchema>
			<MiningField name=""prob1""/>
			<MiningField name=""out"" usageType=""predicted""/>
		</MiningSchema>
		<Node id=""N1"" score=""0"">
			<True/>
			<Node id=""T1"" score=""1"">
				<SimplePredicate field=""prob1"" operator=""greaterThan"" value=""0.33"" />
			</Node>
		</Node>
	</TreeModel>
</PMML>
";
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(pmmlStr);
			Pmml pmml = Pmml.loadModels(xml);
			
			TreeModel model = (TreeModel)pmml.getByName("Test");
			
			// Test that default = returnNullPrediction
			Assert.AreEqual(NoTrueChildStrategy.ReturnNullPrediction, model.NoTrueChildStrategy);
			
			ScoreResult res = model.Score(parseParams("  prob1=0.25  "));
			
			Assert.AreEqual(null, res.Value);
		}
		
		/// <summary>
		/// Test noTrueChildStrategy = returnNullPrediction.
		/// 
		/// In the following example, scoring reaches N1, but the case to be scored has a value for field prob1 which is 
		/// less than or equal to 0.33, the noTrueChildStrategy defined for the tree determines what action to take.
		/// 
		/// If returnNullPrediction is defined for noTrueChildStrategy then no prediction is returned.
		/// </summary>
		[TestCase()]
		public void NoTrueChildStrategyReturnNullPredictionTest()
		{
			string pmmlStr = @"<?xml version=""1.0"" ?>
<PMML version=""4.1"" xmlns=""http://www.dmg.org/PMML-4_1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<Header copyright="""" description=""A very small tree model to test noTrueChildStrategy attribute.""/>
	<DataDictionary numberOfFields=""2"" >
		<DataField name=""prob1"" optype=""continuous"" dataType=""double""/>
		<DataField name=""out"" optype=""continuous"" dataType=""double""/>
	</DataDictionary>
	<TreeModel modelName=""Test"" functionName=""classification"" noTrueChildStrategy=""returnNullPrediction"" >
		<MiningSchema>
			<MiningField name=""prob1""/>
			<MiningField name=""out"" usageType=""predicted""/>
		</MiningSchema>
		<Node id=""N1"" score=""0"">
			<True/>
			<Node id=""T1"" score=""1"">
				<SimplePredicate field=""prob1"" operator=""greaterThan"" value=""0.33"" />
			</Node>
		</Node>
	</TreeModel>
</PMML>
";
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(pmmlStr);
			Pmml pmml = Pmml.loadModels(xml);
			
			TreeModel model = (TreeModel)pmml.getByName("Test");
			
			// Test that = returnNullPrediction
			Assert.AreEqual(NoTrueChildStrategy.ReturnNullPrediction, model.NoTrueChildStrategy);
			
			ScoreResult res = model.Score(parseParams("  prob1=0.25  "));
			
			Assert.AreEqual(null, res.Value);
		}
		
		/// <summary>
		/// Test noTrueChildStrategy = returnLastPrediction.
		/// 
		/// In the following example, scoring reaches N1, but the case to be scored has a value for field prob1 which is 
		/// less than or equal to 0.33, the noTrueChildStrategy defined for the tree determines what action to take.
		/// 
		/// If set to returnLastPrediction, then the score of N1 (0) is returned.
		/// </summary>
		[TestCase()]
		public void NoTrueChildStrategyReturnLastPredictionTest()
		{
			string pmmlStr = @"<?xml version=""1.0"" ?>
<PMML version=""4.1"" xmlns=""http://www.dmg.org/PMML-4_1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<Header copyright="""" description=""A very small tree model to test noTrueChildStrategy attribute.""/>
	<DataDictionary numberOfFields=""2"" >
		<DataField name=""prob1"" optype=""continuous"" dataType=""double""/>
		<DataField name=""out"" optype=""continuous"" dataType=""double""/>
	</DataDictionary>
	<TreeModel modelName=""Test"" functionName=""classification"" noTrueChildStrategy=""returnLastPrediction"" >
		<MiningSchema>
			<MiningField name=""prob1""/>
			<MiningField name=""out"" usageType=""predicted""/>
		</MiningSchema>
		<Node id=""N1"" score=""0"">
			<True/>
			<Node id=""T1"" score=""1"">
				<SimplePredicate field=""prob1"" operator=""greaterThan"" value=""0.33"" />
			</Node>
		</Node>
	</TreeModel>
</PMML>
";
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(pmmlStr);
			Pmml pmml = Pmml.loadModels(xml);
			
			TreeModel model = (TreeModel)pmml.getByName("Test");
			
			// Test that = returnNullPrediction
			Assert.AreEqual(NoTrueChildStrategy.ReturnLastPrediction, model.NoTrueChildStrategy);
			
			ScoreResult res = model.Score(parseParams("  prob1=0.25  "));
			
			Assert.AreEqual("0", res.Value);
		}
	}
}
