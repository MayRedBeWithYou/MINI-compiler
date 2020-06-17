
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Linq.Expressions;
using System.Text;

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

    public class Compiler
    {
        public static ProgramNode ProgramTree = new ProgramNode();

        public static int Main(string[] args)
        {
            if (args.Length == 0) return -1;
            FileStream source = new FileStream(args[0], FileMode.Open);
            Scanner scanner = new Scanner(source);
            Parser parser = new Parser(scanner, ProgramTree);
            parser.Parse();
            ProgramTreeChecker checker = new ProgramTreeChecker(ProgramTree);
            int result = checker.CheckSemantics();
            Console.WriteLine("Parsing ended with code: " + result);
            if(result == 0)
            {
                using (StreamWriter output = new StreamWriter(args[0] + ".il", false))
                {
                    output.Write(GenerateCode(ProgramTree, checker.locals));
                }
                Console.WriteLine("Created file: " + args[0] + ".il");
            }
            return result;
        }

        public static string GenerateCode(ProgramNode node, List<(string name, ValType type)> variables)
        {
            StringBuilder sb = new StringBuilder(".method private hidebysig static void  Main(string[] args) cil managed");
            sb.AppendLine();
            sb.AppendLine("{");
            sb.AppendLine(".entrypoint");
            sb.AppendLine(".locals init (");

            for(int i = 0; i<variables.Count;i++)
            {
                sb.Append($"[{i}] ");
                switch (variables[i].type)
                {
                    case ValType.Int:
                        sb.Append("int32");
                        break;
                    case ValType.Double:
                        sb.Append("float64");
                        break;
                    case ValType.Bool:
                        sb.Append("bool");
                        break;
                }
                sb.Append($" {variables[i].name},");
                sb.AppendLine();
            }
            sb.Remove(sb.Length - 3, 1);
            sb.AppendLine(")");

            // Parse the tree again

            sb.AppendLine("ret");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public class ProgramTreeChecker
    {
        public List<(string name, ValType type)> locals = new List<(string name, ValType type)>();

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
                Console.WriteLine("Found error: " + Enum.GetName(typeof(ErrorCode), e.Error) + " in line " + e.Line);
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

        public void CheckInScope(Node node, Scope scope, Scope outer)
        {
            switch (node.GetNodeType())
            {
                case NodeType.Block:
                    Scope newOuter = new Scope(outer);
                    newOuter.AddScope(scope);
                    GoDeeperInScope(node as BlockNode, newOuter);
                    break;
                case NodeType.If:
                    IfNode ifNode = node as IfNode;
                    if (CheckValueType(ifNode.check, scope, outer) == ValType.Bool)
                    {
                        CheckInScope(ifNode.ifBlock, scope, outer);
                        if (!(ifNode.elseBlock is null)) CheckInScope(ifNode.elseBlock, scope, outer);
                    }
                    else throw new SemanticException(ErrorCode.IllegalCast, node.line);
                    break;
                case NodeType.While:
                    WhileNode whileNode = node as WhileNode;
                    if (CheckValueType(whileNode.check, scope, outer) == ValType.Bool)
                    {
                        CheckInScope(whileNode.block, scope, outer);
                    }
                    else throw new SemanticException(ErrorCode.IllegalCast, node.line);
                    break;
                case NodeType.Read:
                    ReadNode readNode = node as ReadNode;
                    CheckInScope(readNode.target, scope, outer);
                    break;
                case NodeType.Write:
                    WriteNode writeNode = node as WriteNode;
                    if (!(writeNode.content is StringNode))
                    {
                        CheckInScope(writeNode.content, scope, outer);
                    }
                    break;
                case NodeType.Variable:
                    VariableNode variableNode = node as VariableNode;
                    if (!scope.variables.TryGetValue(variableNode.name, out ValType val))
                    {
                        if (!outer.variables.TryGetValue(variableNode.name, out val))
                        {
                            throw new SemanticException(ErrorCode.UndeclaredVariable, node.line);
                        }
                        else variableNode.type = val;
                    }
                    else variableNode.type = val;
                    break;
                case NodeType.Init:
                    InitNode initNode = node as InitNode;
                    if (scope.variables.ContainsKey(initNode.variable.name)) throw new SemanticException(ErrorCode.UndeclaredVariable, initNode.variable.line);
                    else scope.variables.Add(initNode.variable.name, initNode.variable.type);
                    locals.Add((initNode.variable.name, initNode.variable.type));
                    break;
                case NodeType.Assign:
                    AssignNode assignNode = node as AssignNode;
                    if (scope.variables.TryGetValue(assignNode.left.name, out val))
                    {
                        if (val < CheckValueType(assignNode.right, scope, outer))
                        {
                            throw new SemanticException(ErrorCode.IllegalCast, assignNode.right.line);
                        }
                    }
                    else if (outer.variables.TryGetValue(assignNode.left.name, out val))
                    {
                        if (val < CheckValueType(assignNode.right, scope, outer))
                        {
                            throw new SemanticException(ErrorCode.IllegalCast, assignNode.right.line);
                        }
                    }
                    else throw new SemanticException(ErrorCode.UndeclaredVariable, assignNode.left.line);
                    break;
                case NodeType.BinaryOp:
                    BinaryOpNode binaryOpNode = node as BinaryOpNode;
                    CheckValueType(binaryOpNode, scope, outer);
                    break;
                case NodeType.Comparison:
                    ComparisonNode comparisonNode = node as ComparisonNode;
                    CheckValueType(comparisonNode, scope, outer);
                    break;
                case NodeType.Parenthesis:
                    ParenthesisNode parenthesisNode = node as ParenthesisNode;
                    CheckInScope(parenthesisNode.content, scope, outer);
                    break;
                case NodeType.Int:
                case NodeType.Double:
                case NodeType.Bool:
                case NodeType.String:
                    break;
                case NodeType.LogicOp:
                    LogicOpNode logicNode = node as LogicOpNode;
                    if (CheckValueType(logicNode.left, scope, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, logicNode.left.line);

                    if (CheckValueType(logicNode.right, scope, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, logicNode.right.line);
                    break;
                case NodeType.IntCast:
                    IntCastNode intCastNode = node as IntCastNode;
                    CheckValueType(intCastNode, scope, outer);
                    break;
                case NodeType.DoubleCast:
                    DoubleCastNode doubleCastNode = node as DoubleCastNode;
                    CheckValueType(doubleCastNode, scope, outer);
                    break;
                case NodeType.Not:
                    NotNode notNode = node as NotNode;
                    if (CheckValueType(notNode.content, scope, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, notNode.content.line);
                    break;
                case NodeType.Minus:
                    MinusNode minusNode = node as MinusNode;
                    if (CheckValueType(minusNode.content, scope, outer) < ValType.Int)
                        throw new SemanticException(ErrorCode.IllegalCast, minusNode.content.line);
                    break;
                case NodeType.Neg:
                    NegNode negNode = node as NegNode;
                    if (CheckValueType(negNode.content, scope, outer) != ValType.Bool)
                        throw new SemanticException(ErrorCode.IllegalCast, negNode.content.line);
                    break;
            }
        }

        public ValType CheckValueType(Node node, Scope scope, Scope outer)
        {
            switch (node.GetNodeType())
            {
                case NodeType.Variable:
                    VariableNode variableNode = node as VariableNode;
                    if (scope.variables.TryGetValue(variableNode.name, out ValType val))
                    {
                        variableNode.type = val;
                        return val;
                    }
                    else if (outer.variables.TryGetValue(variableNode.name, out val))
                    {
                        variableNode.type = val;
                        return val;
                    }
                    else throw new SemanticException(ErrorCode.UndeclaredVariable, variableNode.line);
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
                    val = CheckValueType(minusNode.content, scope, outer);
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
    }

    public class Scope
    {
        public Dictionary<string, ValType> variables = new Dictionary<string, ValType>();

        public Scope() { }

        public Scope(Scope outer)
        {
            this.variables = new Dictionary<string, ValType>(outer.variables);
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

        public abstract NodeType GetNodeType();

        public abstract string GenerateCode();

        protected Node(int line)
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
            return "";
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
            throw new NotImplementedException();
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
            return $"call\tvoid [mscorlib]System.Console::WriteLine({content.GetNodeType()})\n";
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
            throw new NotImplementedException();
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
            string result = $"call string [mscorlib]System.Console::ReadLine()\n";
            switch (target.type)
            {
                case ValType.Bool:
                    result += $"call bool [mscorlib]System.Boolean::Parse(string)";
                    break;
                case ValType.Int:
                    result += $"call int32 [mscorlib]System.Int32::Parse(string)";
                    break;
                case ValType.Double:
                    result += $"call float64 [mscorlib]System.Double::Parse(string)";
                    break;
            }
            return result;
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Read;
        }
    }

    public class VariableNode : Node
    {
        public string name;

        public ValType type;

        public VariableNode(string name, int line) : base(line)
        {
            this.name = name;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Init;
        }
    }

    public class AssignNode : Node
    {
        public VariableNode left;

        public Node right;

        public AssignNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Assign;
        }
    }

    public class IntNode : Node
    {
        public int value;

        public IntNode(int value, int line) : base(line)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Int;
        }
    }

    public class DoubleNode : Node
    {
        public double value;

        public DoubleNode(double value, int line) : base(line)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Double;
        }
    }

    public class BoolNode : Node
    {
        public bool value;

        public BoolNode(bool value, int line) : base(line)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Bool;
        }
    }

    public class ComparisonNode : Node
    {
        public Node left;

        public Node right;

        public ComparisonType type;

        public ComparisonNode(ComparisonType type, int line = -1) : base(line)
        {
            this.type = type;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Comparison;
        }
    }

    public class BinaryOpNode : Node
    {
        public Node left;

        public Node right;

        public BinaryOpType type;

        public BinaryOpNode(BinaryOpType type, int line = -1) : base(line)
        {
            this.type = type;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.BinaryOp;
        }
    }

    public class LogicOpNode : Node
    {
        public Node left;

        public Node right;

        public LogicOpType type;

        public LogicOpNode(LogicOpType type, int line = -1) : base(line)
        {
            this.type = type;
        }
        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.LogicOp;
        }
    }

    public class IntCastNode : Node
    {
        public Node content;

        public IntCastNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.IntCast;
        }
    }

    public class DoubleCastNode : Node
    {
        public Node content;

        public DoubleCastNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.DoubleCast;
        }
    }

    public class ParenthesisNode : Node
    {
        public Node content;

        public ParenthesisNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Parenthesis;
        }
    }

    public class IfNode : Node
    {
        public Node check;

        public BlockNode ifBlock;

        public BlockNode elseBlock;

        public IfNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.If;
        }
    }

    public class NotNode : Node
    {
        public Node content;

        public NotNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Not;
        }
    }

    public class MinusNode : Node
    {
        public Node content;

        public MinusNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Minus;
        }
    }

    public class NegNode : Node
    {
        public Node content;

        public NegNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.Minus;
        }
    }

    public class WhileNode : Node
    {
        public Node check;

        public BlockNode block;

        public WhileNode(int line) : base(line)
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override NodeType GetNodeType()
        {
            return NodeType.While;
        }
    }
}