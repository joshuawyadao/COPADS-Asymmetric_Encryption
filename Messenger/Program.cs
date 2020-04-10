/**
 * Joshua Yadao
 * Professor Jeremy Brown
 * CSCI 251
 * 17 April 2020
 */

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        
        private static bool IsProbablyPrime( this BigInteger value, int witnesses = 10 ) {
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

        private static readonly object MyLock = new object();

        public static BigInteger GenPrimeNum( int bits )
        {
            var count = 3;

            var byteArray = new byte[ bits / 8 ];
            var rngGen = new RNGCryptoServiceProvider();
            var pCount = 1;

            var po = new ParallelOptions {MaxDegreeOfParallelism = 3};
            var source = new CancellationTokenSource();
            po.CancellationToken = source.Token;

            var prime = BigInteger.One;
            
            try
            {
                Parallel.For(1, int.MaxValue, po, (i, state) =>
                {
                    rngGen.GetBytes(byteArray);
                    var randNum = new BigInteger(byteArray);

                    lock (MyLock)
                    {
                        if (pCount > count )
                            source.Cancel();
                    }

                    if (!randNum.IsProbablyPrime()) return;
                    lock (MyLock)
                    {
                        if (pCount > count)
                            source.Cancel();
                        else
                        {
                            prime = randNum;
                            Interlocked.Increment(ref pCount);
                        }
                    }
                });
            }
            catch (OperationCanceledException) { }

            return prime;
        } 
    }

    class Keys
    {
        public string Email { get; set; }
        public string Key { get; set; }  // make sure to be Base64

        public Keys( string email, string key )
        {
            Email = email;
            Key = key;
        }
    }

    class Messages
    {
        public string Email { get; set; }
        public string Content { get; set; }  // make sure to be Base64

        public Messages( string email, string content )
        {
            Email = email;
            Content = content;
        }
    }

    class Program
    {
        private static readonly HttpClient Client = new HttpClient();
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
        
        private static IEnumerable<BigInteger> DecodeKey( string base64Key )
        {
            var keyByte = Convert.FromBase64String( base64Key );
            
            var eByte = new byte[4];
            Array.Copy( keyByte, 0, 
                eByte, 0, 4 );
            
            Console.WriteLine("[{0}]", string.Join(", ", eByte));
            
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( eByte, 0, eByte.Length );
            }
            var eBigInt = new BigInteger( eByte );

            var byteE = new byte[ (int) eBigInt ];
            Array.Copy(keyByte, 4, 
                byteE, 0, (int) eBigInt );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteE, 0, byteE.Length );
            }
            var e = new BigInteger( byteE ); 
            
            var nByte = new byte[4];
            Array.Copy( keyByte, 4 + (int) eBigInt, 
                nByte, 0, 4 );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( nByte, 0, eByte.Length );
            }
            var nBigInt = new BigInteger( nByte );
            
            var byteN = new byte[ (int) nBigInt ];
            Array.Copy(keyByte, 4 + (int) eBigInt + 4, 
                byteN, 0, (int) nBigInt );
            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteN, 0, byteE.Length );
            }
            var n = new BigInteger( byteN );
            
            var enArr = new BigInteger[] { e, n };
            return enArr;
        }

        private static string EncryptKey( BigInteger e, BigInteger n )
        {
            return "EncryptKey()";
        }

        private static void RequestKeyGen( int keySize )
        {
            // SLOW PLEASE FIX!!!
            var rand = new Random();
            var firstSize = keySize / 2 + rand.Next( -keySize / 5, keySize / 5 );

            var p = Extension.GenPrimeNum( firstSize );
            var q = Extension.GenPrimeNum( keySize - firstSize );

            var n = p * q;
            var r = ( p - 1 ) * ( q - 1 );
            var e = Extension.GenPrimeNum( 2 ^ 16 );
            var d = Extension.ModInverse( e, r ); 

            // below works fine
            var publicKeyStr = EncryptKey( e, n );
            var privateKeyStr = EncryptKey( d, n );

            var publicKey = new Keys( "", publicKeyStr );
            var privateKey = new Keys( "", privateKeyStr );

            var publicKeyJson = JsonConvert.SerializeObject( publicKey, Formatting.Indented );
            var privateKeyJson = JsonConvert.SerializeObject(privateKey, Formatting.Indented);

            File.WriteAllText( "public.key", publicKeyJson );
            File.WriteAllText( "private.key", privateKeyJson );
        }

        private static void RequestSendKey( string email )
        {
            
        }
        
        private static void RequestGetKey( string email )
        {
            var response = Client.GetAsync( "http://kayrun.cs.rit.edu:5000/Key/" + email ).Result;
            response.EnsureSuccessStatusCode();
            
            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var keyObj = JsonConvert.DeserializeObject<Keys>( jsonObj );
            var newJsonObj = JsonConvert.SerializeObject( keyObj, Formatting.Indented );
            
            File.WriteAllText(email + ".key", newJsonObj );
        }

        private static void RequestSendMsg( string email, string plaintext )
        {
            try
            {
                var keyObj = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( email + ".key" ) );
                var decodeEN = DecodeKey( keyObj.Key );
            }
            catch (FileNotFoundException)
            {
                
            }
            
        }

        private static void RequestGetMsg( string email )
        {
            
        }
        
        public static void Main( string[] args )
        {
            if ( args.Length < 2 || args.Length > 3 ) Usage();
            var option = GetOptions( args[0] );
            if ( option == Options.Invalid ) Usage( 1 );

            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            switch (option)
            {
                case Options.KeyGen:
                    if ( args.Length != 2 ) Usage();
                    RequestKeyGen( Convert.ToInt32( args[1] ) );
                    break;
                case Options.SendKey:
                    if ( args.Length != 2 ) Usage();
                    RequestSendKey( args[1] );
                    break;
                case Options.GetKey:
                    if ( args.Length != 2 ) Usage();
                    RequestGetKey( args[1] );
                    break;
                case Options.SendMsg:
                    if ( args.Length != 3 ) Usage();
                    RequestSendMsg( args[1], args[2] );
                    break;
                case Options.GetMsg:
                    if ( args.Length != 2 ) Usage();
                    RequestGetMsg( args[1] );
                    break;
                case Options.Invalid:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}