using System;

namespace ReedSolomonCodes
{
    public class ReedSolomonCode
    {
        public bool TurnOverBits { get; }
        public int BitsNumberInSymbol { get; }
        public int SymbolBitsMask { get; }

        public int CorrectableSymbolsNumber { get; }
        public int ParitySymbolsNumber { get; }
        public int GalyaFieldGeneratingPolynomial { get; }
        public int CodewordLength { get; }

        private readonly int bufferSize;
        private readonly int[] _valueToIndex;
        private readonly int[] _indexToValue;
        private readonly int[] _generatingPolynomial;

        private readonly int[] galyaFieldPolynoms =
        {
            //  MM=3 poly x3+x+1=1011=0xB
            //  MM=8 poly x8+x4+x3+x2+1=0x11D
            0, 0x3, 0x7, 0xB, 0x13, 0x25, 0x43, 0x89, 0x11D, 0x211
        };

        internal ReedSolomonCode(bool turnOverBits = true, int bitsNumberInSymbol = 8, int correctableSymbolsNumber = 8)
        {
            if (bitsNumberInSymbol > 9)
                throw new ArgumentException("bitsNumberInSymbol must be less than or equal 8", "bitsNumberInSymbol");

            if (correctableSymbolsNumber < 2)
                throw new ArgumentException("correctableSymbolsNumber must be greater than 1", "correctableSymbolsNumber"); 

            TurnOverBits = turnOverBits;
            BitsNumberInSymbol = bitsNumberInSymbol;
            SymbolBitsMask = GenerateMask(BitsNumberInSymbol);
            bufferSize = 1 << BitsNumberInSymbol;
            CorrectableSymbolsNumber = correctableSymbolsNumber;
            ParitySymbolsNumber = 2 * CorrectableSymbolsNumber;
            GalyaFieldGeneratingPolynomial = galyaFieldPolynoms[BitsNumberInSymbol];
            CodewordLength = bufferSize - 1;

            _valueToIndex = new int[bufferSize];
            _indexToValue = new int[bufferSize];
            _generatingPolynomial = new int[bufferSize];

            _valueToIndex[0] = CodewordLength;
            _indexToValue[CodewordLength] = 0;
            int sr = 1;
            int i;
            for (i = 0; i < CodewordLength; i++)
            {
                int val = TurnOverBits ? BitsTurnOver(sr, BitsNumberInSymbol) : sr;
                _valueToIndex[val] = i;
                _indexToValue[i] = val;
                sr <<= 1;
                if ((sr & (1 << BitsNumberInSymbol)) != 0)
                {
                    sr = Add(sr, GalyaFieldGeneratingPolynomial);
                }
                sr &= CodewordLength;
            }

            int[] gpoly1 = new int[bufferSize];
            int[] gpoly2 = new int[bufferSize];
            for (i = 0; i < bufferSize; i++)
            {
                _generatingPolynomial[i] = gpoly1[i] = gpoly2[i] = 0;
            }

            gpoly1[0] = _indexToValue[0];

            for (int j = 0; j < ParitySymbolsNumber; j++)
            {
                gpoly2[0] = _indexToValue[0];
                //  ( a^0*x + a^(j+1) )
                gpoly2[1] = _indexToValue[j + (TurnOverBits ? 0 : 1)];

                int resultDegree = PolyMul(j,
                    gpoly1, 1, gpoly2, _generatingPolynomial);

                for (i = 0; i < resultDegree + 1; i++)
                {
                    gpoly1[i] = _generatingPolynomial[i];
                }
            }
        }

        private static int BitsTurnOver(int s, int bitsNumber)
        {
            int b = 0;
            for (int bit = 0; bit < bitsNumber; bit++)
                b |= (((s >> bit) & 0x1) << ((bitsNumber - 1) - bit));
            return b;
        }

        private static int GenerateMask(int length)
        {
            int mask = 1;
            for (int i = 1; i < length; i++)
            {
                mask = (mask << 1) + 1;
            }
            return mask;
        }

        private int Mul(int a, int b)
        {
            return ((a == 0) || (b == 0)) ? 0 : _indexToValue[(_valueToIndex[a] + _valueToIndex[b]) % CodewordLength];
        }

        private int Div(int a, int b)
        {
            return ((a == 0) || (b == 0)) ? 0 : _indexToValue[(CodewordLength + _valueToIndex[a] - _valueToIndex[b]) % CodewordLength];
        }

