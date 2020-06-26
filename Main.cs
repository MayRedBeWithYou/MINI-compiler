
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Linq.Expressions;
using System.Text;
using System.Linq;
using System.Data;

namespace MINICompiler
{
    public enum NodeType
    {
        None,
        EmptyNode,
        Program,
        Return,
        Block,
        If,
        While,
        Read,
        Write,
        Variable,
        Init,
        Assign,
        Int,
        Double,
        Bool,
        String,
        BinaryOp,
        LogicOp,
        Comparison,
        Parenthesis,
        IntCast,
        DoubleCast,
        Not,
        Minus,
        Neg
    }

    public enum ComparisonType
    {
        None,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    public enum BinaryOpType
    {
        None,
        Add,
        Sub,
        Mult,
        Div,
        BitAnd,
        BitOr
    }

    public enum LogicOpType
    {
        None,
        And,
        Or
    }

    public enum ValType
    {
        None = -1,
        Bool = 0,
        Int = 1,
        Double = 2
    }

    public enum SemanticErrorCode
    {
        None = 0,
        UnexpectedError = 200,
        UndeclaredVariable,
        IllegalCast,
        VariableAlreadyDeclared
    }

    public class SemanticException : Exception
    {
        public SemanticErrorCode Error { private set; get; }

        public int Line { private set; get; }

        public new string Message { private set; get; }

        public SemanticException(SemanticErrorCode code, string msg, int line = -1)
        {
            Error = code;
            Line = line;
            Message = msg;
        }
    }

    public class Compiler
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0) return -1;
            FileStream source = new FileStream(args[0], FileMode.Open);

            ProgramNode ProgramTree = new ProgramNode();

            Scanner scanner = new Scanner(source, ProgramTree);
            Parser parser = new Parser(scanner, ProgramTree);

            bool success = parser.Parse();
            if (!success)
            {
                if (parser.syntaxErrorLines.Count == 0) Console.WriteLine("Unexpected EOF.");
                else
                {
                    Console.WriteLine("Found " + parser.syntaxErrorLines.Count + " syntax errors:");
                    foreach (int line in parser.syntaxErrorLines)
                    {
                        Console.WriteLine("Found SyntaxError in line " + line);
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Parsing ended with code: 100");
                return 100;
            }

            ProgramTreeChecker checker = new ProgramTreeChecker(ProgramTree);
            int result = checker.CheckSemantics();

            Console.WriteLine("Parsing ended with code: " + result);
            if (result == 0)
            {
                using (StreamWriter output = new StreamWriter(args[0] + ".il", false))
                {
                    output.Write(checker.GenerateCode(ProgramTree));
                }
                Console.WriteLine("Created file: " + args[0] + ".il");
            }
            return result;
        }

        public static string ValTypeToString(ValType type)
        {
            switch (type)
            {
                case ValType.None:
                    return "none";
                case ValType.Bool:
                    return "bool";
                case ValType.Int:
                    return "int32";
                case ValType.Double:
                    return "float64";
            }
            return "none";
        }
    }

    public struct LocalVariable
    {
        public string Name { get; set; }
        public ValType Type { get; set; }
    }

    public class ProgramTreeChecker
    {
        public List<LocalVariable> locals = new List<LocalVariable>();

        ProgramNode program;

        public ProgramTreeChecker(ProgramNode node)
        {
            program = node;
        }

        public int CheckSemantics()
        {
            Scope scope = new Scope();
            try
            {
                GoDeeperInScope(program.block, scope);
            }
            catch (SemanticException e)
            {
                Console.WriteLine(Enum.GetName(typeof(SemanticErrorCode), e.Error) + " in line " + e.Line + ":");
                Console.WriteLine(e.Message);
                Console.WriteLine();
                return (int)e.Error;
            }
            return 0;
        }

        private void GoDeeperInScope(BlockNode node, Scope outer)
        {
            Scope scope = new Scope();
            foreach (Node line in node.instructions)
            {
                CheckInScope(line, scope, outer);
            }
        }

