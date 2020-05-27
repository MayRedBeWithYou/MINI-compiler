
%using MINICompiler;
%namespace GardensPoint

%union
{
public Node node;
}

%token Program Write Assign OpenBracket CloseBracket Semicolon Endl Eof Error
%token <val> Ident Int Double Bool String

%%
start			: Program Endl {inc(); Console.WriteLine("Program found");}
				  OpenBracket Endl { inc(); Console.WriteLine("Opening bracket");}
				  lines
				  CloseBracket { inc(); Console.WriteLine("Closing bracket");} Eof {Console.WriteLine("End of file. Lines: " + line);}
				;
lines			: {Console.WriteLine("No more lines");}
				| {inc(); Console.WriteLine("Found line.");} line lines
				;
line			: {Console.WriteLine("Found write.");} Write String Semicolon Endl
				| Endl
				;

%%

public int line=0;

public ProgramNode root;

public Node current;

public Parser(Scanner scanner) : base(scanner) { }

public void inc() { line++;}