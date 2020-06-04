
%using MINICompiler;
%namespace GardensPoint

%union
{
public Node node;
}

%right Assign
%left And Or
%left Comparison
%left Add Sub
%left Mult Div
%left BitOr BitAnd
%right Minus Tilde Not IntCast DoubleCast


%token Program Write Read If Else While Return
%token Endl OpenPar ClosePar OpenBracket CloseBracket Semicolon
%token Eof Error
%token Int Double Bool
%token <node> Variable IntVal DoubleVal BoolVal String

%%
start			: Program Endl 
					{
						inc();
						Console.WriteLine("Program found.");
						root = new ProgramNode();
						current = root;
					}
					block
					Eof {Console.WriteLine("End of file. Lines: " + line);}
				;
block			: OpenBracket Endl { inc(); Console.WriteLine("Opening block.");}
				  lines
				  CloseBracket { inc(); Console.WriteLine("Closing block.");}
				;
lines			: {Console.WriteLine("No more lines");}
				| line Endl {inc(); } lines
				| line lines
				| Endl {inc(); } lines
				;
line			: init Semicolon
				| assign Semicolon
				| write Semicolon
				;
write			: Write String {Console.WriteLine("Found write of string.");}
				| Write exp {Console.WriteLine("Found write of expression.");}
				;
init			: Int Variable {Console.WriteLine("Found int init.");}
				| Double Variable {Console.WriteLine("Found double init.");}
				| Bool Variable {Console.WriteLine("Found bool init.");}
				;
assign			: Variable Assign exp {Console.WriteLine("Found assignment.");} 
				;
exp				: exp Add exp
				| exp Sub exp
				| exp Mult exp
				| exp Div exp
				| exp Comparison exp
				| Variable {Console.WriteLine("Found variable.");}
				| IntVal {Console.WriteLine("Found int value.");}
				| DoubleVal {Console.WriteLine("Found double value.");}
				| BoolVal {Console.WriteLine("Found bool value.");}
				;
				
%%

public int line=0;

public ProgramNode root;

public Node current;

public Parser(Scanner scanner) : base(scanner) { }

public void inc() { line++;}