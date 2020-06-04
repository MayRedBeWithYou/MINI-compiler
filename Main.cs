
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;

namespace MINICompiler
{
    public class Compiler
    {
        public static int Main()
        {
            FileStream source = new FileStream("test.txt", FileMode.Open);
            Scanner scanner = new Scanner(source);
            Parser parser = new Parser(scanner);
            parser.Parse();
            Console.ReadKey();
            return 0;
        }
    }

    public abstract class Node
    {
        public int line = -1;

        public abstract string getType();

        public abstract string GenerateCode();
    }

    public class ProgramNode : Node
    {
        public BlockNode child;

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class BlockNode : Node
    {
        public List<Node> lines;

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class WriteNode : Node
    {
        public Node content;

        public override string GenerateCode()
        {
            return $"call\tvoid [mscorlib]System.Console::WriteLine({content.getType()})\n";
        }

        public override string getType()
        {
            return "write";
        }
    }

    public class StringNode : Node
    {
        public string text;

        public StringNode(string text)
        {
            this.text = text;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class ReadNode : Node
    {
        public VariableNode assignTo;
        public override string GenerateCode()
        {
            string result = $"call string [mscorlib]System.Console::ReadLine()\n";
            if (assignTo.getType() == "int")
                result += $"call int32 [mscorlib]System.Int32::Parse(string)";
            else if (assignTo.getType() == "float64")
                result += $"call float64 [mscorlib]System.Double::Parse(string)";
            else if (assignTo.getType() == "bool")
                result += $"call bool [mscorlib]System.Boolean::Parse(string)";
            return result;
        }

        public override string getType()
        {
            return "read";
        }
    }

    public class VariableNode : Node
    {
        public string name;

        public string type;

        public VariableNode(string name)
        {
            this.name = name;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class AssignNode : Node
    {
        public VariableNode left;

        public Node right;

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class IntNode : Node
    {
        public int value;

        public IntNode(int value)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleNode : Node
    {
        public double value;

        public DoubleNode(double value)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class BoolNode : Node
    {
        public bool value;

        public BoolNode(bool value)
        {
            this.value = value;
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class ExpressionNode : Node
    {
        public ExpressionNode()
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
    }

    public class ComparisonNode : Node
    {
        public ExpressionNode left;

        public ExpressionNode right;

        public ComparisonType type;

        public ComparisonNode()
        {
        }

        public override string GenerateCode()
        {
            throw new NotImplementedException();
        }

        public override string getType()
        {
            throw new NotImplementedException();
        }
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
}