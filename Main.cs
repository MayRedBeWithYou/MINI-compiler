
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Linq.Expressions;

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
        DoubleCast
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
        None,
        Bool,
        Int,
        Double
    }

    public enum ErrorCode
    {
        UnexpectedError = -1,
        UndeclaredVariable = -2,
        IllegalCast = -3,
    }

    public class Compiler
    {
        public static ProgramNode ProgramTree = new ProgramNode();

        public static int Main(string[] args)
        {
            if (args.Length == 0) return -1;
            FileStream source = new FileStream(args[0], FileMode.Open);
            Scanner scanner = new Scanner(source);
            Parser parser = new Parser(scanner);
            parser.Parse();
            Console.ReadKey();
            return 0;
        }
    }

    public class ProgramTreeChecker
    {
        ProgramNode program;

        public ProgramTreeChecker(ProgramNode node)
        {
            program = node;
        }

        public int CheckSemantics()
        {
            Scope scope = new Scope();
            return GoDeeperInScope(program.block, scope);
        }

        public int CheckInScope(Node node, Scope scope, Scope outer)
        {
            switch (node.getType())
            {
                case NodeType.Block:
                    Scope newOuter = new Scope(outer);
                    newOuter.AddScope(scope);
                    GoDeeperInScope(node as BlockNode, newOuter);
                    break;
                case NodeType.If:
                    IfNode n = node as IfNode;

                    break;
                case NodeType.While:
                    break;
                case NodeType.Read:
                    ReadNode readNode = node as ReadNode;
                    return CheckInScope(readNode.target, scope, outer);
                    break;
                case NodeType.Write:
                    WriteNode writeNode = node as WriteNode;
                    if (!(writeNode.content is StringNode))
                    {                        
                        return CheckInScope(writeNode.content, scope, outer);
                    }
                    break;
                case NodeType.Variable:
                    break;
                case NodeType.Init:
                    InitNode initNode = node as InitNode;
                    if (scope.variables.ContainsKey(initNode.variable.name)) throw new Exception();
                    else scope.variables.Add(initNode.variable.name, initNode.variable.type);
                    break;
                case NodeType.Assign:
                    AssignNode assignNode = node as AssignNode;
                    if (scope.variables.TryGetValue(assignNode.left.name, out ValType val))
                    {
                        if (val < CheckValueType(assignNode.right, scope, outer))
                        {
                            return (int)ErrorCode.IllegalCast;
                        }
                    }
                    else if (outer.variables.TryGetValue(assignNode.left.name, out val))
                    {
                        if (val < CheckValueType(assignNode.right, scope, outer))
                        {
                            return (int)ErrorCode.IllegalCast;
                        }
                    }
                    else return (int)ErrorCode.UndeclaredVariable;
                    break;
                case NodeType.Int:
                case NodeType.Double:
                case NodeType.Bool:
                case NodeType.String:
                case NodeType.BinaryOp:
                    break;
                case NodeType.Comparison:
                    break;
                case NodeType.Parenthesis:
                    break;
            }
            return 0;
        }

        public ValType CheckValueType(Node node, Scope scope, Scope outer)
        {
            return ValType.Bool;
        }

        private int GoDeeperInScope(BlockNode node, Scope outer)
        {
            Scope scope = new Scope();
            foreach (Node line in node.instructions)
            {
                int result = CheckInScope(line, scope, outer);
                if (result != 0) return result;
            }
            return 0;
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

        public abstract NodeType getType();

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
            throw new NotImplementedException();
        }

        public override NodeType getType()
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

        public override NodeType getType()
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
            return $"call\tvoid [mscorlib]System.Console::WriteLine({content.getType()})\n";
        }

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
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

        public override NodeType getType()
        {
            return NodeType.If;
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

        public override NodeType getType()
        {
            return NodeType.While;
        }
    }
}