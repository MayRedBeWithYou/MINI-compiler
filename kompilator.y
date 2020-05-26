%namespace GardensPoint

%union
{
public string  val;
public char    type;
}

%token start

%%

start : {
		}
		;
%%