        public void CheckInScope(Node node, Scope inner, Scope outer)
        {
            switch (node.GetNodeType())
            {
                case NodeType.Block:
                    Scope newOuter = new Scope(outer);
                    newOuter.AddScope(inner);
                    GoDeeperInScope(node as BlockNode, newOuter);
                    break;
                case NodeType.If:
                    IfNode ifNode = node as IfNode;
                    ValType type = CheckValueType(ifNode.check, inner, outer);
                    if (type == ValType.Bool)
                    {
                        CheckInScope(ifNode.ifBlock, inner, outer);
                        if (!(ifNode.elseBlock is null)) CheckInScope(ifNode.elseBlock, inner, outer);
                    }
                    else
                    {
                        string typeString = Enum.GetName(typeof(ValType), type);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Bool, but got " + typeString + ".", node.Line);
                    }
                    break;
                case NodeType.While:
                    WhileNode whileNode = node as WhileNode;
                    type = CheckValueType(whileNode.check, inner, outer);
                    if (type == ValType.Bool)
                    {
                        CheckInScope(whileNode.block, inner, outer);
                    }
                    else
                    {
                        string typeString = Enum.GetName(typeof(ValType), type);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Bool, but got " + typeString + ".", node.Line);
                    }
                    break;
                case NodeType.Read:
                    ReadNode readNode = node as ReadNode;
                    CheckInScope(readNode.target, inner, outer);
                    break;
                case NodeType.Write:
                    WriteNode writeNode = node as WriteNode;
                    if (!(writeNode.content is StringNode))
                    {
                        CheckInScope(writeNode.content, inner, outer);
                    }
                    break;
                case NodeType.Variable:
                    VariableNode variableNode = node as VariableNode;
                    CheckValueType(variableNode, inner, outer);
                    break;
                case NodeType.Init:
                    InitNode initNode = node as InitNode;
                    if (inner.variables.ContainsKey(initNode.variable.name))
                    {
                        throw new SemanticException(SemanticErrorCode.VariableAlreadyDeclared, "Variable \"" + initNode.variable.name + "\" already declared in scope", initNode.variable.Line);
                    }
                    else inner.variables.Add(initNode.variable.name, initNode.variable);
                    initNode.variable.LocalIndex = locals.Count;
                    locals.Add(new LocalVariable { Name = initNode.variable.name, Type = initNode.variable.ValType });
                    break;
                case NodeType.Assign:
                    AssignNode assignNode = node as AssignNode;
                    CheckValueType(assignNode, inner, outer);
                    break;
                case NodeType.BinaryOp:
                    BinaryOpNode binaryOpNode = node as BinaryOpNode;
                    CheckValueType(binaryOpNode, inner, outer);
                    break;
                case NodeType.Comparison:
                    ComparisonNode comparisonNode = node as ComparisonNode;
                    CheckValueType(comparisonNode, inner, outer);
                    break;
                case NodeType.Parenthesis:
                    ParenthesisNode parenthesisNode = node as ParenthesisNode;
                    CheckInScope(parenthesisNode.content, inner, outer);
                    break;
                case NodeType.Int:
                case NodeType.Double:
                case NodeType.Bool:
                case NodeType.String:
                    break;
                case NodeType.LogicOp:
                    LogicOpNode logicNode = node as LogicOpNode;
                    CheckValueType(logicNode, inner, outer);
                    break;
                case NodeType.IntCast:
                    IntCastNode intCastNode = node as IntCastNode;
                    CheckValueType(intCastNode, inner, outer);
                    break;
                case NodeType.DoubleCast:
                    DoubleCastNode doubleCastNode = node as DoubleCastNode;
                    CheckValueType(doubleCastNode, inner, outer);
                    break;
                case NodeType.Not:
                    NotNode notNode = node as NotNode;
                    CheckValueType(notNode, inner, outer);
                    break;
                case NodeType.Minus:
                    MinusNode minusNode = node as MinusNode;
                    CheckValueType(minusNode, inner, outer);
                    break;
                case NodeType.Neg:
                    NegNode negNode = node as NegNode;
                    CheckValueType(negNode, inner, outer);
                    break;
            }
        }

