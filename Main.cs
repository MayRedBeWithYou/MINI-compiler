
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Linq.Expressions;
using System.Text;
using System.Linq;

namespace MINICompiler
{
    public enum NodeType
    {
        Program,
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
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    public enum BinaryOpType
    {
        Add,
        Sub,
        Mult,
        Div,
        BitAnd,
        BitOr
    }

    public enum LogicOpType
    {
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

    public enum ErrorCode
    {
        None = 0,
        UnexpectedError = 1,
        UndeclaredVariable = 2,
        IllegalCast = 3,
        VariableAlreadyDeclared = 4
    }

    public class SemanticException : Exception
    {
        public ErrorCode Error { private set; get; }

        public int Line { private set; get; } = -1;

        public SemanticException(ErrorCode code, int line)
        {
            Error = code;
            Line = line;
        }
    }

    public static class Tools
    {
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

    public class Compiler
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0) return -1;
            FileStream source = new FileStream(args[0], FileMode.Open);

            ProgramNode ProgramTree = new ProgramNode();

            Scanner scanner = new Scanner(source, ProgramTree);
            Parser parser = new Parser(scanner, ProgramTree);
            parser.Parse();

            CodeChecker checker = new CodeChecker(ProgramTree);
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
    }

    public struct LocalVariable
    {
        public string Name { get; set; }
        public ValType Type { get; set; }
        public int Depth { get; set; }
    }

    public class CodeChecker
    {
        public List<LocalVariable> locals = new List<LocalVariable>();

        ProgramNode program;

        private static CodeChecker instance;

        public static CodeChecker Instance => instance;


        public CodeChecker(ProgramNode node)
        {
            program = node;
            instance = this;
        }

        public int CheckSemantics()
        {
            Scope scope = new Scope(0);
            try
            {
                GoDeeperInScope(program.block, scope);
            }
            catch (SemanticException e)
            {
                Console.WriteLine("Found error: " + Enum.GetName(typeof(ErrorCode), e.Error) + " in line " + e.Line);
                return (int)e.Error;
            }
            return 0;
        }

