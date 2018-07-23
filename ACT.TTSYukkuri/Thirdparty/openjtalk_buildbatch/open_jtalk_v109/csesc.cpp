#include <string>
#include <iostream>
int main()
{
	while( !std::cin.eof() )
	{
		std::string line;
		std::getline(std::cin, line);
		for ( int i = 0; i < line.length(); i++ )
		{
			unsigned char ch = line[i];
			printf( (ch >= 128)?"\\x%02X":"%c", ch );
		}
		std::cout << std::endl;
	}
	return 0;
}