        public ValType CheckValueType(Node node, Scope inner, Scope outer)
        {
            switch (node.GetNodeType())
            {
                case NodeType.Variable:
                    VariableNode variableNode = node as VariableNode;
                    if (!inner.variables.TryGetValue(variableNode.name, out VariableNode val))
                    {
                        if (!outer.variables.TryGetValue(variableNode.name, out val))
                        {
                            throw new SemanticException(SemanticErrorCode.UndeclaredVariable, "Variable \"" + variableNode.name + "\" is not declared.", node.Line);
                        }
                        else
                        {
                            variableNode.ValType = val.ValType;
                            variableNode.LocalIndex = val.LocalIndex;
                        }
                    }
                    else
                    {
                        variableNode.ValType = val.ValType;
                        variableNode.LocalIndex = val.LocalIndex;
                    }
                    return variableNode.ValType;
                case NodeType.Assign:
                    AssignNode assignNode = node as AssignNode;
                    ValType left = CheckValueType(assignNode.left, inner, outer);
                    ValType right = CheckValueType(assignNode.right, inner, outer);
                    if (assignNode.left.ValType != right && !(assignNode.left.ValType == ValType.Double && right == ValType.Int))
                    {
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Cannot cast " + Enum.GetName(typeof(ValType), right) +
                             " to " + Enum.GetName(typeof(ValType), assignNode.left.ValType) + ".", assignNode.right.Line);
                    }
                    if (right == ValType.Int && assignNode.left.ValType == ValType.Double)
                    {
                        DoubleCastNode dcn = new DoubleCastNode(assignNode.right.Line);
                        dcn.content = assignNode.right;
                        assignNode.right = dcn;
                    }
                    assignNode.ValType = left;
                    return left;
                case NodeType.Int:
                    return ValType.Int;
                case NodeType.Double:
                    return ValType.Double;
                case NodeType.Bool:
                    return ValType.Bool;
                case NodeType.BinaryOp:
                    BinaryOpNode binaryOpNode = node as BinaryOpNode;
                    ValType l = CheckValueType(binaryOpNode.left, inner, outer);
                    ValType r = CheckValueType(binaryOpNode.right, inner, outer);
                    int v = (int)l * (int)r;

                    if (binaryOpNode.type == BinaryOpType.BitAnd || binaryOpNode.type == BinaryOpType.BitOr)
                    {
                        if (v != 1)
                        {
                            throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Int value.", node.Line);
                        }
                        return ValType.Int;
                    }
                    else
                    {
                        if (v > 1 && v < 4)
                        {
                            if (l == ValType.Double)
                            {
                                DoubleCastNode dcn = new DoubleCastNode(binaryOpNode.right.Line);
                                dcn.content = binaryOpNode.right;
                                binaryOpNode.right = dcn;
                            }
                            else
                            {
                                DoubleCastNode dcn = new DoubleCastNode(binaryOpNode.left.Line);
                                dcn.content = binaryOpNode.left;
                                binaryOpNode.left = dcn;
                            }

                            binaryOpNode.ValType = ValType.Double;
                            return ValType.Double;
                        }
                        else if (l == ValType.Bool || r == ValType.Bool)
                        {
                            throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Int or Double.", binaryOpNode.Line);
                        }
                    }
                    binaryOpNode.ValType = l;
                    return l;
                case NodeType.LogicOp:
                    LogicOpNode logicOpNode = node as LogicOpNode;
                    left = CheckValueType(logicOpNode.left, inner, outer);
                    right = CheckValueType(logicOpNode.right, inner, outer);
                    if (left != ValType.Bool)
                    {
                        string typeString = Enum.GetName(typeof(ValType), left);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Bool, but got " + typeString + ".", logicOpNode.left.Line);
                    }

                    if (right != ValType.Bool)
                    {
                        string typeString = Enum.GetName(typeof(ValType), right);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Bool, but got " + typeString + ".", logicOpNode.right.Line);
                    }
                    logicOpNode.ValType = ValType.Bool;
                    return ValType.Bool;
                case NodeType.Comparison:
                    ComparisonNode comparisonNode = node as ComparisonNode;
                    left = CheckValueType(comparisonNode.left, inner, outer);
                    right = CheckValueType(comparisonNode.right, inner, outer);
                    if (left != right)
                    {
                        if (left == ValType.Bool || right == ValType.Bool)
                        {
                            throw new SemanticException(SemanticErrorCode.IllegalCast, "Comparison arguments are not the same type.", comparisonNode.Line);
                        }
                        else if (left == ValType.Int && right == ValType.Double)
                        {
                            DoubleCastNode dcn = new DoubleCastNode(comparisonNode.left.Line);
                            dcn.content = comparisonNode.left;
                            comparisonNode.left = dcn;
                        }
                        else if (right == ValType.Int && left == ValType.Double)
                        {
                            DoubleCastNode dcn = new DoubleCastNode(comparisonNode.right.Line);
                            dcn.content = comparisonNode.right;
                            comparisonNode.right = dcn;
                        }
                    }
                    comparisonNode.ValType = ValType.Bool;
                    return ValType.Bool;
                case NodeType.Parenthesis:
                    ParenthesisNode parenthesisNode = node as ParenthesisNode;
                    ValType type = CheckValueType(parenthesisNode.content, inner, outer);
                    parenthesisNode.ValType = type;
                    return type;
                case NodeType.IntCast:
                    IntCastNode intCastNode = node as IntCastNode;
                    CheckInScope(intCastNode.content, inner, outer);
                    intCastNode.ValType = ValType.Int;
                    return ValType.Int;
                case NodeType.DoubleCast:
                    DoubleCastNode doubleCastNode = node as DoubleCastNode;
                    CheckInScope(doubleCastNode.content, inner, outer);
                    doubleCastNode.ValType = ValType.Double;
                    return ValType.Double;
                case NodeType.Not:
                    NotNode notNode = node as NotNode;
                    type = CheckValueType(notNode.content, inner, outer);
                    if (type != ValType.Bool)
                    {
                        string typeString = Enum.GetName(typeof(ValType), type);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Bool, but got " + typeString + ".", notNode.content.Line);
                    }
                    notNode.ValType = ValType.Bool;
                    return ValType.Bool;
                case NodeType.Minus:
                    MinusNode minusNode = node as MinusNode;
                    type = CheckValueType(minusNode.content, inner, outer);
                    if (type == ValType.Bool)
                    {
                        string typeString = Enum.GetName(typeof(ValType), type);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Int or Double.", minusNode.content.Line);
                    }
                    minusNode.ValType = type;
                    return type;
                case NodeType.Neg:
                    NegNode negNode = node as NegNode;
                    type = CheckValueType(negNode.content, inner, outer);
                    if (type != ValType.Int)
                    {
                        string typeString = Enum.GetName(typeof(ValType), type);
                        throw new SemanticException(SemanticErrorCode.IllegalCast, "Expected Int, but got " + typeString + ".", negNode.content.Line);
                    }
                    negNode.ValType = type;
                    return ValType.Int;
            }
            return ValType.None;
        }

