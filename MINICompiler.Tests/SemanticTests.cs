using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GardensPoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MINICompiler.Tests
{
    [TestClass]
    public class SemanticTests
    {
        List<String> files;

        [TestInitialize]
        public void Init()
        {
            files = Directory.GetFiles("D:\\MINICompiler\\MINICompiler.Tests\\TestFiles").ToList();
        }

        private Parser PrepareFile(int index, ProgramNode node)
        {
            FileStream source = new FileStream(files[index], FileMode.Open);
            Scanner scanner = new Scanner(source, node);
            Parser parser = new Parser(scanner, node);
            return parser;
        }

        [TestMethod]
        public void EmptyProgram()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(0, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void BrodkaExample()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(1, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void CoveredInScope()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(2, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void UndeclaredVariableInIfCondition()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(3, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual((int)SemanticErrorCode.UndeclaredVariable, result);
        }

        [TestMethod]
        public void UndeclaredVariableInBlock()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(4, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual((int)SemanticErrorCode.UndeclaredVariable, result);
        }

        [TestMethod]
        public void UndeclaredVariableInOuterScope()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(5, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual((int)SemanticErrorCode.UndeclaredVariable, result);
        }

        [TestMethod]
        public void IllegalCast_IntToBool()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(6, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual((int)SemanticErrorCode.IllegalCast, result);
        }

        [TestMethod]
        public void IfWithInstruction()
        {
            ProgramNode node = new ProgramNode();
            Parser parser = PrepareFile(7, node);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(node);
            int result = checker.CheckSemantics();
            Assert.AreEqual(0, result);
        }
    }
}
