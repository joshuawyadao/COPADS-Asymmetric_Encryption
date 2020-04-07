/**
 * Joshua Yadao
 * Professor Jeremy Brown
 * CSCI 251
 * 17 April 2020
 */

using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    internal static class Extension
    {
        public static BigInteger ModInverse( BigInteger a, BigInteger n )
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
        
        public static bool IsProbablyPrime( this BigInteger value, int witnesses = 10 ) {
            if ( value <= 1 ) return false;
            
            if ( witnesses <= 0 ) witnesses = 10;
            var d = value - 1;
            var s = 0;
            
            while ( d % 2 == 0 ) {
                d /= 2;
                s += 1;
            }
            
            var bytes = new byte[ value.ToByteArray().LongLength ];

            for ( var i = 0; i < witnesses; i++ ) {
                BigInteger a;
                do {
                    var gen = new Random();
                    gen.NextBytes( bytes );
                    a = new BigInteger( bytes );
                } while ( a < 2 || a >= value - 2 );
                
                var x = BigInteger.ModPow( a, d, value );
                if ( x == 1 || x == value - 1 ) continue;
                
                for ( var r = 1; r < s; r++ ) {
                    x = BigInteger.ModPow( x, 2, value );
                    if ( x == 1 ) return false;
                    if ( x == value - 1 ) break;
                }
                
                if ( x != value - 1 ) return false;
            }
            return true;
        }
    }
    
    internal static class PrimeGen
    {
         private static readonly object MyLock = new object();
         public static BigInteger GenPrimeNum( int bits )
         {
            var byteArray = new byte[ bits / 8 ];
            var rngGen = new RNGCryptoServiceProvider();

            var po = new ParallelOptions {MaxDegreeOfParallelism = 3};
            var source = new CancellationTokenSource();
            po.CancellationToken = source.Token;

            var primeNum = BigInteger.One;

            try
            {
                Parallel.For(1, int.MaxValue, po, (i, state) =>
                {
                    rngGen.GetBytes(byteArray);
                    var randNum = new BigInteger(byteArray);
                    if (!randNum.IsProbablyPrime()) return;
                    lock (MyLock)
                    {
                        primeNum = randNum;
                        source.Cancel();
                    }
                });
            }
            catch ( OperationCanceledException ) {}
            return primeNum;
            }
    }

    class Program
    {
        
        private enum Options
        {
            KeyGen,
            SendKey,
            GetKey,
            SendMsg,
            GetMsg,
            Invalid
        }
        
        private static void Usage( int variation = 0, Options op = Options.Invalid )
        {
            switch ( variation )
            {
                case 1:
                    Console.WriteLine( "<options>: keyGen, sendKey, getKey, sendMsg, getMsg" );
                    break;
                default:
                    Console.WriteLine( "dotnet run <option> <other arguments>\n" );
                    Console.WriteLine( "{0,15}", "<options>: keyGen, sendKey, getKey, sendMsg, getMsg" );
                    Console.WriteLine( "{0,29}", "<keyGen> <keysize>");
                    Console.WriteLine( "{0,60}", "<keysize>: size of keys generated");
                    break;
            }
            Environment.Exit( 1 );
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

        private static void GenerateKey( int keySize, string priKeyFile, string pubKeyFile )
        {
            var rand = new Random();
            var first = keySize / 2 + rand.Next(-keySize / 5 , keySize / 5 ) ;
            
            var p = PrimeGen.GenPrimeNum(  first );
            var q = PrimeGen.GenPrimeNum( keySize - first );

            var n = p * q;
            var r = ( p - 1 ) * ( q - 1 );
            var e = PrimeGen.GenPrimeNum( 65536 );
            var d = Extension.ModInverse( e, r );
            
            
        }

        public static void Main( string[] args )
        {
            if ( args.Length != 2 ) Usage();
            var option = GetOptions( args[0] );
            if ( option == Options.Invalid ) Usage( 1 );
            var email = args[1];

            switch (option)
            {
                case Options.KeyGen:
                    GenerateKey( Convert.ToInt32( args[1] ), "private.key", "pubic.key" );
 
                    break;
                case Options.SendKey:
                    break;
                case Options.GetKey:
                    break;
                case Options.SendMsg:
                    break;
                case Options.GetMsg:
                    break;
                case Options.Invalid:
                    break;
                default:
                    break;
            }
        }
    }
}