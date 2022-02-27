#ifndef _BASE64_H_
#define _BASE64_H_
#include <string>
#include <vector>
#include <stdexcept>
#include <cstdint>

// https://github.com/mvorbrodt/blog/blob/master/src/base64.hpp
// https://en.wikibooks.org/wiki/Algorithm_Implementation/Miscellaneous/Base64#C++

namespace base64
{
	static const char kEncodeLookup[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
	static const char kPadCharacter = '=';

	using byte = std::uint8_t;

	std::wstring encode(const std::vector<byte>& input);
	std::vector<byte> decode(const std::wstring& input);
}
#endif
