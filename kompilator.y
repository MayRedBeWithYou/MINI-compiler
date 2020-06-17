
%using MINICompiler;
%namespace GardensPoint

%YYSTYPE Node

%right Assign
%left Or And
%left Comparison
%left Add Sub
%left Mult Div
%left BitOr BitAnd
%right Tilde Not IntCast DoubleCast

%token Program Write Read If Else While Return
%token OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Eof Error
%token Int Double Bool
%token Variable IntVal DoubleVal BoolVal String

%%
start			: Program { ProgramTree.line = line; } block Eof
				  {					
					ProgramTree.block = blockStack.Pop();
				  }
				;
block			: OpenBracket
				  {
					BlockNode node = new BlockNode(line);
					blockStack.Push(node);
					$$ = node;
				  }
				  lines
				  CloseBracket
				;
lines			: 
				| instruction { blockStack.Peek().instructions.Add($1);} lines
				;
instruction		: init Semicolon
				| assign Semicolon
				| write Semicolon
				| read Semicolon
				| exp Semicolon
				| if
				| while
				;

write			: Write String 
				{
					WriteNode node = new WriteNode(line);
					node.content = $2;
					$$ = node;
				}
				| Write exp 
				{
					WriteNode node = new WriteNode(line);
					node.content = $2;
					$$ = node;
				}
				;
read			: Read Variable
				{
					ReadNode node = new ReadNode(line);
					node.target = $2 as VariableNode;
					$$ = node;
				}
				;
init			: Int Variable 
				{
					InitNode node = new InitNode(line);
					node.variable = $2 as VariableNode;
					node.variable.type = ValType.Int;
					$$ = node;
				}
				| Double Variable 
				{
					InitNode node = new InitNode(line);
					node.variable = $2 as VariableNode;
					node.variable.type = ValType.Double;
					$$ = node;
					}
				| Bool Variable 
				{
					InitNode node = new InitNode(line);
					node.variable = $2 as VariableNode;
					node.variable.type = ValType.Bool;
					$$ = node;
				}
				;
assign			: Variable Assign exp 
				{
					AssignNode node = new AssignNode(line);
					node.left = $1 as VariableNode;
					node.right = $3;
					$$ = node;
				} 
				;

exp				: OpenPar exp ClosePar 
				{
					ParenthesisNode node = new ParenthesisNode(line);
					node.content = $2;
					$$ = node;
				} 
				| exp Add exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| exp Sub exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| exp Mult exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| exp Div exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| exp BitAnd exp 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| exp BitOr exp
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					node.line = line;
					$$ = AssignToBinaryOp(node, $1 as Node, $3 as Node); 
				}
				| Variable
				| IntCast exp
				{
					IntCastNode node = new IntCastNode(line);
					node.content = $2;
					$$ = node;
				}
				| DoubleCast exp
				{
					DoubleCastNode node = new DoubleCastNode(line);
					node.content = $2;
					$$ = node;
				}				
				| IntVal
				| DoubleVal
				| BoolVal
				| Not exp
				{
					NotNode node = new NotNode(line);
					node.content = $2;
					$$ = node;
				}
				| Tilde exp
				{
					NegNode node = new NegNode(line);
					node.content = $2;
					$$ = node;
				}
				| Sub exp
				{
					MinusNode node = new MinusNode(line);
					node.content = $2;
					$$ = node;
				}
				| exp And exp
				{
					LogicOpNode node = new LogicOpNode(LogicOpType.And, line);
					node.left = $1;
					node.right = $3;
					$$ = node;
				}
				| exp Or exp
				{
					LogicOpNode node = new LogicOpNode(LogicOpType.Or, line);
					node.left = $1;
					node.right = $3;
					$$ = node;
				}
				| exp Comparison exp 
				{
					ComparisonNode node = $2 as ComparisonNode;
					node.line = line;
					$$ = AssignToComparisonOp(node, $1 as Node, $3 as Node); 
				}
				;

if				: If OpenPar exp ClosePar block Else block
				{
					IfNode node = new IfNode(line);
					node.check = $3;
					node.elseBlock = blockStack.Pop();
					node.ifBlock = blockStack.Pop();
					$$ = node;
				}
				| If OpenPar exp ClosePar block
				{
					IfNode node = new IfNode(line);
					node.check = $3;
					node.ifBlock = blockStack.Pop();
					$$ = node;
				}
				;
while			: While OpenPar exp ClosePar block
				{
					WhileNode node = new WhileNode(line);
					node.check = $3 as ComparisonNode;
					node.block = blockStack.Pop();
					$$ = node;
				}
				;
				
%%

public int line {get => ProgramTree.lineCount; }

public ProgramNode ProgramTree;

public Stack<BlockNode> blockStack = new Stack<BlockNode>();

public Parser(Scanner scanner, ProgramNode node) : base(scanner)
{
	ProgramTree = node;
}

public BinaryOpNode AssignToBinaryOp(BinaryOpNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	return node;
}

public ComparisonNode AssignToComparisonOp(ComparisonNode node, Node left, Node right)
{
	node.line = line;
	node.left = left;
	node.right = right;
	return node;
}