namespace ReedSolomonCodes
{
    public static class ReedSolomon
    {
        public static ReedSolomonCode Create7X3(bool turnOverBits = true)
        {
            return new ReedSolomonCode(turnOverBits, 3, 2);
        }

        public static ReedSolomonCode Create15X11(bool turnOverBits = true)
        {
            return new ReedSolomonCode(turnOverBits, 4, 2);
        }

        public static ReedSolomonCode Create31X27(bool turnOverBits = true)
        {
            return new ReedSolomonCode(turnOverBits, 5, 2);
        }

        public static ReedSolomonCode Create255X233(bool turnOverBits = true)
        {
            return new ReedSolomonCode(turnOverBits, 8, 11);
        }

        public static ReedSolomonCode Create255X239(bool turnOverBits = true)
        {
            return new ReedSolomonCode(turnOverBits, 8, 8);
        }

        public static ReedSolomonCode Create(bool turnOverBits, int bitsNumberInSymbol, int correctableSymbolsNumber)
        {
            return new ReedSolomonCode(turnOverBits, bitsNumberInSymbol, correctableSymbolsNumber);
        }
    }
}