        private int Add(int a, int b)
        {
            return (a ^ b);
        }

        private int PolyMul(int degree1, int[] poly1, int degree2, int[] poly2, int[] respoly)
        {
            int resdegree = degree1 + degree2;

            for (int i = 0; i < resdegree + 1; i++)
            {
                respoly[i] = 0;
            }

            for (int i = 0; i < degree2 + 1; i++)
            {
                for (int j = 0; j < degree1 + 1; j++)
                {
                    respoly[i + j] = Add(respoly[i + j], Mul( poly1[j], poly2[i] ) );
                }
            }

            return resdegree;
        }

        //private int PolyDiv(int degree1, int[] poly1, int degree2, int[] poly2, int[] respoly)
        //{
        //    int resdegree = (degree2 - 1);

        //    for (int i = 0; i < degree1 + 1; i++)
        //    {
        //        respoly[i] = poly1[i];
        //    }

        //    for (int j = 0; j < (degree1 - degree2 + 1); j++)
        //    {
        //        int div = Div(respoly[j], poly2[0]);
        //        for (int i = 0; i < degree2 + 1; i++)
        //        {
        //            respoly[i + j] = Add(respoly[i + j], Mul(div, poly2[i]));
        //        }
        //    }

        //    return resdegree;
        //}

        /// <remarks>
        /// dataInput
        /// +----------------------------------------------+
        /// |                                              |
        /// +----------------------------------------------+
        /// |.--- CodewordLength - ParitySymbolsNumber ---.|
        ///
        /// output
        /// +-----------------------------+
        /// |                             |
        /// +-----------------------------+
        /// |.--- ParitySymbolsNumber ---.|
        /// </remarks>
        public int[] Encode(int[] dataInput)
        {
            int[] rsOutput = new int[ParitySymbolsNumber];
            return Encode(dataInput, rsOutput, 0) ? rsOutput : null;
        }

        public bool Encode(int[] dataInput, int[] dataOutput, int dataOutputIndex)
        {
            if ((dataInput == null) ||
                (dataInput.Length != (CodewordLength - ParitySymbolsNumber))) return false;

            int dataLength = dataInput.Length;
            int i, j;
            int[] nPolyRes = new int[bufferSize];
            int indexInput = 0;
            int indexEnc = (CodewordLength - ParitySymbolsNumber) - dataLength;
            for (i = 0; i < dataLength; i++)
            {
                nPolyRes[indexEnc] = dataInput[indexInput] & SymbolBitsMask;
                indexEnc += 1;
                indexInput += 1;
            }

            int degree2 = ParitySymbolsNumber + 1;
            int degree1 = CodewordLength - ParitySymbolsNumber;

            int indexRes = 0;
            int indexGenPoly = 0;
            for (j = 0; j < degree1; j++)
            {
                int div = Div(nPolyRes[indexRes], _generatingPolynomial[indexGenPoly]);
                if (div != 0)
                {
                    int indexRes2 = indexRes;
                    int indexGenPoly2 = indexGenPoly;
                    for (i = 0; i < degree2; i++)
                    {
                        nPolyRes[indexRes2] = Add(nPolyRes[indexRes2], Mul(div, _generatingPolynomial[indexGenPoly2]));
                        indexRes2 += 1;
                        indexGenPoly2 += 1;
                    }
                }
                indexRes += 1;
            }

            degree2 -= 1;
            for (i = 0; i < degree2; i++)
            {
                dataOutput[dataOutputIndex + i] = nPolyRes[degree1 + i];
            }

            return true;
        }