        public string GenerateCode(ProgramNode node)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(".assembly extern mscorlib { }");
            sb.AppendLine(".assembly MINICompiler { }");
            sb.AppendLine(".method public static void Main()");
            sb.AppendLine("{");
            sb.AppendLine(".entrypoint");
            sb.AppendLine(".locals init (");

            for (int i = 0; i < locals.Count; i++)
            {
                sb.Append($"[{i}] ");
                sb.Append(Compiler.ValTypeToString(locals[i].Type));
                sb.Append($" V_{locals[i].Name},");
                sb.AppendLine();
            }
            if (locals.Count != 0) sb.Remove(sb.Length - 3, 1);
            sb.AppendLine(")");
            sb.AppendLine();

            for (int i = 0; i < locals.Count; i++)
            {
                switch (locals[i].Type)
                {
                    case ValType.Bool:
                    case ValType.Int:
                        sb.AppendLine("ldc.i4 0");
                        break;
                    case ValType.Double:
                        sb.AppendLine("ldc.r8 0");
                        break;
                }
                sb.AppendLine("stloc.s " + i);
            }

            sb.Append(node.GenerateCode());

            sb.AppendLine("ret");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public class Scope
    {
        public readonly Dictionary<string, VariableNode> variables = new Dictionary<string, VariableNode>();

        public Scope() { }

        public Scope(Scope outer)
        {
            this.variables = new Dictionary<string, VariableNode>(outer.variables);
        }

        public Scope(Dictionary<string, VariableNode> v)
        {
            this.variables = new Dictionary<string, VariableNode>(v);
        }

        public void AddScope(Scope scope)
        {
            foreach (KeyValuePair<string, VariableNode> pair in scope.variables)
            {
                if (variables.ContainsKey(pair.Key)) variables[pair.Key] = pair.Value;
                else variables.Add(pair.Key, pair.Value);
            }
        }
    }

