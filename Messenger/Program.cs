using System;
using System.Numerics;

namespace Messenger
{
    internal static class Extension
    {
        static BigInteger modInverse( BigInteger a, BigInteger n )
        {
            BigInteger i = n, v = 0, d = 1;
            while ( a > 0 )
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if ( v < 0 ) v = ( v + n ) % n;
            return v;
        }
    }

    class Program
    {
        private static void Usage()
        {   
            string usageText = "dotnet run <option> <other arguments>\n";
            usageText += $"{"<option>:", 12} keyGen, sendKey, getKey, sendMsg, getMsg\n";
            usageText += $"{"<other args>:", 12}\n";
            Console.WriteLine( usageText );
            Environment.Exit( 1 );
        }
        
        private enum Options
        {
            KeyGen,
            SendKey,
            GetKey,
            SendMsg,
            GetMsg,
            Invalid
        }

        private static Options GetOptions( string arg )
        {
            var val = arg switch
            {
                "keyGen" => Options.KeyGen,
                "sendKey" => Options.SendKey,
                "getKey" => Options.GetKey,
                "sendMsg" => Options.SendMsg,
                "getMsg" => Options.GetMsg,
                _ => Options.Invalid
            };

            return val;
        }
        
        public static void Main( string[] args )
        {
            if ( args.Length != 2 ) Usage();
            var option = GetOptions( args[0] );
            
        }
    }
}