        /// <remarks>
        /// dataInput                  RS
        /// +-----------------------*------+
        /// |                       |      |   -   source package
        /// +-----------------------*------+
        /// |.------ CodewordLength ------.|
        ///
        /// output                     RS
        /// +-----------------------*------+
        /// |                       |      |   -   corrected package
        /// +-----------------------*------+
        /// |.------ CodewordLength ------.|
        /// </remarks>
        public int[] Decode(int[] dataInput, int[] distortionIndexes)
        {
            //  Parameter check
            if ((dataInput == null) ||
                (dataInput.Length != CodewordLength)) return null;

            int[] dataOutput = new int[CodewordLength];
            int dataLength = dataInput.Length;
            int distortionNumber = (distortionIndexes != null) && (distortionIndexes.Length <= ParitySymbolsNumber) ? distortionIndexes.Length : 0;

            int i, j, k;
            int tc;

            // Decode buffer
            int[] nPolyDec = new int[bufferSize];

            //  Clearing the buffer
            for (i = 0; i < CodewordLength; i++) nPolyDec[i] = 0;

            //  Copying information to buffer
            int x = CodewordLength - dataLength;
            for (i = 0; i < dataLength; i++)
            {
                nPolyDec[x + i] = dataInput[i] & SymbolBitsMask;
            }

            int clear; //  Zero Syndrome Flag
            int posErr = 0; //  number of errors

            int[] syndr = new int[ParitySymbolsNumber]; //  Array of syndromes
            int[] syndc = new int[ParitySymbolsNumber]; //  copy of syndromes
            int[] syndrMod = new int[ParitySymbolsNumber]; //  Modified syndromes
            int[] locatore = new int[ParitySymbolsNumber + 1]; //  Error locator
            int[] locatorc = new int[ParitySymbolsNumber + 1]; //  Erase locator
            int[] p1 = new int[ParitySymbolsNumber + 1];
            int[] p2 = new int[ParitySymbolsNumber + 1];
            int[] position = new int[bufferSize]; //  Positions
            int[] poseInd = new int[ParitySymbolsNumber]; //  error positions
            int[] possInd = new int[ParitySymbolsNumber]; //  erase positions
            int[] geInd = new int[ParitySymbolsNumber]; //  error locator roots
            int[] gsInd = new int[ParitySymbolsNumber]; //  erasure locator roots
            int[] error = new int[ParitySymbolsNumber]; //  error values
            int[] polyerr = new int[bufferSize]; //  error polynomial

            //  Syndrome matrix
            int[,] matrSyndr = new int[ParitySymbolsNumber, ParitySymbolsNumber + 1];

            //  Error matrix
            int[,] matrErr = new int[ParitySymbolsNumber, ParitySymbolsNumber + 1];

            for (i = 0; i < bufferSize; i++) polyerr[i] = 0;
            for (i = 0; i < ParitySymbolsNumber; i++)
            {
                locatore[i] = locatorc[i] =
                    p1[i] = p2[i] = poseInd[i] = possInd[i] =
                        syndr[i] = syndc[i] = syndrMod[i] = 0;
                gsInd[i] = geInd[i] = (CodewordLength - 1);
            }

            var ts = ((TurnOverBits == false) ? 1 : 0);
            for (clear = 1, i = 0; i < ParitySymbolsNumber; i++)
            {
                for (j = 0; j < CodewordLength; j++)
                {
                    //  !! The compiler will optimize itself !!
                    //x = _indexToValue[ ( ( i + ts ) * ( CodewordLength - j - 1 ) ) % CodewordLength ];
                    int xz = (i + ts) * (CodewordLength - j - 1);

                    while (xz >= CodewordLength) xz -= CodewordLength;

                    x = _indexToValue[xz];

                    //syndr[ i ] = Add( syndr[ i ], Mul( x, nPolyDec[ j ] ) );
                    //syndr[ i ] ^= Mul( x, nPolyDec[ j ] );
                    if ((x == 0) || (nPolyDec[j] == 0)) x = 0;
                    else
                    {
                        k = _valueToIndex[x] + _valueToIndex[nPolyDec[j]];
                        if (k >= CodewordLength) k -= CodewordLength;
                        x = _indexToValue[k];
                    }

                    syndr[i] ^= x;
                }

                syndc[i] = syndr[i];
                if (syndr[i] != 0) clear = 0;
            }

            //  We use erasure information
            if (distortionNumber != 0)
            {
                //  We take positions from the input array of positions
                for (i = 0; i < distortionNumber; i++)
                {
                    possInd[i] = ((CodewordLength - dataLength) + distortionIndexes[i]) & SymbolBitsMask;
                    gsInd[i] = ((CodewordLength - 1) - possInd[i]) & SymbolBitsMask;
                }

                //  Determining the polynomial of erasure locators
                p1[0] = _indexToValue[0]; //  The first polynomial is empty - а0
                for (j = 0; j < distortionNumber; j++) //  Multiplying by the number of brackets
                {
                    p2[0] = _indexToValue[0];
                    p2[1] = _indexToValue[gsInd[j]];
                    var rdegree = PolyMul(j,
                        p1, 1, p2, locatorc); //  Resulting degree
                    //  Copying the result for further multiplication
                    for (i = 0; i < rdegree + 1; i++)
                    {
                        p1[i] = locatorc[i];
                    }
                }

                //  Only if erasures are less than 2 * CorrectableSymbolsNumber - 1
                if (distortionNumber < (2 * CorrectableSymbolsNumber - 1))
                {
                    //  Identifying modified syndromes
                    for (i = 0; i < (2 * CorrectableSymbolsNumber - distortionNumber); i++)
                    {
                        for (j = 0; j < distortionNumber + 1; j++)
                        {
                            syndrMod[i] = (syndrMod[i] ^ Mul(
                                syndr[i + j], locatorc[distortionNumber - j]));
                        }
                    }

                    for (i = 0; i < 2 * CorrectableSymbolsNumber; i++) syndr[i] = 0;

                    //  Now we have new syndromes
                    clear = 1;
                    for (i = 0; i < (2 * CorrectableSymbolsNumber - distortionNumber); i++)
                    {
                        syndr[i] = syndrMod[i];
                        if (syndr[i] != 0) clear = 0;
                    }
                }
            }

            //  The number of bugs that can still be fixed
            int err = (2 * CorrectableSymbolsNumber - distortionNumber) / 2;

            //  Not all syndromes are equal to zero - search for other error positions
            if ((clear == 0) && (err != 0))
            {
                for (i = 0; i < err; i++)
                {
                    for (j = 0; j < err; j++)
                    {
                        matrSyndr[i,j] = syndr[i + j];
                    }
                }

                //  Syndrome column
                for (i = 0; i < err; i++)
                {
                    matrSyndr[i,err] = syndr[err + i];
                }

                //  Matrix solution by Gauss method [row] [column] - along the main diagonal
                for (k = 0; k < err; k++)
                {
                    for (j = 0; j < err; j++) //  row
                    {
                        if (j == k) continue;
                        //  Exception - Key Item is 0
                        //  There will be no changes in other lines in its column
                        //  It shouldn't be like this - add it with another line
                        if (matrSyndr[k,k] == 0)
                        {
                            //  Search for a row in which the element of the k column is not 0
                            for (ts = (err - 1); ts > 0; ts--)
                            {
                                if (matrSyndr[ts,k] != 0) break;
                            }

                            //  Add string ts with string k
                            for (tc = 0; tc < err + 1; tc++)
                            {
                                matrSyndr[k,tc] = (matrSyndr[k,tc] ^ matrSyndr[ts,tc]);
                            }
                        }

                        x = Div(matrSyndr[j,k], matrSyndr[k,k]);
                        for (i = 0; i < err + 1; i++) //  column
                        {
                            matrSyndr[j,i] = (matrSyndr[j,i] ^ Mul(x, matrSyndr[k,i]));
                        }
                    }
                }

                //  Quantifying Errors - Locator Values
                for (i = 0; i < err; i++)
                {
                    locatore[(CorrectableSymbolsNumber - err) + i] = Div(
                        matrSyndr[i,err], matrSyndr[i,i]);
                }

                locatore[CorrectableSymbolsNumber] = _indexToValue[0];

                //  Substitute the roots in the error locator polynomial(= 0 - error)
                for (i = 0; i < bufferSize; i++)
                {
                    position[i] = 0;
                    for (j = 0; j < err + 1; j++)
                    {
                        x = _indexToValue[((i) * (err - j)) % CodewordLength];
                        position[i] = SymbolBitsMask & (position[i] ^
                                      Mul(x, locatore[(CorrectableSymbolsNumber - err) + j]));
                    }
                }

                //  If position[i] equal 0 then error in position a^i
                for (i = 0; i < CodewordLength; i++)
                {
                    if (position[i] == 0)
                    {
                        poseInd[posErr] = ((CodewordLength + i - 1) % CodewordLength);
                        posErr++;
                    }
                }

                if ((posErr + distortionNumber) > 2 * CorrectableSymbolsNumber)
                {
                    return null;
                }
            }

            //  Add erasure positions to error positions
            for (i = 0; i < distortionNumber; i++)
            {
                gsInd[i] = ((CodewordLength - 1) - possInd[i]) & SymbolBitsMask;
            }

            for (i = 0; i < posErr; i++)
            {
                geInd[i] = ((CodewordLength - 1) - poseInd[i]) & SymbolBitsMask;
            }

            if (distortionIndexes != null)
            {
                //  Position information in one array
                for (i = 0; i < distortionNumber; i++)
                {
                    poseInd[posErr + i] = possInd[i];
                    geInd[posErr + i] = gsInd[i];
                }
            }

            err = (posErr + distortionNumber);

            //  We compose the matrix [err x err] to determine the value of ERRORS
            for (i = 0; i < err; i++)
            {
                for (j = 0; j < err; j++)
                {
                    matrErr[i,j] = _indexToValue[(geInd[j] *
                                               (i + ((TurnOverBits == false) ? 1 : 0))) % CodewordLength];
                }
            }

            //  Preparing a column of syndromes
            for (i = 0; i < err; i++)
            {
                matrErr[i,err] = syndc[i];
            }

            //  Matrix solution by Gauss method [row] [column] - along the main diagonal
            for (k = 0; k < err; k++)
            {
                for (j = 0; j < err; j++)
                {
                    if (j == k) continue;
                    //  Exception - Key Item is 0
                    //  There will be no changes in other lines in its column
                    //  It shouldn't be like this - add it with another line
                    if (matrErr[k,k] == 0)
                    {
                        //  Search for a row in which the element of the k column is not 0
                        for (ts = (err - 1); ts > 0; ts--)
                        {
                            if (matrErr[ts,k] != 0) break;
                        }

                        //  Add string ts with string k
                        for (tc = 0; tc < err + 1; tc++)
                        {
                            matrErr[k,tc] =
                                (matrErr[k,tc] ^ matrErr[ts,tc]);
                        }
                    }

                    x = Div(matrErr[j,k], matrErr[k,k]);
                    for (i = 0; i < err + 1; i++)
                    {
                        matrErr[j,i] = (matrErr[j,i] ^ Mul(x, matrErr[k,i]));
                    }
                }
            }

            //  Determining error values
            for (i = 0; i < err; i++)
            {
                error[i] = Div(matrErr[i,err], matrErr[i,i]);
            }

            //  Error polynomial
            for (i = 0; i < err; i++)
            {
                //  Don't delete this line!
                if (_valueToIndex[error[i]] == CodewordLength) continue;

                polyerr[poseInd[i]] = error[i];
            }

            //  Reconstructed polynomial
            for (i = 0; i < CodewordLength; i++)
            {
                nPolyDec[i] = (nPolyDec[i] ^ polyerr[i]);
            }

            //  Copying information to the output buffer
            for (i = 0; i < dataLength; i++)
            {
                dataOutput[i] = nPolyDec[CodewordLength - dataLength + i];
            }

            return dataOutput;
        }