    public abstract class Node
    {
        public int Line { get; set; } = -1;

        public abstract NodeType GetNodeType();

        public abstract string GenerateCode();

        protected Node(int line)
        {
            this.Line = line;
        }
    }

    public abstract class ExpressionNode : Node
    {
        public ValType ValType { get; set; }

        public bool ShouldReturnValue { get; set; } = true;

        protected ExpressionNode(int line) : base(line)
        {
            this.Line = line;
        }
    }

    public class EmptyNode : Node
    {
        public EmptyNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return "";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.EmptyNode;
        }
    }

    public class ProgramNode : Node
    {
        public BlockNode block;

        public int LineCount { get; set; } = 1;

        public ProgramNode(int line = -1) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return block.GenerateCode();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Program;
        }
    }

    public class ReturnNode : Node
    {
        public ReturnNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return "ret\n";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Return;
        }
    }

    public class BlockNode : Node
    {
        public List<Node> instructions = new List<Node>();

        public BlockNode(int line = -1) : base(line)
        {
        }

        public BlockNode(BlockNode block) : base(block.Line)
        {
            instructions.AddRange(block.instructions);
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Node node in instructions)
            {
                sb.Append(node.GenerateCode());
            }
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Block;
        }
    }

    public class WriteNode : Node
    {
        public Node content;

        public WriteNode(int line = -1) : base(line)
        {
        }

        public override string GenerateCode()
        {
            string type;
            if (content is StringNode) type = "string";
            else type = Compiler.ValTypeToString((content as ExpressionNode).ValType);
            if (type == "float64")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                sb.AppendLine(@"ldstr ""{0:0.000000}""");
                sb.Append(content.GenerateCode());
                sb.AppendLine("box [mscorlib]System.Double");
                sb.AppendLine("call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)");
                sb.AppendLine("call void [mscorlib]System.Console::Write(string)");
                return sb.ToString();
            }
            else return content.GenerateCode() + $"call void [mscorlib]System.Console::Write({type})\n";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Write;
        }
    }

    public class StringNode : Node
    {
        public string text;

        public StringNode(string text, int line) : base(line)
        {
            this.text = text;
        }

        public override string GenerateCode()
        {
            return $"ldstr {text}\n";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.String;
        }
    }

    public class ReadNode : Node
    {
        public VariableNode target;

        public ReadNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("call string [mscorlib]System.Console::ReadLine()");
            switch (target.ValType)
            {
                case ValType.Bool:
                    sb.AppendLine($"call bool [mscorlib]System.Boolean::Parse(string)");
                    break;
                case ValType.Int:
                    sb.AppendLine($"call int32 [mscorlib]System.Int32::Parse(string)");
                    break;
                case ValType.Double:
                    sb.AppendLine($"call float64 [mscorlib]System.Double::Parse(string)");
                    break;
            }
            sb.AppendLine("stloc." + target.LocalIndex);
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Read;
        }
    }

    public class VariableNode : ExpressionNode
    {
        public string name;

        private int localIndex;

        public int LocalIndex
        {
            get => localIndex;
            set => localIndex = value;
        }

        public VariableNode(string name, int line) : base(line)
        {
            this.name = name;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ldloc.s " + LocalIndex);
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Variable;
        }
    }

    public class InitNode : Node
    {
        public VariableNode variable;

        public InitNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return "";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Init;
        }
    }

    public class AssignNode : ExpressionNode
    {
        public VariableNode left;

        public Node right;

        public AssignNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(right.GenerateCode());

            sb.AppendLine("stloc.s " + left.LocalIndex);

            if (ShouldReturnValue) sb.AppendLine("ldloc.s " + left.LocalIndex);


            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Assign;
        }
    }

    public class IntNode : ExpressionNode
    {
        public int value;

        public IntNode(int value, int line) : base(line)
        {
            this.value = value;
            ValType = ValType.Int;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ldc.i4 " + value);
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Int;
        }
    }

    public class DoubleNode : ExpressionNode
    {
        public double value;

        public DoubleNode(double value, int line) : base(line)
        {
            this.value = value;
            ValType = ValType.Double;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ldc.r8 " + value);
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Double;
        }
    }

    public class BoolNode : ExpressionNode
    {
        public bool value;

        public BoolNode(bool value, int line) : base(line)
        {
            this.value = value;
            ValType = ValType.Bool;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ldc.i4." + Convert.ToInt32(value));
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Bool;
        }
    }

    public class ComparisonNode : ExpressionNode
    {
        public ExpressionNode left;

        public ExpressionNode right;

        public ComparisonType type;

        public ComparisonNode(ComparisonType type, int line = -1) : base(line)
        {
            this.type = type;
            ValType = ValType.Bool;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(left.GenerateCode());
            sb.Append(right.GenerateCode());
            switch (type)
            {
                case ComparisonType.Equal:
                    sb.AppendLine("ceq");
                    break;
                case ComparisonType.NotEqual:
                    sb.AppendLine("ceq");
                    sb.AppendLine("ldc.i4.0");
                    sb.AppendLine("ceq");
                    break;
                case ComparisonType.Greater:
                    sb.AppendLine("cgt");
                    break;
                case ComparisonType.GreaterOrEqual:
                    sb.AppendLine("clt");
                    sb.AppendLine("ldc.i4.0");
                    sb.AppendLine("ceq");
                    break;
                case ComparisonType.Less:
                    sb.AppendLine("clt");
                    break;
                case ComparisonType.LessOrEqual:
                    sb.AppendLine("cgt");
                    sb.AppendLine("ldc.i4.0");
                    sb.AppendLine("ceq");
                    break;
            }
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Comparison;
        }
    }

    public class BinaryOpNode : ExpressionNode
    {
        public ExpressionNode left;

        public ExpressionNode right;

        public BinaryOpType type;

        public BinaryOpNode(BinaryOpType type, int line = -1) : base(line)
        {
            this.type = type;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(left.GenerateCode());
            sb.Append(right.GenerateCode());
            switch (type)
            {
                case BinaryOpType.Add:
                    sb.AppendLine("add");
                    break;
                case BinaryOpType.Sub:
                    sb.AppendLine("sub");
                    break;
                case BinaryOpType.Mult:
                    sb.AppendLine("mul");
                    break;
                case BinaryOpType.Div:
                    sb.AppendLine("div");
                    break;
                case BinaryOpType.BitAnd:
                    sb.AppendLine("and");
                    break;
                case BinaryOpType.BitOr:
                    sb.AppendLine("or");
                    break;
            }
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.BinaryOp;
        }
    }

    public class LogicOpNode : ExpressionNode
    {
        private static int id = 0;

        public static int GetUniqueId() { return id++; }

        public Node left;

        public Node right;

        public LogicOpType type;

        public LogicOpNode(LogicOpType type, int line = -1) : base(line)
        {
            this.type = type;
            ValType = ValType.Bool;
        }
        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(left.GenerateCode());

            string shortC = "S_" + GetUniqueId();
            string outC = "O_" + GetUniqueId();

            switch (type)
            {
                case LogicOpType.And:
                    sb.AppendLine("brfalse " + shortC);
                    sb.Append(right.GenerateCode());
                    sb.AppendLine("brfalse " + shortC);
                    sb.AppendLine("ldc.i4.1");
                    sb.AppendLine("br " + outC);
                    sb.AppendLine(shortC + ": ldc.i4.0");
                    sb.AppendLine("br " + outC);
                    break;
                case LogicOpType.Or:
                    sb.AppendLine("brtrue " + shortC);
                    sb.Append(right.GenerateCode());
                    sb.AppendLine("brtrue " + shortC);
                    sb.AppendLine("ldc.i4.0");
                    sb.AppendLine("br " + outC);
                    sb.AppendLine(shortC + ": ldc.i4.1");
                    sb.AppendLine("br " + outC);
                    break;
            }
            sb.AppendLine(outC + ": nop");

            if (!ShouldReturnValue) sb.AppendLine("pop");

            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.LogicOp;
        }
    }

    public class IntCastNode : ExpressionNode
    {
        public Node content;

        public IntCastNode(int line) : base(line)
        {
            ValType = ValType.Int;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(content.GenerateCode());
            sb.AppendLine("conv.i4");
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.IntCast;
        }
    }

    public class DoubleCastNode : ExpressionNode
    {
        public Node content;

        public DoubleCastNode(int line) : base(line)
        {
            ValType = ValType.Double;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(content.GenerateCode());
            sb.AppendLine("conv.r8");
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.DoubleCast;
        }
    }

    public class ParenthesisNode : ExpressionNode
    {
        public ExpressionNode content;

        public ParenthesisNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(content.GenerateCode());
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Parenthesis;
        }
    }

    public class IfNode : Node
    {
        private static int id = 0;

        public static int GetUniqueId() { return id++; }

        public ExpressionNode check;

        public Node ifBlock;

        public Node elseBlock;

        string elseId = "ELSE_" + GetUniqueId();

        string afterId = "AFIF_" + GetUniqueId();

        public IfNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(check.GenerateCode());

            if (elseBlock is null)
            {
                sb.AppendLine("brfalse " + afterId);
                sb.Append(ifBlock.GenerateCode());
            }
            else
            {
                sb.AppendLine("brfalse " + elseId);
                sb.Append(ifBlock.GenerateCode());
                sb.AppendLine("br " + afterId);
                sb.AppendLine(elseId + ": nop");
                sb.Append(elseBlock.GenerateCode());
            }
            sb.AppendLine(afterId + ": nop");

            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.If;
        }
    }

    public class NotNode : ExpressionNode
    {
        public ExpressionNode content;

        public NotNode(int line) : base(line)
        {
            ValType = ValType.Bool;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(content.GenerateCode());
            sb.AppendLine("ldc.i4.0");
            sb.AppendLine("ceq");
            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Not;
        }
    }

    public class MinusNode : ExpressionNode
    {
        public ExpressionNode content;

        public MinusNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(content.GenerateCode() + "neg");

            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Minus;
        }
    }

    public class NegNode : ExpressionNode
    {
        public ExpressionNode content;

        public NegNode(int line) : base(line)
        {
            ValType = ValType.Int;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(content.GenerateCode() + "not");

            if (!ShouldReturnValue) sb.AppendLine("pop");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Neg;
        }
    }

    public class WhileNode : Node
    {
        private static int id = 0;

        public static int GetUniqueId() { return id++; }

        public ExpressionNode check;

        public Node block;

        string checkId = "WC_" + GetUniqueId();

        string blockId = "WB_" + GetUniqueId();

        public WhileNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("br " + checkId);
            sb.AppendLine();
            sb.AppendLine(blockId + ": nop");
            sb.Append(block.GenerateCode());
            sb.AppendLine();
            sb.AppendLine(checkId + ": nop");
            sb.Append(check.GenerateCode());
            sb.AppendLine("brtrue " + blockId);

            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.While;
        }
    }
}