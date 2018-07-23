#using <System.dll>
using namespace System;
using namespace System::Text;
using namespace System::IO;

int main(array<System::String ^> ^args)
{
	TextReader^ input = Console::In;
	String^ charset;

	if (args->Length != 1)
	{
		return -1;
	}

	String^ sw = args[0]->ToLower();

	if (String::Compare(sw, "-utf-8") == 0) {
		charset = "UTF-8";
	}else if (String::Compare(sw, "-utf_8") == 0) {
		charset = "UTF-8";
	}else if (String::Compare(sw, "-utf8") == 0) {
		charset = "UTF-8";
	}else if (String::Compare(sw, "-euc-jp") == 0) {
		charset = "EUC-JP";
	}else if (String::Compare(sw, "-euc_jp") == 0) {
		charset = "EUC-JP";
	}else if (String::Compare(sw, "-eucjp") == 0) {
		charset = "EUC-JP";
	}else if (String::Compare(sw, "-euc") == 0) {
		charset = "EUC-JP";
	}else if (String::Compare(sw, "-shift_jis") == 0) {
		charset = "SHIFT_JIS";
	}else if (String::Compare(sw, "-shiftjis") == 0) {
		charset = "SHIFT_JIS";
	}else if (String::Compare(sw, "-sjis") == 0) {
		charset = "SHIFT_JIS";
	}else if (String::Compare(sw, "-shift-jis") == 0) {
		charset = "SHIFT_JIS";
	} else {
		Console::Error->Write("引数が-UTF_8と -EUC_JP のとき、その文字セットに変換します。");
		Console::Error->Write("-SHIFT_JISの場合は変換せずに出力します。");
		return -1;
	}

	Encoding^ src = Encoding::Unicode;
	Encoding^ dst = Encoding::GetEncoding( charset );
	Stream^ output = Console::OpenStandardOutput();
	String^ line;

	while ((line = input->ReadLine()) != nullptr)
	{
		array<Byte>^temp = Encoding::Convert(src, dst, src->GetBytes(line + Environment::NewLine));
		output->Write(temp, 0, temp->Length);
	}
	return 0;
}