        /// <remarks>
        /// dataInput
        /// +--------------------------------------------------+
        /// |                                                  |
        /// +--------------------------------------------------+
        /// |.--- CodewordLength + 1 - ParitySymbolsNumber ---.|
        ///
        /// output
        /// +-----------------------------+
        /// |                             |
        /// +-----------------------------+
        /// |.--- CodewordLength + 1 ----.|
        /// </remarks>
        public int[] EncodeHorner(int[] dataInput)
        {
            if ((dataInput == null) ||
                (dataInput.Length != (CodewordLength + 1 - ParitySymbolsNumber))) return null;

            int dataLength = dataInput.Length;

            int[] dataOutput = new int[CodewordLength + 1];
            int dl = dataLength / 2;
            int start = CodewordLength + 1 - (dataLength / 2 + CorrectableSymbolsNumber);
            int i;

            int indexOutput = 0;
            for (i = start; i <= CodewordLength; i++)
            {
                int indexInput = 0;

                int r = dataInput[indexInput] & SymbolBitsMask;
                indexInput += 1;
                int l = dataInput[indexInput] & SymbolBitsMask;
                indexInput += 1;

                var idexI = _valueToIndex[i];

                int j;
                for (j = 1; j < dl; j++)
                {
                    r = (((r == 0) ? 0 : (_indexToValue[(_valueToIndex[r] + idexI) % CodewordLength])) ^ (dataInput[indexInput] & SymbolBitsMask));
                    indexInput += 1;
                    l = (((l == 0) ? 0 : (_indexToValue[(_valueToIndex[l] + idexI) % CodewordLength])) ^ (dataInput[indexInput] & SymbolBitsMask));
                    indexInput += 1;
                }

                dataOutput[indexOutput] = l;
                indexOutput += 1;
                dataOutput[indexOutput] = r;
                indexOutput += 1;
            }

            return dataOutput;
        }

