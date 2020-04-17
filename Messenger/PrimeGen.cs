using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    internal static class Extension
    {
        internal static bool IsProbablyPrime( this BigInteger value, int witnesses = 10 ) {
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
    internal class PrimeGen
    {
        private static readonly object MyLock = new object();
        private static readonly RNGCryptoServiceProvider RngGen = new RNGCryptoServiceProvider();

        internal BigInteger GenPrimeNum( int bytes, int count )
        {
            var numResults = 0;
            var po = new ParallelOptions();
            var source = new CancellationTokenSource();
            po.CancellationToken = source.Token;
            po.CancellationToken.Register(() => { });

            BigInteger prime = 0;
            
            try
            {
                Parallel.For(0, int.MaxValue, po, (loop) =>
                {
                    var byteArray = new byte[bytes / 8];
                    RngGen.GetBytes(byteArray);
                    var randNum = new BigInteger(byteArray);
                    
                    if (randNum.IsProbablyPrime())
                    {
                        lock (MyLock)
                        {
                            if (numResults < count)
                            {
                                prime = randNum;
                                Interlocked.Increment(ref numResults);
                            }
                        }
                    }
                    
                    if (numResults >= count)
                    {
                        source.Cancel();
                    }
                });
            }
            catch
            {
                // ignored
            }
            return prime;
        } 
    }
}