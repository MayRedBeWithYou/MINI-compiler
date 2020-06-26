
%using MINICompiler;
%namespace GardensPoint

%YYSTYPE Node

%token Assign Or And Comparison Add Sub Mult Div BitOr BitAnd Tilde Not IntCast DoubleCast

%token Program Write Read If Else While Return
%token OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Int Double Bool
%token Variable IntVal DoubleVal BoolVal String

%%
start			: Program block EOF
				  {
					if(syntaxErrorLines.Count != 0)
					{
						YYABORT;
					}
					ProgramTree.block = $2 as BlockNode;
					ProgramTree.Line = $1.Line;
				  }
				;
block			: OpenBracket
				  lines
				  CloseBracket
				  {
					BlockNode node;
					if($2 is null) node = new BlockNode();
					else node = new BlockNode($2 as BlockNode);
					node.Line = $1.Line;
					$$ = node;
				  }
				;
lines			: 
				| lines instruction
				{
					BlockNode node;
					if($1 is null) node = new BlockNode();
					else node = new BlockNode($1 as BlockNode);
					node.instructions.Add($2);
					$$ = node;
				}
				| EOF
				{			
					syntaxErrorLines.Add(ProgramTree.LineCount);
					YYABORT;
				}
				;
instruction		: init Semicolon
				| Return Semicolon
				| write Semicolon
				| read Semicolon
				| exp Semicolon
				{
					ExpressionNode node = $1 as ExpressionNode;
					node.ShouldReturnValue = false;
				}
				| if
				| while
				| block
				| Semicolon
				{
					syntaxErrorLines.Add($1.Line);
				}
				| error
				{
					syntaxErrorLines.Add($1.Line);
				}
				;

write			: Write String 
				{
					WriteNode node = new WriteNode($1.Line);
					node.content = $2;
					$$ = node;
				}
				| Write exp 
				{
					WriteNode node = new WriteNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				;
read			: Read Variable
				{
					ReadNode node = new ReadNode($1.Line);
					node.target = $2 as VariableNode;
					$$ = node;
				}
				;
init			: Int Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Int;
					$$ = node;

				}
				| Double Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Double;
					$$ = node;
					}
				| Bool Variable 
				{
					InitNode node = new InitNode($1.Line);
					node.variable = $2 as VariableNode;
					node.variable.ValType = ValType.Bool;
					$$ = node;
				}
				;

exp				: priority7
				;

priority7		: priority6
				| Variable Assign exp 
				{
					AssignNode node = new AssignNode($1.Line);
					node.left = $1 as VariableNode;
					node.right = $3;
					node.ShouldReturnValue = true;
					$$ = node;
				}
				;

priority6		: priority5
				| priority6 And priority5
				{
					LogicOpNode node = $2 as LogicOpNode;
					node.left = $1 as ExpressionNode;
					node.right = $3 as ExpressionNode;
					$$ = node;
				}
				| priority6 Or priority5
				{
					LogicOpNode node = $2 as LogicOpNode;
					node.left = $1 as ExpressionNode;
					node.right = $3 as ExpressionNode;
					$$ = node;
				}
				;
priority5		: priority4
				| priority5 Comparison priority4 
				{
					ComparisonNode node = $2 as ComparisonNode;
					$$ = AssignToComparisonOp(node, $1 as ExpressionNode, $3 as ExpressionNode);
				}
				;

priority4		: priority3
				| priority4 Add priority3
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| priority4 Sub priority3
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				;

priority3		: priority2
				| priority3 Mult priority2 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| priority3 Div priority2 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				;

priority2		: priority1
				| priority2 BitAnd priority1 
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				| priority2 BitOr priority1
				{
					BinaryOpNode node = $2 as BinaryOpNode;
					$$ = AssignToBinaryOp(node, $1 as ExpressionNode, $3 as ExpressionNode); 
				}
				;
priority1		: priority0
				|
				OpenPar priority7 ClosePar 
				{
					ParenthesisNode node = new ParenthesisNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| IntCast priority1
				{
					IntCastNode node = new IntCastNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| DoubleCast priority1
				{
					DoubleCastNode node = new DoubleCastNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| Not priority1
				{
					NotNode node = new NotNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| Tilde priority1
				{
					NegNode node = new NegNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				| Sub priority1
				{
					MinusNode node = new MinusNode($1.Line);
					node.content = $2 as ExpressionNode;
					$$ = node;
				}
				;

priority0		: Variable				
				| IntVal
				| DoubleVal
				| BoolVal
				;

if				: If OpenPar exp ClosePar instruction
				{
					IfNode node = new IfNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.ifBlock = $5;
					$$ = node;
				}
				| If OpenPar exp ClosePar instruction Else instruction
				{
					IfNode node = new IfNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.elseBlock = $7;
					node.ifBlock = $5;
					$$ = node;
				}				
				;
while			: While OpenPar exp ClosePar instruction
				{
					WhileNode node = new WhileNode($1.Line);
					node.check = $3 as ExpressionNode;
					node.block = $5;
					$$ = node;
				}
				;
				
%%

public ProgramNode ProgramTree;

public HashSet<int> syntaxErrorLines = new HashSet<int>();

public Parser(Scanner scanner, ProgramNode node) : base(scanner)
{
	ProgramTree = node;
}

public BinaryOpNode AssignToBinaryOp(BinaryOpNode node, ExpressionNode left, ExpressionNode right)
{
	node.left = left;
	node.right = right;
	return node;
}

public ComparisonNode AssignToComparisonOp(ComparisonNode node, ExpressionNode left, ExpressionNode right)
{
	node.left = left;
	node.right = right;
	return node;
}