        /// <remarks>
        /// dataInput                  RS
        /// +-----------------------*------+
        /// |                       |      |   -   source package
        /// +-----------------------*------+
        /// |.---- CodewordLength + 1 ----.|
        ///
        /// output                     RS
        /// +-----------------------*------+
        /// |                       |      |   -   corrected package
        /// +-----------------------*------+
        /// |.---- CodewordLength + 1 ----.|
        /// </remarks>
        public int[] DecodeHorner(int[] dataInput, int[] distortions)
        {
            int dataLength = CodewordLength + 1;

            //  Parameter check
            if ((dataInput == null) ||
                (dataInput.Length != dataLength) ||
                (distortions == null) ||
                (distortions.Length != dataLength)) return null;

            int[] dataOutput = new int[dataLength];

            // Variables for encoding and decoding
            int[] mas = new int[6 * bufferSize + bufferSize * bufferSize / 2];

            int dl = dataLength / 2 - CorrectableSymbolsNumber;

            int i;
            int s;
            int realS;
            int shiftS = 1 + CodewordLength - (dataLength / 2);

            int matrixSize = dl * dl; // Matrix for solving equations. Total column and row size (DL * 2)
            int inputIndex = 0;
            int indexPm1 = 0;
            int indexPm2 = matrixSize;
            //  Create a matrix [row x column] equations
            for (realS = 0, s = 0; s < dataLength / 2; s++)
            {
                if (distortions[s * 2] != 0)
                {
                    inputIndex += 2;
                    continue;
                }

                int c;
                for (c = dl - 1; c >= 0; c--)
                {
                    mas[indexPm1] = _indexToValue[(_valueToIndex[s + shiftS] * c) % CodewordLength];
                    indexPm1 += 1;
                }

                mas[indexPm2] = dataInput[inputIndex];
                indexPm2 += 1;
                inputIndex += 1;
                mas[indexPm2] = dataInput[inputIndex];
                indexPm2 += 1;
                inputIndex += 1;

                realS++;
                if (realS == dl) break;
            }

            // solve the system of equations
            for (i = 0; i < dl; i++)
            {
                int j;
                int k;
                if (mas[i * dl + i] == 0)
                {
                    for (k = i; k < dl; k++)
                        if (mas[k * dl + i] != 0)
                            break;

                    for (j = i; j < dl; j++)
                        mas[i * dl + j] ^= mas[k * dl + j];

                    mas[matrixSize + i * 2 + 0] ^= mas[matrixSize + k * 2 + 0];
                    mas[matrixSize + i * 2 + 1] ^= mas[matrixSize + k * 2 + 1];
                }

                int x;
                for (k = i + 1; k < dl; k++)
                {
                    x = Div(mas[k * dl + i], mas[i * dl + i]);

                    for (j = i; j < dl; j++)
                        mas[k * dl + j] ^= Mul(mas[i * dl + j], x);

                    mas[matrixSize + k * 2 + 0] ^= Mul(mas[matrixSize + i * 2 + 0], x);
                    mas[matrixSize + k * 2 + 1] ^= Mul(mas[matrixSize + i * 2 + 1], x);
                }


                for (k = i - 1; k >= 0; k--)
                {
                    x = Div(mas[k * dl + i], mas[i * dl + i]);

                    for (j = i; j < dl; j++)
                        mas[k * dl + j] ^= Mul(mas[i * dl + j], x);

                    mas[matrixSize + k * 2 + 0] ^= Mul(mas[matrixSize + i * 2 + 0], x);
                    mas[matrixSize + k * 2 + 1] ^= Mul(mas[matrixSize + i * 2 + 1], x);
                }
            }

            for (i = 0; i < dl; i++)
            {
                dataOutput[i * 2 + 0] =
                    Div(mas[matrixSize + i * 2 + 1], mas[i * dl + i]);
                dataOutput[i * 2 + 1] =
                    Div(mas[matrixSize + i * 2 + 0], mas[i * dl + i]);
            }

            return dataOutput;
        }
    }
}
