# ReedSolomonCodes
Reed-Solomon codes implementation. Reed–Solomon codes operate on a block of data treated as a set of finite field elements called symbols. Reed–Solomon codes are able to detect and correct multiple symbol errors.

# How to use

If you just need to encode and decode data, use the extension code.

To encode an array of bytes of any size in standard blocks of 255 bytes with the ability to recover 8 bytes in each block:

```C#
byte[] data = new byte[2000]; // any size
// ...
byte[] bytes = ReedSolomon255X239.Encode(data);
``` 

Оbtaining initial data:

```C#
byte[] initialData = ReedSolomon255X239.Decode(bytes);
``` 

Use ReedSolomonExtensions.EncodeBlock and ReedSolomonExtensions.DecodeBlock to encode and decode a single block with non-standard code.

Use ReedSolomonExtensions.EncodeBlocks and ReedSolomonExtensions.DecodeBlocks to encode and decode a set of blocks with non-standard code.

## How it works

To perform operations of encoding and decoding information you need to create the ReedSolomonCode class using one of the standard constructors or a special constructor:

```C#
ReedSolomonCode rs = ReedSolomon.Create255X239();
``` 

## Information block

Information (set of symbols) must be submitted as array of integer of type 'int':

```C#
int[] inputSymbols = new int[rs.CodewordLength - rs.ParitySymbolsNumber];
``` 

## Getting Reed-Solomon code for information block

To get the recovery data you need to call the Encode method:

```C#
int[] paritySymbols = rs.Encode(inputSymbols);
``` 

## Concatenation of parity symbols and information symbols

The code block is obtained by concatenating parity symbols with information symbols:

```C#
int[] package = new int[rs.CodewordLength];
Array.Copy(inputSymbols, 0, package, 0, rs.CodewordLength - rs.ParitySymbolsNumber);
Array.Copy(rs.Encode(inputSymbols), 0, package, rs.CodewordLength - rs.ParitySymbolsNumber, rs.ParitySymbolsNumber);
``` 

## Correcting a corrupted code block

To get the initial information you need to call the Decode method:

```C#
int[] decoded = rs.Decode(package, null);
``` 

If the returned value is null then the information cannot be recovered.

## Example

```C#
byte[] source = {1, 2, 3};
var rs = ReedSolomon.Create255X239(false);
// adding data length before data
int sourceLength = source.Length;
byte[] bytes = new byte[rs.CodewordLength - rs.ParitySymbolsNumber];
bytes[0] = (byte) sourceLength;
Array.Copy(source, 0, bytes, 1, sourceLength);
// adding redundancy
bytes = rs.EncodeBlock(bytes);
// distortion of data
bytes[0] = bytes[1] = bytes[2] = 0;
// data recovery
bytes = rs.DecodeBlock(bytes);
// formation of initial data
byte[] destination = new byte[bytes[0]];
Array.Copy(bytes, 1, destination, 0, destination.Length);
``` 