        private void GoDeeperInScope(BlockNode node, Scope outer)
        {
            Scope scope = new Scope(outer.Depth + 1);
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
                    Scope newOuter = new Scope(outer, outer.Depth);
                    newOuter.AddScope(inner);
                    GoDeeperInScope(node as BlockNode, newOuter);
                    break;
                case NodeType.If:
                    IfNode ifNode = node as IfNode;
                    if (CheckValueType(ifNode.check, inner, outer) == ValType.Bool)
                    {
                        CheckInScope(ifNode.ifBlock, inner, outer);
                        if (!(ifNode.elseBlock is null)) CheckInScope(ifNode.elseBlock, inner, outer);
                    }
                    else throw new SemanticException(ErrorCode.IllegalCast, node.line);
                    break;
                case NodeType.While:
                    WhileNode whileNode = node as WhileNode;
                    if (CheckValueType(whileNode.check, inner, outer) == ValType.Bool)
                    {
                        CheckInScope(whileNode.block, inner, outer);
                    }
                    else throw new SemanticException(ErrorCode.IllegalCast, node.line);
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
                    if (!inner.variables.TryGetValue(variableNode.name, out ValType val))
                    {
                        if (!outer.variables.TryGetValue(variableNode.name, out val))
                        {
                            throw new SemanticException(ErrorCode.UndeclaredVariable, node.line);
                        }
                        else variableNode.valType = val;
                    }
                    else variableNode.valType = val;
                    LocalVariable v = locals.Where(x => x.Name == variableNode.name && x.Type == variableNode.valType
                                            && x.Depth <= inner.Depth).OrderByDescending(x => x.Depth).First();
                    variableNode.LocalIndex = locals.IndexOf(v);
                    break;
                case NodeType.Init:
                    InitNode initNode = node as InitNode;
                    if (inner.variables.ContainsKey(initNode.variable.name)) throw new SemanticException(ErrorCode.VariableAlreadyDeclared, initNode.variable.line);
                    else inner.variables.Add(initNode.variable.name, initNode.variable.valType);
                    initNode.variable.LocalIndex = locals.Count;
                    locals.Add(new LocalVariable { Name = initNode.variable.name, Type = initNode.variable.valType, Depth = inner.Depth });
                    break;
                case NodeType.Assign:
                    AssignNode assignNode = node as AssignNode;
                    CheckInScope(assignNode.left, inner, outer);
                    ValType right = CheckValueType(assignNode.right, inner, outer);
                    if (assignNode.left.valType != right && !(assignNode.left.valType == ValType.Double && right == ValType.Int))
                    {
                        throw new SemanticException(ErrorCode.IllegalCast, assignNode.right.line);
                    }

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
                    if (CheckValueType(logicNode.left, inner, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, logicNode.left.line);

                    if (CheckValueType(logicNode.right, inner, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, logicNode.right.line);
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
                    if (CheckValueType(notNode.content, inner, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, notNode.content.line);
                    break;
                case NodeType.Minus:
                    MinusNode minusNode = node as MinusNode;
                    if (CheckValueType(minusNode.content, inner, outer) < ValType.Int)
                        throw new SemanticException(ErrorCode.IllegalCast, minusNode.content.line);
                    break;
                case NodeType.Neg:
                    NegNode negNode = node as NegNode;
                    if (CheckValueType(negNode.content, inner, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, negNode.content.line);
                    break;
            }
        }

        public ValType CheckValueType(Node node, Scope scope, Scope outer)
        {
            switch (node.GetNodeType())
            {
                case NodeType.Variable:
                    CheckInScope(node, scope, outer);
                    VariableNode variableNode = node as VariableNode;
                    return variableNode.valType;
                case NodeType.Int:
                    return ValType.Int;
                case NodeType.Double:
                    return ValType.Double;
                case NodeType.Bool:
                    return ValType.Bool;
                case NodeType.BinaryOp:
                    BinaryOpNode binaryOpNode = node as BinaryOpNode;
                    if (binaryOpNode.type == BinaryOpType.BitAnd || binaryOpNode.type == BinaryOpType.BitOr)
                    {
                        return ValType.Int;
                    }
                    else
                    {
                        if ((int)CheckValueType(binaryOpNode.left, scope, outer) * (int)CheckValueType(binaryOpNode.right, scope, outer) > 1)
                        {
                            return ValType.Double;
                        }
                        else return ValType.Int;
                    }
                case NodeType.LogicOp:
                    LogicOpNode logicOpNode = node as LogicOpNode;
                    if ((int)CheckValueType(logicOpNode.left, scope, outer) * (int)CheckValueType(logicOpNode.right, scope, outer) > 0)
                        throw new SemanticException(ErrorCode.IllegalCast, logicOpNode.line);
                    return ValType.Bool;
                case NodeType.Comparison:
                    ComparisonNode comparisonNode = node as ComparisonNode;
                    CheckInScope(comparisonNode.left, scope, outer);
                    CheckInScope(comparisonNode.right, scope, outer);
                    return ValType.Bool;
                case NodeType.Parenthesis:
                    ParenthesisNode parenthesisNode = node as ParenthesisNode;
                    return CheckValueType(parenthesisNode.content, scope, outer);
                case NodeType.IntCast:
                    IntCastNode intCastNode = node as IntCastNode;
                    CheckInScope(intCastNode.content, scope, outer);
                    return ValType.Int;
                case NodeType.DoubleCast:
                    DoubleCastNode doubleCastNode = node as DoubleCastNode;
                    CheckInScope(doubleCastNode.content, scope, outer);
                    return ValType.Int;
                case NodeType.Not:
                    NotNode notNode = node as NotNode;
                    if (CheckValueType(notNode.content, scope, outer) != ValType.Bool) throw new SemanticException(ErrorCode.IllegalCast, notNode.content.line);
                    return ValType.Bool;
                case NodeType.Minus:
                    MinusNode minusNode = node as MinusNode;
                    ValType val = CheckValueType(minusNode.content, scope, outer);
                    if (val == ValType.Bool)
                    {
                        return ValType.Int;
                    }
                    else return val;
                case NodeType.Neg:
                    NegNode negNode = node as NegNode;
                    if (CheckValueType(negNode.content, scope, outer) != ValType.Int) throw new SemanticException(ErrorCode.IllegalCast, negNode.content.line);
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
                sb.Append(Tools.ValTypeToString(locals[i].Type));
                sb.Append($" {locals[i].Name},");
                sb.AppendLine();
            }
            sb.Remove(sb.Length - 3, 1);
            sb.AppendLine(")");
            sb.AppendLine();

            // Parse the tree again
            sb.Append(node.GenerateCode());

            sb.AppendLine("ret");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public class Scope
    {
        public readonly Dictionary<string, ValType> variables = new Dictionary<string, ValType>();

        public int Depth { get; set; }

        public Scope(int depth)
        {
            Depth = depth;
        }

        public Scope(Scope outer, int depth)
        {
            this.variables = new Dictionary<string, ValType>(outer.variables);
            this.Depth = depth;
        }

        public Scope(Dictionary<string, ValType> v)
        {
            this.variables = new Dictionary<string, ValType>(v);
        }

        public void AddScope(Scope scope)
        {
            foreach (KeyValuePair<string, ValType> pair in scope.variables)
            {
                variables.Add(pair.Key, pair.Value);
            }
        }
    }

    public abstract class Node
    {
        public int line = -1;

        public int depth = 1;

        public abstract NodeType GetNodeType();

        public abstract string GenerateCode();

        protected Node(int line)
        {
            this.line = line;
        }
    }

    public abstract class ExpressionNode : Node
    {
        public ValType valType;

        public string GetValueTypeString()
        {
            switch (valType)
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

        protected ExpressionNode(int line) : base(line)
        {
            this.line = line;
        }
    }

    public class ProgramNode : Node
    {
        public BlockNode block;

        public int lineCount = 1;

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

    public class BlockNode : Node
    {
        public List<Node> instructions = new List<Node>();

        public BlockNode(int line) : base(line)
        {
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
            else type = (content as ExpressionNode).GetValueTypeString();
            if (type == "float64")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                sb.AppendLine(@"ldstr ""{ 0:0.000000}""");
                sb.Append(content.GenerateCode());
                sb.AppendLine("box [mscorlib]System.Double");
                sb.AppendLine("call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)");
                sb.AppendLine("call void [mscorlib]System.Console::Write(string)");
                return sb.ToString();
            }
            else return content.GenerateCode() + $"call\tvoid [mscorlib]System.Console::Write({type})\n";
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
            switch (target.valType)
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
            return "ldloc.s " + LocalIndex + "\n";
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

    public class AssignNode : Node
    {
        public VariableNode left;

        public ExpressionNode right;

        public AssignNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return right.GenerateCode() + "stloc.s " + left.LocalIndex + "\n";
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
            valType = ValType.Int;
        }

        public override string GenerateCode()
        {
            return "ldc.i4.s " + value + "\n";
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
            valType = ValType.Double;
        }

        public override string GenerateCode()
        {
            return "ldc.r8 " + value + "\n";
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
            valType = ValType.Bool;
        }

        public override string GenerateCode()
        {
            return "ldc.i4." + Convert.ToInt32(value) + "\n";
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
            valType = ValType.Bool;
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
            valType = ValType.Bool;
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
            valType = ValType.Int;
        }

        public override string GenerateCode()
        {
            return content.GenerateCode() + "conv.i4\n";
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
            valType = ValType.Double;
        }

        public override string GenerateCode()
        {
            return content.GenerateCode() + "conv.r8\n";
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
            return content.GenerateCode();
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
            valType = ValType.Bool;
        }

        public override string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(content.GenerateCode());
            sb.AppendLine("ldc.i4.0");
            sb.AppendLine("ceq");
            return sb.ToString();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Not;
        }
    }

    public class MinusNode : Node
    {
        public ExpressionNode content;

        public MinusNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            return content.GenerateCode() + "neg\n";
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
            valType = ValType.Int;
        }

        public override string GenerateCode()
        {
            return content.GenerateCode() + "not\n";
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Minus;
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