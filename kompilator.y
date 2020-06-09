
%using MINICompiler;
%namespace GardensPoint

%YYSTYPE Node

%right Assign
%left And Or
%left Comparison
%left Add Sub
%left Mult Div
%left BitOr BitAnd
%right Minus Tilde Not IntCast DoubleCast

%token Program Write Read If Else While Return
%token OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Eof Error
%token Int Double Bool
%token Variable IntVal DoubleVal BoolVal String

%%
start			: Program { Compiler.ProgramTree.line = line; } block Eof
				  {					
					Compiler.ProgramTree.block = blockStack.Pop();
					Console.WriteLine("EOF. Lines: " + line);
				  }
				;
block			: OpenBracket
				  {
					BlockNode node = new BlockNode(line);
					blockStack.Push(node);
					Console.WriteLine("Open block.");
					$$ = node;
				  }
				  lines
				  CloseBracket
				  {
					Console.WriteLine("Close block.");
				  }
				;
lines			: 
				| instruction { blockStack.Peek().instructions.Add($1);} lines
				;
instruction		: init Semicolon
				| assign Semicolon
				| write Semicolon
				| read Semicolon
				| exp Semicolon
				| bool Semicolon
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
parExp			: OpenPar exp ClosePar 
				{
					ParenthesisNode node = new ParenthesisNode(line);
					node.content = $2;
					$$ = node;
				} 
				;

exp				: parExp
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
				| cast
				| BoolVal
				;

cast			: IntCast parExp
				{
					IntCastNode node = new IntCastNode(line);
					node.content = $2;
					$$ = node;
				}
				| DoubleCast parExp
				{
					DoubleCastNode node = new DoubleCastNode(line);
					node.content = $2;
					$$ = node;
				}
				| IntCast Variable
				{
					IntCastNode node = new IntCastNode(line);
					node.content = $2;
					$$ = node;
				}
				| DoubleCast Variable
				{
					DoubleCastNode node = new DoubleCastNode(line);
					node.content = $2;
					$$ = node;
				}
				;

comp			: exp Comparison exp 
				{
					ComparisonNode node = $2 as ComparisonNode;
					node.line = line;
					$$ = AssignToComparisonOp(node, $1 as Node, $3 as Node); 
				}
				;

bool			: comp
				| BoolVal
				| Variable
				| Not bool
				| bool And bool
				{
					LogicOpNode node = new LogicOpNode(LogicOpType.And, line);
					node.left = $1;
					node.right = $3;
					$$ = node;
				}
				| bool Or bool
				{
					LogicOpNode node = new LogicOpNode(LogicOpType.Or, line);
					node.left = $1;
					node.right = $3;
					$$ = node;
				}
				| OpenPar bool ClosePar
				{
					ParenthesisNode node = new ParenthesisNode(line);
					node.content = $2;
					$$ = node;
				}
				;
if				: If OpenPar bool ClosePar block Else block
				{
					IfNode node = new IfNode(line);
					node.check = $3;
					node.elseBlock = blockStack.Pop();
					node.ifBlock = blockStack.Pop();
					$$ = node;
				}
				| If OpenPar bool ClosePar block
				{
					IfNode node = new IfNode(line);
					node.check = $3;
					node.ifBlock = blockStack.Pop();
					$$ = node;
				}
				;
while			: While OpenPar comp ClosePar block
				{
					WhileNode node = new WhileNode(line);
					node.check = $3 as ComparisonNode;
					node.block = blockStack.Pop();
					$$ = node;
				}
				;
				
%%

public int line {get => Compiler.ProgramTree.lineCount; }

public Stack<BlockNode> blockStack = new Stack<BlockNode>();

public Node current;

public Parser(Scanner scanner) : base(scanner) { }

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