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
    internal static class Given
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
    }

    internal class Keys
    {
        public List<string> Email { get; set; }
        public string Key { get; set; }  // make sure to be Base64

        public Keys( List<string> email, string key )
        {
            Email = email;
            Key = key;
        }
    }

    internal class Messages : IDisposable
    {
        public string Email { get; set; }
        public string Content { get; set; }  // make sure to be Base64

        public Messages( string email, string content )
        {
            Email = email;
            Content = content;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    internal class Modifier
    {
        public List<BigInteger> DecodeKey( string base64Key )
        {
            var keyByte = Convert.FromBase64String( base64Key );  // big endian

            // length of e
            var eByte = new byte[4];
            Array.Copy( keyByte, 0, 
                eByte, 0, 4 );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( eByte, 0, eByte.Length );
            }
            var eBigInt = new BigInteger( eByte );
            
            // contents of e
            var byteE = new byte[ (int) eBigInt ];
            Array.Copy(keyByte, 4, 
                byteE, 0, (int) eBigInt );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteE, 0, byteE.Length );
            }
            var e = new BigInteger( byteE ); 
            
            // length of n
            var nByte = new byte[4];
            Array.Copy( keyByte, 4 + (int) eBigInt, 
                nByte, 0, 4 );
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( nByte, 0, eByte.Length );
            }
            var nBigInt = new BigInteger( nByte );
            
            // contents of n
            var byteN = new byte[ (int) nBigInt ];
            Array.Copy(keyByte, 8 + (int) eBigInt, 
                byteN, 0, (int) nBigInt );
            if ( !BitConverter.IsLittleEndian )
            {
                Array.Reverse( byteN, 0, byteE.Length );
            }
            var n = new BigInteger( byteN );
            
            return new List<BigInteger> { e, n };
        }

        public string EncryptKey( BigInteger e , BigInteger n )  // WRONG
        {
            Console.WriteLine( "ENCRYPT: e: {0}", e);
            //Console.WriteLine( "ENCRYPT: n: {0}", n);
            
            var eByteSource = e.ToByteArray();  // little endian
            var nByteSource = n.ToByteArray();  // little endian
            
            Console.WriteLine( "ENCRYPT: e len: {0}", eByteSource.Length);
            Console.WriteLine( "ENCRYPT: n len: {0}", nByteSource.Length);

            var byteArr = new byte[ 8 + eByteSource.Length + nByteSource.Length ];

            // length of e
            var eLenBytes = new byte[] { 0, 0, 0, 0 };
            var tempE = BitConverter.GetBytes( eByteSource.Length );
            Array.Copy(tempE, 0,
            eLenBytes, 0, tempE.Length);
            
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( eLenBytes, 0, eLenBytes.Length );
            }
            Array.Copy( eLenBytes, 0, 
            byteArr, 0, eLenBytes.Length );
            
            // contents of e
            var eByteContents = new byte[ eByteSource.Length ];
            Array.Copy(eByteSource, 0, 
                eByteContents, 0, eByteSource.Length);
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( eByteContents, 0, eByteContents.Length );
            }
            Array.Copy( eByteContents, 0, 
                byteArr, 4, eByteContents.Length);
            
            // length of n
            var nLenBytes = new byte[] { 0, 0, 0, 0 };
            var tempN = BitConverter.GetBytes( nByteSource.Length );
            Array.Copy(tempN, 0,
                nLenBytes, 0, tempN.Length);
            
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( nLenBytes, 0, nLenBytes.Length );
            }
            Array.Copy( nLenBytes, 0, 
                byteArr, 4 + eByteContents.Length, nLenBytes.Length );

            // contents of n
            var nByteContents = new byte[ nByteSource.Length ];
            Array.Copy(nByteSource, 0, 
                nByteContents, 0, nByteSource.Length);
            if ( BitConverter.IsLittleEndian )
            {
                Array.Reverse( nByteContents, 0, nByteContents.Length );
            }
            Array.Copy( nByteContents, 0, 
                byteArr, 8 + eByteContents.Length, nByteContents.Length);
            
            return Convert.ToBase64String( byteArr );
        }
    }
    internal class ReqTasks
    {
        private readonly Modifier Mod = new Modifier();
        private readonly HttpClient Client = new HttpClient();
        internal void GenerateKey( int keySize )
        {
            // SLOW PLEASE FIX!!!
            //var rand = new Random();
            //var firstSize = keySize / 2 + rand.Next( -keySize / 5, keySize / 5 );

            Console.WriteLine("GENERATING KEYS!!!");
            
            var primeGen = new PrimeGen();
            var firstSize = keySize/2;
            BigInteger p = primeGen.GenPrimeNum( firstSize, 1 );
            //var q = primeGen.GenPrimeNum( firstSize );
            Console.WriteLine("p:{0}", p);
            //Console.WriteLine("p:{0}\nq:{1}", p, q );

            /*
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
            */
        }
        internal void SendKey( string email )
        {
            try
            {
                var privateKey = JsonConvert.DeserializeObject<Keys>(File.ReadAllText("private.key" ));
                var publicKey = JsonConvert.DeserializeObject<Keys>(File.ReadAllText( "public.key" ));
                var publicKeyJson = JsonConvert.SerializeObject(publicKey, Formatting.Indented);

                var response = Client.PutAsync("http://kayrun.cs.rit.edu:5000/Key/" + email,
                    new StringContent(publicKeyJson, Encoding.UTF8, "application/json")).Result;
                response.EnsureSuccessStatusCode();

                privateKey.Email.Add(email);
                var privateKeyJson = JsonConvert.SerializeObject(privateKey, Formatting.Indented);
                File.WriteAllText("private.key", privateKeyJson);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("Cannot find " + email + ".key to send key");
            }
        }
        internal void SendMessage( string email, string plaintext )
        {
            try
            {
                var keyObj = JsonConvert.DeserializeObject<Keys>( File.ReadAllText( email + ".key" ) );
                var decodeEn = Mod.DecodeKey( keyObj.Key );
                var e = decodeEn[0];
                var n = decodeEn[1];

                var bigText = BigInteger.Parse(plaintext);
                var cypherText = (bigText ^ e) % n;
                
                var message = new Messages(email, Convert.ToBase64String(cypherText.ToByteArray()));
                var messageJson = JsonConvert.SerializeObject(message, Formatting.Indented);

                var response = Client.PutAsync("http://kayrun.cs.rit.edu:5000/Message/" + email,
                        new StringContent(messageJson, Encoding.UTF8, "application/json")).Result;
                response.EnsureSuccessStatusCode();
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("Cannot find " + email + ".key to send message");
            }
        }

        internal void GetKey( string email )
        {
            var response = Client.GetAsync("http://kayrun.cs.rit.edu:5000/Key/" + email).Result;
            response.EnsureSuccessStatusCode();

            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var keyObj = JsonConvert.DeserializeObject<Keys>(jsonObj);
            var newJsonObj = JsonConvert.SerializeObject(keyObj, Formatting.Indented);

            File.WriteAllText(email + ".key", newJsonObj);
        }

        internal void GetMessage( string email )
        {
            var response = Client.GetAsync("http://kayrun.cs.rit.edu:5000/Message/" + email).Result;
            response.EnsureSuccessStatusCode();
            
            var jsonObj = response.Content.ReadAsStringAsync().Result;
            var messageObj = JsonConvert.DeserializeObject<Messages>(jsonObj);

            var privateKeyObj = JsonConvert.DeserializeObject<Keys>(File.ReadAllText("private.key" ));
            
            if (!privateKeyObj.Email.Contains(email))
            {
                throw new ArgumentException(email + " was not found in private.key. " +
                                            "Please send " + email + " a key first" );
            }
            
            var decodeEn = Mod.DecodeKey( privateKeyObj.Key );
            var d = decodeEn[0];
            var n = decodeEn[1];
            
            var cypherText = BigInteger.Parse(messageObj.Content);
            var plainText = (cypherText ^ d) % n;
            
            Console.WriteLine(plainText);
        }
    }
    
    public static class Program
    {
        private static readonly ReqTasks Request = new ReqTasks();
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
        
        public static void Main( string[] args )
        {
            if ( args.Length < 2 || args.Length > 3 ) Usage();
            var option = GetOptions( args[0] );
            if ( option == Options.Invalid ) Usage( 1 );
            //Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            switch (option)
            {
                case Options.KeyGen:
                    if ( args.Length != 2 ) Usage();
                    Request.GenerateKey( Convert.ToInt32( args[1] ) );
                    break;
                case Options.SendKey:
                    if ( args.Length != 2 ) Usage();
                    Request.SendKey( args[1] );
                    break;
                case Options.GetKey:  // WORKS FINE!!!
                    if ( args.Length != 2 ) Usage();
                    Request.GetKey( args[1] );
                    break;
                case Options.SendMsg:
                    if ( args.Length != 3 ) Usage();
                    Request.SendMessage( args[1], args[2] );
                    break;
                case Options.GetMsg:
                    if ( args.Length != 2 ) Usage();
                    Request.GetMessage( args[1] );
                    break;
                case Options.Invalid:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}